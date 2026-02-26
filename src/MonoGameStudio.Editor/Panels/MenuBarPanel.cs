using Hexa.NET.ImGui;
using ktsu.ImGuiStyler;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class MenuBarPanel
{
    private readonly EditorState _editorState;
    private bool _showAbout;

    public event Action? OnNewScene;
    public event Action? OnOpenScene;
    public event Action? OnSaveScene;
    public event Action? OnSaveSceneAs;
    public event Action? OnCloseProject;
    public event Action? OnUndo;
    public event Action? OnRedo;
    public event Action<string>? OnLoadLayout;
    public event Action<string>? OnDeleteLayout;
    public event Action? OnSaveLayoutRequested;

    public bool UseNativeMenu { get; set; }
    public LayoutProfileManager? LayoutProfileManager { get; set; }

    public MenuBarPanel(EditorState editorState)
    {
        _editorState = editorState;
    }

    public void Draw()
    {
        if (UseNativeMenu) return;

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New Scene", "Ctrl+N")) OnNewScene?.Invoke();
                if (ImGui.MenuItem("Open Scene...", "Ctrl+O")) OnOpenScene?.Invoke();
                ImGui.Separator();
                if (ImGui.MenuItem("Save Scene", "Ctrl+S")) OnSaveScene?.Invoke();
                if (ImGui.MenuItem("Save Scene As...", "Ctrl+Shift+S")) OnSaveSceneAs?.Invoke();
                ImGui.Separator();
                if (ImGui.MenuItem("Close Project")) OnCloseProject?.Invoke();
                ImGui.Separator();
                if (ImGui.MenuItem("Exit", "Alt+F4")) Environment.Exit(0);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo", "Ctrl+Z")) OnUndo?.Invoke();
                if (ImGui.MenuItem("Redo", "Ctrl+Shift+Z")) OnRedo?.Invoke();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Hierarchy", (string?)null, ref _editorState.ShowHierarchy);
                ImGui.MenuItem("Inspector", (string?)null, ref _editorState.ShowInspector);
                ImGui.MenuItem("Game Viewport", (string?)null, ref _editorState.ShowViewport);
                ImGui.MenuItem("Console", (string?)null, ref _editorState.ShowConsole);
                ImGui.MenuItem("Asset Browser", (string?)null, ref _editorState.ShowAssetBrowser);
                ImGui.Separator();
                ImGui.MenuItem("Sprite Sheet", (string?)null, ref _editorState.ShowSpriteSheet);
                ImGui.MenuItem("Animation", (string?)null, ref _editorState.ShowAnimation);
                ImGui.MenuItem("Tilemap Editor", (string?)null, ref _editorState.ShowTilemapEditor);
                ImGui.MenuItem("Particle Editor", (string?)null, ref _editorState.ShowParticleEditor);
                ImGui.Separator();
                ImGui.MenuItem("Collision Matrix", (string?)null, ref _editorState.ShowCollisionMatrix);
                ImGui.MenuItem("Shader Preview", (string?)null, ref _editorState.ShowShaderPreview);
                ImGui.MenuItem("Post Processing", (string?)null, ref _editorState.ShowPostProcess);
                ImGui.MenuItem("Game Run", (string?)null, ref _editorState.ShowGameRun);
                ImGui.Separator();
                ImGui.MenuItem("Settings", (string?)null, ref _editorState.ShowSettings);
                ImGui.Separator();

                DrawLayoutsSubmenu();

                ImGui.Separator();
                if (ImGui.MenuItem("Change Theme..."))
                    Theme.ShowThemeSelector("Select a Theme");
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("About"))
                {
                    ImGui.OpenPopup("About MonoGameStudio");
                    _showAbout = true;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        DrawAboutPopup();
    }

    private void DrawAboutPopup()
    {
        if (!_showAbout) return;

        if (ImGui.BeginPopupModal("About MonoGameStudio", ref _showAbout, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("MonoGameStudio");
            ImGui.Text("Version 0.1");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("A 2D game editor built with MonoGame + Arch ECS + ImGui");
            ImGui.Spacing();
            ImGui.TextDisabled("MonoGame 3.8 | Arch ECS 2.1 | Hexa.NET.ImGui");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float buttonWidth = 120;
            ImGui.SetCursorPosX((ImGui.GetWindowSize().X - buttonWidth) * 0.5f);
            if (ImGui.Button("OK", new System.Numerics.Vector2(buttonWidth, 0)))
            {
                _showAbout = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawLayoutsSubmenu()
    {
        if (ImGui.BeginMenu("Layouts"))
        {
            if (LayoutProfileManager != null)
            {
                var profiles = LayoutProfileManager.ProfileNames;
                var active = LayoutProfileManager.ActiveProfile;

                foreach (var name in profiles)
                {
                    bool isActive = name == active;
                    if (ImGui.MenuItem(name, (string?)null, isActive))
                    {
                        OnLoadLayout?.Invoke(name);
                    }
                }

                ImGui.Separator();
                if (ImGui.MenuItem("Save Layout..."))
                {
                    OnSaveLayoutRequested?.Invoke();
                }

                var deletable = LayoutProfileManager.GetDeletableProfiles();
                if (deletable.Count > 0 && ImGui.BeginMenu("Delete Layout"))
                {
                    foreach (var name in deletable)
                    {
                        if (ImGui.MenuItem(name))
                        {
                            OnDeleteLayout?.Invoke(name);
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            else
            {
                if (ImGui.MenuItem("Default", (string?)null, true)) { }
            }

            ImGui.EndMenu();
        }
    }
}
