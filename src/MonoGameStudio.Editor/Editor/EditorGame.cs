using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Core.Systems;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Commands;
using MonoGameStudio.Editor.Gizmos;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;
using MonoGameStudio.Editor.Panels;
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

    // Commands / Shortcuts
    private CommandHistory _commandHistory = null!;
    private ShortcutManager _shortcutManager = null!;
    private PlayModeManager _playModeManager = null!;

    private MouseState _prevMouse;

    public EditorGame()
    {
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
        Window.Title = "MonoGameStudio";
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

        // Commands
        _commandHistory = new CommandHistory();
        _shortcutManager = new ShortcutManager();
        _playModeManager = new PlayModeManager(_worldManager, _editorState);

        // Panels (constructed after dependencies)
        _menuBar = new MenuBarPanel();
        _toolbar = new ToolbarPanel();
        _hierarchy = new SceneHierarchyPanel(_worldManager, _editorState);
        _inspector = new InspectorPanel(_worldManager, _editorState);
        _console = new ConsolePanel();
        _assetBrowser = new AssetBrowserPanel();

        // Wire menu events
        _menuBar.OnNewScene += NewScene;
        _menuBar.OnSaveScene += SaveScene;
        _menuBar.OnSaveSceneAs += SaveSceneAs;
        _menuBar.OnOpenScene += OpenScene;
        _menuBar.OnUndo += () => _commandHistory.Undo();
        _menuBar.OnRedo += () => _commandHistory.Redo();
        _menuBar.OnResetLayout += () => _dockingLayout.ResetLayout();

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
        _shortcutManager.OnGizmoMove += () => _gizmoManager.CurrentMode = GizmoMode.Move;
        _shortcutManager.OnGizmoRotate += () => _gizmoManager.CurrentMode = GizmoMode.Rotate;
        _shortcutManager.OnGizmoScale += () => _gizmoManager.CurrentMode = GizmoMode.Scale;
        _shortcutManager.OnFocusSelected += FocusSelected;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imgui.ini");
        _imGui.Initialize(iniPath);

        _viewportRenderer = new ViewportRenderer(GraphicsDevice, _imGui);
        _viewportPanel = new GameViewportPanel(_viewportRenderer);

        _gridRenderer.LoadContent(GraphicsDevice);
        _gizmoRenderer.LoadContent(GraphicsDevice);

        Log.Info("MonoGameStudio loaded and ready.");
    }

    protected override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        _shortcutManager.Update(keyboard);

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
        if (!ImGuiNET.ImGui.GetIO().WantCaptureMouse && vpHovered && isEditMode)
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
                    if (currentPos != _gizmoManager.DragStartPosition)
                    {
                        _commandHistory.Execute(new MoveEntityCommand(
                            _worldManager, entity,
                            _gizmoManager.DragStartPosition, currentPos));
                    }
                }
            }
        }

        // Transform propagation always runs
        _transformSystem.Update();

        // Update title bar
        var title = "MonoGameStudio";
        if (_editorState.CurrentScenePath != null)
            title += $" - {Path.GetFileName(_editorState.CurrentScenePath)}";
        if (_editorState.IsDirty)
            title += "*";
        if (_editorState.Mode != EditorMode.Edit)
            title += $" [{_editorState.Mode}]";
        Window.Title = title;

        _prevMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
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

        // Draw entity markers
        DrawEntityMarkers();

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

        _viewportRenderer.End();

        // 2. Render backbuffer + ImGui
        GraphicsDevice.Clear(new Color(40, 40, 40));

        _imGui.BeginFrame(gameTime);

        _dockingLayout.SetupDockSpace();
        _menuBar.Draw();
        _toolbar.Draw();
        _hierarchy.Draw();
        _inspector.Draw();
        _viewportPanel.Draw();
        _console.Draw();
        _assetBrowser.Draw();

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
        Log.Info("New scene created");
    }

    private void SaveScene()
    {
        if (_editorState.CurrentScenePath != null)
        {
            SceneSerializer.SaveToFile(_editorState.CurrentScenePath, _worldManager);
            _editorState.IsDirty = false;
        }
        else
        {
            SaveSceneAs();
        }
    }

    private void SaveSceneAs()
    {
        // Simple approach: save to a default location
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenes");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "scene.json");
        SceneSerializer.SaveToFile(path, _worldManager);
        _editorState.CurrentScenePath = path;
        _editorState.IsDirty = false;
    }

    private void OpenScene()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenes");
        var path = Path.Combine(dir, "scene.json");
        if (File.Exists(path))
        {
            SceneSerializer.LoadFromFile(path, _worldManager);
            _editorState.CurrentScenePath = path;
            _editorState.IsDirty = false;
            _editorState.ClearSelection();
            _commandHistory.Clear();
        }
        else
        {
            Log.Warn("No scene file found to open");
        }
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

    private void FocusSelected()
    {
        var primary = _editorState.PrimarySelection;
        if (!primary.HasValue || !_worldManager.World.IsAlive(primary.Value)) return;

        var pos = _worldManager.World.Get<MonoGameStudio.Core.Components.Position>(primary.Value);
        _editorCamera.Position = new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y);
    }
}
