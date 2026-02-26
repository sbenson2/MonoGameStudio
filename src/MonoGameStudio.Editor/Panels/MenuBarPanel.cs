using Hexa.NET.ImGui;
using ktsu.ImGuiStyler;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class MenuBarPanel
{
    private readonly EditorState _editorState;

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
                    // TODO: About popup
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
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
