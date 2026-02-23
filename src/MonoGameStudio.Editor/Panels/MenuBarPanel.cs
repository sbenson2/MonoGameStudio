using ImGuiNET;

namespace MonoGameStudio.Editor.Panels;

public class MenuBarPanel
{
    public event Action? OnNewScene;
    public event Action? OnOpenScene;
    public event Action? OnSaveScene;
    public event Action? OnSaveSceneAs;
    public event Action? OnUndo;
    public event Action? OnRedo;
    public event Action? OnResetLayout;

    public void Draw()
    {
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
                if (ImGui.MenuItem("Reset Layout")) OnResetLayout?.Invoke();
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
}
