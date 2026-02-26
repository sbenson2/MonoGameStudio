using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Project;
using MonoGameStudio.Core.Serialization;
using PrefabSerializer = MonoGameStudio.Core.Serialization.PrefabSerializer;
using MonoGameStudio.Core.Systems;
using MonoGameStudio.Core.UI;
using SpriteRenderingSystem = MonoGameStudio.Core.Systems.SpriteRenderingSystem;
using AnimationSystem = MonoGameStudio.Core.Systems.AnimationSystem;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Commands;
using MonoGameStudio.Editor.Assets;
using MonoGameStudio.Editor.Gizmos;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;
using MonoGameStudio.Editor.Panels;
using MonoGameStudio.Editor.Platform;
using Theme = ktsu.ImGuiStyler.Theme;
using MonoGameStudio.Editor.Project;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Editor;

public class EditorGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    // ImGui
    private ImGuiManager _imGui = null!;
    private DockingLayout _dockingLayout = null!;

    // Panels
    private MenuBarPanel _menuBar = null!;
    private ToolbarPanel _toolbar = null!;
    private SceneHierarchyPanel _hierarchy = null!;
    private InspectorPanel _inspector = null!;
    private GameViewportPanel _viewportPanel = null!;
    private ConsolePanel _console = null!;
    private AssetBrowserPanel _assetBrowser = null!;

    // ECS
    private WorldManager _worldManager = null!;
    private TransformPropagationSystem _transformSystem = null!;
    private EditorState _editorState = null!;

    // Viewport
    private ViewportRenderer _viewportRenderer = null!;
    private EditorCamera _editorCamera = null!;
    private GridRenderer _gridRenderer = null!;
    private GizmoRenderer _gizmoRenderer = null!;
    private GizmoManager _gizmoManager = null!;
    private SelectionSystem _selectionSystem = null!;
    private ColliderVisualization _colliderViz = null!;

    // Commands / Shortcuts
    private CommandHistory _commandHistory = null!;
    private ShortcutManager _shortcutManager = null!;
    private PlayModeManager _playModeManager = null!;
    private ClipboardManager _clipboardManager = null!;

    // Gum UI
    private GumUIManager _gumUIManager = null!;
    private GumUISystem _gumUISystem = null!;

    // Project management
    private ProjectManager _projectManager = null!;
    private StartScreenPanel _startScreen = null!;
    private readonly string[] _launchArgs;

    // Assets
    private AssetDatabase _assetDatabase = null!;
    private TextureCache _textureCache = null!;

    // File dialogs
    private IFileDialogService _fileDialogs = null!;

    // Settings
    private UserDataManager _userDataManager = null!;
    private EditorPreferences _editorPreferences = null!;
    private SettingsPanel _settingsPanel = null!;

    // Sprite / Animation
    private SpriteRenderingSystem _spriteRenderingSystem = null!;
    private AnimationSystem _animationSystem = null!;
    private SpriteSheetPanel _spriteSheetPanel = null!;
    private AnimationPanel _animationPanel = null!;

    // Layout profiles
    private LayoutProfileManager _layoutProfileManager = null!;
    private bool _showSaveLayoutPopup;
    private string _saveLayoutName = "";

    private MouseState _prevMouse;

    public EditorGame(string[]? args = null)
    {
        _launchArgs = args ?? Array.Empty<string>();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1920,
            PreferredBackBufferHeight = 1080,
            PreferMultiSampling = true,
            SynchronizeWithVerticalRetrace = true
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "MonoGame.Studio";
    }

    protected override void Initialize()
    {
        Log.Info("MonoGameStudio initializing...");

        // ImGui
        _imGui = new ImGuiManager(this);
        _dockingLayout = new DockingLayout();

        // ECS
        _worldManager = new WorldManager();
        _transformSystem = new TransformPropagationSystem(_worldManager);
        _editorState = new EditorState();

        // Viewport
        _editorCamera = new EditorCamera();
        _gridRenderer = new GridRenderer();
        _gizmoRenderer = new GizmoRenderer();
        _gizmoManager = new GizmoManager(_worldManager, _editorState, _gizmoRenderer, _editorCamera);
        _selectionSystem = new SelectionSystem(_worldManager, _editorState, _editorCamera);
        _colliderViz = new ColliderVisualization(_worldManager, _gizmoRenderer, _editorCamera);

        // Commands
        _commandHistory = new CommandHistory();
        _shortcutManager = new ShortcutManager();
        _playModeManager = new PlayModeManager(_worldManager, _editorState);
        _clipboardManager = new ClipboardManager(_worldManager, _editorState);

        // Gum UI
        _gumUIManager = new GumUIManager();
        _gumUISystem = new GumUISystem(_worldManager, _gumUIManager);

        // User data + preferences (must be before ProjectManager for recent projects)
        _userDataManager = new UserDataManager();
        _editorPreferences = _userDataManager.LoadPreferences();

        // File dialogs
        _fileDialogs = OperatingSystem.IsMacOS()
            ? new MacFileDialogService()
            : new FallbackFileDialogService();

        // Project management
        _projectManager = new ProjectManager();
        _projectManager.OnProjectOpened += OnProjectOpened;
        _projectManager.OnProjectClosed += OnProjectClosed;

        // Panels (constructed after dependencies)
        _startScreen = new StartScreenPanel(_projectManager, _fileDialogs);
        _menuBar = new MenuBarPanel(_editorState);
        _toolbar = new ToolbarPanel();
        _hierarchy = new SceneHierarchyPanel(_worldManager, _editorState);
        _inspector = new InspectorPanel(_worldManager, _editorState);
        _inspector.SetCommandHistory(_commandHistory);
        _console = new ConsolePanel(_imGui);
        _assetDatabase = new AssetDatabase();
        _assetBrowser = new AssetBrowserPanel();
        _spriteSheetPanel = new SpriteSheetPanel();
        _animationPanel = new AnimationPanel();
        // SettingsPanel created in LoadContent after ImGuiManager is initialized

        // Layout profiles
        _layoutProfileManager = new LayoutProfileManager(_userDataManager.LayoutsDirectory, _dockingLayout);

        // Wire hierarchy events
        _hierarchy.OnSaveAsPrefab += SaveEntityAsPrefab;

        // Wire menu events
        _menuBar.OnNewScene += NewScene;
        _menuBar.OnSaveScene += SaveScene;
        _menuBar.OnSaveSceneAs += SaveSceneAs;
        _menuBar.OnOpenScene += OpenScene;
        _menuBar.OnCloseProject += () => _projectManager.CloseProject();
        _menuBar.OnUndo += () => _commandHistory.Undo();
        _menuBar.OnRedo += () => _commandHistory.Redo();
        _menuBar.OnLoadLayout += name => _layoutProfileManager.LoadProfile(name);
        _menuBar.OnDeleteLayout += name => _layoutProfileManager.DeleteProfile(name);
        _menuBar.OnSaveLayoutRequested += () => { _showSaveLayoutPopup = true; _saveLayoutName = ""; };
        _menuBar.LayoutProfileManager = _layoutProfileManager;

        // Wire toolbar events
        _toolbar.OnPlay += () => _playModeManager.Play();
        _toolbar.OnPause += () => _playModeManager.Pause();
        _toolbar.OnStop += () => _playModeManager.Stop();

        // Wire shortcuts
        _shortcutManager.OnSave += SaveScene;
        _shortcutManager.OnOpen += OpenScene;
        _shortcutManager.OnNew += NewScene;
        _shortcutManager.OnUndo += () => _commandHistory.Undo();
        _shortcutManager.OnRedo += () => _commandHistory.Redo();
        _shortcutManager.OnDelete += DeleteSelected;
        _shortcutManager.OnDuplicate += DuplicateSelected;
        _shortcutManager.OnGizmoNone += () => _gizmoManager.CurrentMode = GizmoMode.None;
        _shortcutManager.OnGizmoBoundingBox += () => _gizmoManager.CurrentMode = GizmoMode.BoundingBox;
        _shortcutManager.OnGizmoMove += () => _gizmoManager.CurrentMode = GizmoMode.Move;
        _shortcutManager.OnGizmoRotate += () => _gizmoManager.CurrentMode = GizmoMode.Rotate;
        _shortcutManager.OnGizmoScale += () => _gizmoManager.CurrentMode = GizmoMode.Scale;
        _shortcutManager.OnFocusSelected += FocusSelected;
        _shortcutManager.OnCopy += () => _clipboardManager.Copy();
        _shortcutManager.OnPaste += PasteFromClipboard;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imgui.ini");
        _imGui.Initialize(iniPath, _editorPreferences);

        _settingsPanel = new SettingsPanel(_imGui, _userDataManager, _editorPreferences);
        _settingsPanel.SetEditorState(_editorState);

        _viewportRenderer = new ViewportRenderer(GraphicsDevice, _imGui);
        _viewportPanel = new GameViewportPanel(_viewportRenderer);
        _viewportPanel.OnAssetDropped += OnAssetDroppedOnViewport;

        _gridRenderer.LoadContent(GraphicsDevice);
        _gizmoRenderer.LoadContent(GraphicsDevice);

        // Asset browser and sprite systems (need GraphicsDevice for texture loading)
        _textureCache = new TextureCache(GraphicsDevice);
        _assetBrowser.Initialize(_assetDatabase, _textureCache, _imGui);
        _spriteRenderingSystem = new SpriteRenderingSystem(_worldManager, _textureCache);
        _animationSystem = new AnimationSystem(_worldManager);
        _spriteSheetPanel.Initialize(_textureCache, _imGui);


        _gumUIManager.Initialize(this);

        // Native macOS menu bar, title bar, and toolbar
        if (OperatingSystem.IsMacOS())
        {
            MacMenuCallbacks.Initialize();
            MacMenuCallbacks.OnMenuAction += HandleNativeMenuAction;
            MacMenuCallbacks.LayoutProfileManager = _layoutProfileManager;
            MacMenuBar.Install(_editorState, _layoutProfileManager);
            _layoutProfileManager.OnProfilesChanged += RebuildNativeLayoutsMenu;
            _menuBar.UseNativeMenu = true;

            // Native menu bar only — no toolbar padding needed (traffic lights are in title bar)
        }

        Log.Info("MonoGameStudio loaded and ready.");

        // Open project from CLI args (e.g., dotnet run -- /path/to/project.mgstudio)
        if (_launchArgs.Length > 0 && _launchArgs[0].EndsWith(".mgstudio", StringComparison.OrdinalIgnoreCase))
        {
            _projectManager.OpenProject(_launchArgs[0]);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // Start screen: skip all editor logic
        if (_editorState.Phase == ApplicationPhase.StartScreen)
        {
            base.Update(gameTime);
            return;
        }

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        _shortcutManager.Update(keyboard);

        // Gum UI update
        bool isPlayMode = _editorState.Mode == EditorMode.Play;
        _gumUIManager.IsInputEnabled = isPlayMode;
        if (isPlayMode)
            _gumUIManager.SetViewportOffset(_viewportPanel.ViewportOrigin.X, _viewportPanel.ViewportOrigin.Y);
        _gumUIManager.Update(gameTime);

        // Sync toolbar gizmo mode
        _toolbar.CurrentMode = _editorState.Mode;
        _toolbar.CurrentGizmoMode = _gizmoManager.CurrentMode;
        _gizmoManager.CurrentMode = _toolbar.CurrentGizmoMode;

        bool isEditMode = _editorState.Mode == EditorMode.Edit;
        var vpOrigin = new Microsoft.Xna.Framework.Vector2(
            _viewportPanel.ViewportOrigin.X, _viewportPanel.ViewportOrigin.Y);
        var vpSize = new Microsoft.Xna.Framework.Vector2(
            _viewportPanel.ViewportSize.X, _viewportPanel.ViewportSize.Y);
        bool vpHovered = _viewportPanel.IsHovered;

        // Input routing: ImGui > Gizmo > Selection > Camera
        if (!Hexa.NET.ImGui.ImGui.GetIO().WantCaptureMouse && vpHovered && isEditMode)
        {
            bool gizmoConsumed = _gizmoManager.Update(mouse, vpOrigin, vpSize);

            if (!gizmoConsumed && !_gizmoManager.IsActive)
            {
                _selectionSystem.Update(mouse, _prevMouse, vpOrigin, vpSize, vpHovered);
            }

            if (!_gizmoManager.IsActive)
            {
                _editorCamera.Update(mouse, vpOrigin, vpSize, vpHovered);
            }

            // Create undo command on gizmo drag end
            if (_gizmoManager.WasDragging && _editorState.PrimarySelection.HasValue)
            {
                var entity = _editorState.PrimarySelection.Value;
                if (_worldManager.World.IsAlive(entity))
                {
                    var pos = _worldManager.World.Get<MonoGameStudio.Core.Components.Position>(entity);
                    var currentPos = new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y);
                    var scl = _worldManager.World.Get<MonoGameStudio.Core.Components.Scale>(entity);
                    var currentScale = new Microsoft.Xna.Framework.Vector2(scl.X, scl.Y);

                    bool posChanged = currentPos != _gizmoManager.DragStartPosition;
                    bool scaleChanged = currentScale != _gizmoManager.DragStartScale;

                    if (posChanged || scaleChanged)
                    {
                        // Multi-select move: use MoveMultipleEntitiesCommand
                        if (_editorState.SelectedEntities.Count > 1 && posChanged)
                        {
                            var delta = currentPos - _gizmoManager.DragStartPosition;
                            _commandHistory.Execute(new MoveMultipleEntitiesCommand(
                                _worldManager,
                                _gizmoManager.MultiDragEntities,
                                _gizmoManager.MultiDragStartPositions,
                                delta));
                        }
                        else if (_gizmoManager.CurrentMode == GizmoMode.BoundingBox)
                        {
                            _commandHistory.Execute(new TransformEntityCommand(
                                _worldManager, entity,
                                _gizmoManager.DragStartPosition, currentPos,
                                _gizmoManager.DragStartScale, currentScale));
                        }
                        else if (posChanged)
                        {
                            _commandHistory.Execute(new MoveEntityCommand(
                                _worldManager, entity,
                                _gizmoManager.DragStartPosition, currentPos));
                        }
                    }
                }
            }
        }

        // Transform propagation always runs
        _transformSystem.Update();

        // Animation system
        _animationSystem.Update(gameTime);
        _animationPanel.UpdatePreview((float)gameTime.ElapsedGameTime.TotalSeconds);

        // Poll asset database for file changes
        _assetDatabase.PollChanges();

        // Update title bar
        Window.Title = "MonoGame.Studio";

        _prevMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(40, 40, 40));

        // Start screen: only draw the start screen panel + settings
        if (_editorState.Phase == ApplicationPhase.StartScreen)
        {
            _imGui.BeginFrame(gameTime);
            _startScreen.Draw();
            _settingsPanel.Draw(ref _editorState.ShowSettings);
            _imGui.EndFrame();
            base.Draw(gameTime);
            return;
        }

        // Editor phase: full render pipeline
        bool isEditMode = _editorState.Mode == EditorMode.Edit;
        var vpSize = new Microsoft.Xna.Framework.Vector2(
            _viewportPanel.ViewportSize.X, _viewportPanel.ViewportSize.Y);

        // 1. Render scene to RT
        _viewportRenderer.Begin();

        _spriteBatch.Begin(
            SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null, null,
            _editorCamera.GetViewMatrix(vpSize));

        if (isEditMode)
            _gridRenderer.Draw(_spriteBatch, _editorCamera, vpSize);

        // Draw sprites
        _spriteRenderingSystem.Draw(_spriteBatch);

        // Draw entity markers (for entities without sprites)
        DrawEntityMarkers();

        // Draw collider outlines and virtual resolution overlay (edit mode only)
        if (isEditMode)
        {
            _spriteBatch.End();
            _spriteBatch.Begin(); // Screen space for overlays
            _colliderViz.Draw(_spriteBatch, vpSize);

            // Virtual resolution overlay
            if (_editorState.ShowVirtualResolution)
            {
                var res = _editorState.CurrentVirtualResolution;
                var halfW = res.Width * 0.5f;
                var halfH = res.Height * 0.5f;
                var camPos = _editorCamera.Position;
                var worldMin = new Microsoft.Xna.Framework.Vector2(camPos.X - halfW, camPos.Y - halfH);
                var worldMax = new Microsoft.Xna.Framework.Vector2(camPos.X + halfW, camPos.Y + halfH);
                var screenMin = _editorCamera.WorldToScreen(worldMin, vpSize);
                var screenMax = _editorCamera.WorldToScreen(worldMax, vpSize);
                _gizmoRenderer.DrawRectOutline(_spriteBatch, screenMin, screenMax,
                    new Color(255, 200, 0, 120), 2);
            }

            _spriteBatch.End();
            _spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null,
                _editorCamera.GetViewMatrix(vpSize));
        }

        // Draw gizmos
        if (isEditMode)
            _gizmoManager.Draw(_spriteBatch, vpSize);

        // Draw box selection
        if (_selectionSystem.IsBoxSelecting && isEditMode)
        {
            _spriteBatch.End();
            _spriteBatch.Begin(); // Screen space for box select
            _gizmoRenderer.DrawRectOutline(_spriteBatch,
                _selectionSystem.BoxStart, _selectionSystem.BoxEnd,
                new Color(100, 180, 255, 200), 1);
            _spriteBatch.End();
        }
        else
        {
            _spriteBatch.End();
        }

        // Gum UI renders in screen-space on the viewport RT
        _gumUIManager.UpdateCanvasSize(_viewportRenderer.Width, _viewportRenderer.Height);
        _gumUIManager.Draw();

        _viewportRenderer.End();

        // 2. Render backbuffer + ImGui
        _imGui.BeginFrame(gameTime);

        var imViewport = Hexa.NET.ImGui.ImGui.GetMainViewport();

        _menuBar.Draw();
        _toolbar.MenuBarOffset = _menuBar.UseNativeMenu ? 0f : Hexa.NET.ImGui.ImGui.GetFrameHeight();
        _toolbar.Draw();
        _dockingLayout.TopOffset = _toolbar.BottomY - imViewport.Pos.Y;
        _dockingLayout.SetupDockSpace();
        _hierarchy.Draw(ref _editorState.ShowHierarchy);
        _inspector.Draw(ref _editorState.ShowInspector);
        _viewportPanel.Draw(ref _editorState.ShowViewport);
        _console.Draw(ref _editorState.ShowConsole);
        _assetBrowser.Draw(ref _editorState.ShowAssetBrowser);
        _spriteSheetPanel.Draw(ref _editorState.ShowSpriteSheet);
        _animationPanel.Draw(ref _editorState.ShowAnimation);
        _settingsPanel.Draw(ref _editorState.ShowSettings);

        DrawSaveLayoutPopup();

        _imGui.EndFrame();

        base.Draw(gameTime);
    }

    private void DrawEntityMarkers()
    {
        var world = _worldManager.World;
        var query = new Arch.Core.QueryDescription().WithAll<
            MonoGameStudio.Core.Components.Position,
            MonoGameStudio.Core.Components.EntityName>();

        world.Query(query, (Arch.Core.Entity entity,
            ref MonoGameStudio.Core.Components.Position pos) =>
        {
            bool selected = _editorState.IsSelected(entity);
            var color = selected ? Color.Yellow : new Color(100, 180, 255);
            var worldPos = new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y);

            // Draw a small diamond marker
            float size = 6f / _editorCamera.Zoom;
            _spriteBatch.Draw(
                GetPixelTexture(),
                new Rectangle(
                    (int)(worldPos.X - size / 2),
                    (int)(worldPos.Y - size / 2),
                    (int)size, (int)size),
                color);
        });
    }

    private Texture2D? _pixelTexture;
    private Texture2D GetPixelTexture()
    {
        if (_pixelTexture == null)
        {
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
        return _pixelTexture;
    }

    // === Scene operations ===

    private void NewScene()
    {
        _worldManager.ResetWorld();
        _editorState.ClearSelection();
        _editorState.CurrentScenePath = null;
        _editorState.IsDirty = false;
        _commandHistory.Clear();
        _gumUIManager.ClearUI();
        Log.Info("New scene created");
    }

    private void SaveScene()
    {
        if (_editorState.CurrentScenePath != null)
        {
            SceneSerializer.SaveToFile(_editorState.CurrentScenePath, _worldManager);
            _editorState.IsDirty = false;

            // Persist camera position to project settings
            if (_projectManager.IsProjectOpen)
            {
                _projectManager.UpdateCameraSettings(
                    _editorCamera.Position.X, _editorCamera.Position.Y, _editorCamera.Zoom);
                _projectManager.SaveProjectSettings();
            }
        }
        else
        {
            SaveSceneAs();
        }
    }

    private void SaveSceneAs()
    {
        string dir;
        if (_projectManager.IsProjectOpen)
            dir = Path.Combine(_projectManager.CurrentProject!.ProjectDirectory, "Scenes");
        else
            dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenes");

        Directory.CreateDirectory(dir);

        var sceneFilter = new FileFilter("Scene Files", [".json"]);
        var path = _fileDialogs.SaveFileDialog("Save Scene As", "scene.json", dir, [sceneFilter]);
        if (path == null) return;

        SceneSerializer.SaveToFile(path, _worldManager);
        _editorState.CurrentScenePath = path;
        _editorState.IsDirty = false;
    }

    private void OpenScene()
    {
        string? scenesDir = null;
        if (_projectManager.IsProjectOpen)
        {
            scenesDir = Path.Combine(_projectManager.CurrentProject!.ProjectDirectory, "Scenes");
            if (!Directory.Exists(scenesDir))
                scenesDir = _projectManager.CurrentProject.ProjectDirectory;
        }

        var sceneFilter = new FileFilter("Scene Files", [".json"]);
        var path = _fileDialogs.OpenFileDialog("Open Scene", scenesDir, [sceneFilter]);
        if (path == null || !File.Exists(path)) return;

        SceneSerializer.LoadFromFile(path, _worldManager);
        _editorState.CurrentScenePath = path;
        _editorState.IsDirty = false;
        _editorState.ClearSelection();
        _commandHistory.Clear();
        _gumUISystem.OnSceneLoaded();
    }

    private void DeleteSelected()
    {
        foreach (var entity in _editorState.SelectedEntities.ToArray())
        {
            if (_worldManager.World.IsAlive(entity))
            {
                var cmd = new DeleteEntityCommand(_worldManager, entity);
                _commandHistory.Execute(cmd);
            }
        }
        _editorState.ClearSelection();
    }

    private void DuplicateSelected()
    {
        var primary = _editorState.PrimarySelection;
        if (!primary.HasValue || !_worldManager.World.IsAlive(primary.Value)) return;

        var dup = _worldManager.DuplicateEntity(primary.Value);
        _editorState.Select(dup);
        _editorState.IsDirty = true;
    }

    // === Project lifecycle ===

    private void OnProjectOpened(ProjectInfo project)
    {
        _editorState.Phase = ApplicationPhase.Editor;

        // Load default scene if it exists
        var scenePath = _projectManager.GetDefaultScenePath();
        if (scenePath != null)
        {
            SceneSerializer.LoadFromFile(scenePath, _worldManager);
            _editorState.CurrentScenePath = scenePath;
            _editorState.IsDirty = false;
            _editorState.ClearSelection();
            _commandHistory.Clear();
            _gumUISystem.OnSceneLoaded();
        }
        else
        {
            NewScene();
        }

        // Restore camera settings
        _editorCamera.Position = new Microsoft.Xna.Framework.Vector2(
            project.EditorSettings.CameraX,
            project.EditorSettings.CameraY);
        _editorCamera.Zoom = project.EditorSettings.CameraZoom;

        // Point asset browser at project directory
        _assetBrowser.SetProjectRoot(project.ProjectDirectory);

        Window.Title = $"MonoGameStudio — {project.Name}";
    }

    private void OnProjectClosed()
    {
        _worldManager.ResetWorld();
        _editorState.ClearSelection();
        _editorState.CurrentScenePath = null;
        _editorState.IsDirty = false;
        _editorState.Mode = EditorMode.Edit;
        _editorState.Phase = ApplicationPhase.StartScreen;
        _commandHistory.Clear();
        _gumUIManager.ClearUI();
        _assetBrowser.SetProjectRoot(null);
        _textureCache.Clear();
        Window.Title = "MonoGame.Studio";
    }

    private void HandleNativeMenuAction(string action)
    {
        // Handle dynamic layout actions (LoadLayout:N, DeleteLayout:N)
        if (action.StartsWith("LoadLayout:") && int.TryParse(action["LoadLayout:".Length..], out var loadIndex))
        {
            var profiles = _layoutProfileManager.ProfileNames;
            if (loadIndex >= 0 && loadIndex < profiles.Count)
                _layoutProfileManager.LoadProfile(profiles[loadIndex]);
            return;
        }

        if (action.StartsWith("DeleteLayout:") && int.TryParse(action["DeleteLayout:".Length..], out var deleteIndex))
        {
            var deletable = _layoutProfileManager.GetDeletableProfiles();
            if (deleteIndex >= 0 && deleteIndex < deletable.Count)
                _layoutProfileManager.DeleteProfile(deletable[deleteIndex]);
            return;
        }

        switch (action)
        {
            case "NewScene": NewScene(); break;
            case "OpenScene": OpenScene(); break;
            case "SaveScene": SaveScene(); break;
            case "SaveSceneAs": SaveSceneAs(); break;
            case "CloseProject": _projectManager.CloseProject(); break;
            case "Undo": _commandHistory.Undo(); break;
            case "Redo": _commandHistory.Redo(); break;
            case "SaveLayout": _showSaveLayoutPopup = true; _saveLayoutName = ""; break;
            case "ToggleHierarchy": _editorState.ShowHierarchy = !_editorState.ShowHierarchy; break;
            case "ToggleInspector": _editorState.ShowInspector = !_editorState.ShowInspector; break;
            case "ToggleViewport": _editorState.ShowViewport = !_editorState.ShowViewport; break;
            case "ToggleConsole": _editorState.ShowConsole = !_editorState.ShowConsole; break;
            case "ToggleAssetBrowser": _editorState.ShowAssetBrowser = !_editorState.ShowAssetBrowser; break;
            case "ToggleSettings": _editorState.ShowSettings = !_editorState.ShowSettings; break;
            case "ChangeTheme": Theme.ShowThemeSelector("Select a Theme"); break;
        }
    }

    private void HandleNativeToolbarAction(string action)
    {
        switch (action)
        {
            case "GizmoNone": _gizmoManager.CurrentMode = GizmoMode.None; break;
            case "GizmoBoundingBox": _gizmoManager.CurrentMode = GizmoMode.BoundingBox; break;
            case "GizmoMove": _gizmoManager.CurrentMode = GizmoMode.Move; break;
            case "GizmoRotate": _gizmoManager.CurrentMode = GizmoMode.Rotate; break;
            case "GizmoScale": _gizmoManager.CurrentMode = GizmoMode.Scale; break;
            case "PlayPause":
                if (_editorState.Mode == EditorMode.Play)
                    _playModeManager.Pause();
                else
                    _playModeManager.Play();
                break;
            case "Stop": _playModeManager.Stop(); break;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private void RebuildNativeLayoutsMenu() => MacMenuBar.RebuildLayoutsMenu(_layoutProfileManager);

    private void DrawSaveLayoutPopup()
    {
        if (_showSaveLayoutPopup)
        {
            Hexa.NET.ImGui.ImGui.OpenPopup("Save Layout");
            _showSaveLayoutPopup = false;
        }

        var vp = Hexa.NET.ImGui.ImGui.GetMainViewport();
        var center = new System.Numerics.Vector2(
            vp.Pos.X + vp.Size.X * 0.5f,
            vp.Pos.Y + vp.Size.Y * 0.5f);
        Hexa.NET.ImGui.ImGui.SetNextWindowPos(center, Hexa.NET.ImGui.ImGuiCond.Appearing,
            new System.Numerics.Vector2(0.5f, 0.5f));

        if (Hexa.NET.ImGui.ImGui.BeginPopupModal("Save Layout", Hexa.NET.ImGui.ImGuiWindowFlags.AlwaysAutoResize))
        {
            Hexa.NET.ImGui.ImGui.Text("Layout name:");
            Hexa.NET.ImGui.ImGui.SetNextItemWidth(300);

            bool enterPressed = Hexa.NET.ImGui.ImGui.InputText("##layoutName", ref _saveLayoutName, 128,
                Hexa.NET.ImGui.ImGuiInputTextFlags.EnterReturnsTrue);

            if (Hexa.NET.ImGui.ImGui.IsWindowAppearing())
                Hexa.NET.ImGui.ImGui.SetKeyboardFocusHere(-1);

            bool validName = !string.IsNullOrWhiteSpace(_saveLayoutName) && _saveLayoutName != "Default";

            if (!validName)
                Hexa.NET.ImGui.ImGui.BeginDisabled();

            if (Hexa.NET.ImGui.ImGui.Button("Save", new System.Numerics.Vector2(120, 0)) || (enterPressed && validName))
            {
                _layoutProfileManager.SaveCurrentLayout(_saveLayoutName.Trim());
                Hexa.NET.ImGui.ImGui.CloseCurrentPopup();
            }

            if (!validName)
                Hexa.NET.ImGui.ImGui.EndDisabled();

            Hexa.NET.ImGui.ImGui.SameLine();

            if (Hexa.NET.ImGui.ImGui.Button("Cancel", new System.Numerics.Vector2(120, 0)))
            {
                Hexa.NET.ImGui.ImGui.CloseCurrentPopup();
            }

            Hexa.NET.ImGui.ImGui.EndPopup();
        }
    }

    private void SaveEntityAsPrefab(Arch.Core.Entity entity)
    {
        if (!_worldManager.World.IsAlive(entity)) return;

        var name = _worldManager.World.Get<MonoGameStudio.Core.Components.EntityName>(entity).Name;
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

        string dir;
        if (_projectManager.IsProjectOpen)
            dir = Path.Combine(_projectManager.CurrentProject!.ProjectDirectory, "Prefabs");
        else
            dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prefabs");

        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{safeName}.prefab.json");
        PrefabSerializer.SaveToFile(path, _worldManager, entity);
    }

    private void InstantiatePrefabAtPosition(string prefabPath, Microsoft.Xna.Framework.Vector2 worldPos)
    {
        var root = PrefabSerializer.InstantiateFromFile(prefabPath, _worldManager, worldPos);
        if (root.HasValue)
        {
            _editorState.Select(root.Value);
            _editorState.IsDirty = true;
        }
    }

    private void PasteFromClipboard()
    {
        if (!_clipboardManager.HasClipboard) return;
        var pasted = _clipboardManager.Paste();
        if (pasted.Count > 0)
        {
            _editorState.ClearSelection();
            foreach (var entity in pasted)
                _editorState.AddToSelection(entity);
            _editorState.IsDirty = true;
        }
    }

    private void FocusSelected()
    {
        var primary = _editorState.PrimarySelection;
        if (!primary.HasValue || !_worldManager.World.IsAlive(primary.Value)) return;

        var pos = _worldManager.World.Get<MonoGameStudio.Core.Components.Position>(primary.Value);
        _editorCamera.Position = new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y);
    }

    private void OnAssetDroppedOnViewport(string assetPath, System.Numerics.Vector2 localPos)
    {
        var vpSize = new Microsoft.Xna.Framework.Vector2(
            _viewportPanel.ViewportSize.X, _viewportPanel.ViewportSize.Y);
        var worldPos = _editorCamera.ScreenToWorld(
            new Microsoft.Xna.Framework.Vector2(localPos.X, localPos.Y), vpSize);

        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        var ext = Path.GetExtension(assetPath).ToLowerInvariant();

        // Handle prefab files
        if (assetPath.EndsWith(".prefab.json", StringComparison.OrdinalIgnoreCase))
        {
            InstantiatePrefabAtPosition(assetPath, worldPos);
            return;
        }

        // Handle texture files — create entity with SpriteRenderer
        if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga")
        {
            var entity = _worldManager.CreateEntity(fileName);
            _worldManager.World.Set(entity, new MonoGameStudio.Core.Components.Position(worldPos.X, worldPos.Y));

            // Make path relative to project if possible
            var texturePath = assetPath;
            if (_projectManager.IsProjectOpen)
            {
                var projectDir = _projectManager.CurrentProject!.ProjectDirectory;
                if (assetPath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                    texturePath = Path.GetRelativePath(projectDir, assetPath);
            }

            var sprite = new MonoGameStudio.Core.Components.SpriteRenderer
            {
                TexturePath = texturePath,
                Tint = Color.White,
                Opacity = 1f
            };
            _worldManager.World.Add(entity, sprite);
            _editorState.Select(entity);
            _editorState.IsDirty = true;
            Log.Info($"Created sprite entity '{fileName}' at ({worldPos.X:F0}, {worldPos.Y:F0})");
        }
    }
}
