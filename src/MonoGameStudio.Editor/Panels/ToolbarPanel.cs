using ImGuiNET;
using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Editor.Panels;

public class ToolbarPanel
{
    public EditorMode CurrentMode { get; set; } = EditorMode.Edit;
    public GizmoMode CurrentGizmoMode { get; set; } = GizmoMode.Move;

    public event Action? OnPlay;
    public event Action? OnPause;
    public event Action? OnStop;

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(8, 4));
        if (ImGui.Begin("##Toolbar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            // Gizmo mode buttons
            DrawGizmoButton("Q", GizmoMode.None);
            ImGui.SameLine();
            DrawGizmoButton("W", GizmoMode.Move);
            ImGui.SameLine();
            DrawGizmoButton("E", GizmoMode.Rotate);
            ImGui.SameLine();
            DrawGizmoButton("R", GizmoMode.Scale);

            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();

            // Play mode buttons
            bool isPlaying = CurrentMode == EditorMode.Play;
            bool isPaused = CurrentMode == EditorMode.Pause;

            if (isPlaying || isPaused)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f));
            }
            if (ImGui.Button(isPlaying ? "||" : ">"))
            {
                if (isPlaying) OnPause?.Invoke();
                else OnPlay?.Invoke();
            }
            if (isPlaying || isPaused) ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGui.Button("[]")) OnStop?.Invoke();
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawGizmoButton(string label, GizmoMode mode)
    {
        if (CurrentGizmoMode == mode)
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.3f, 0.5f, 0.8f, 1f));

        if (ImGui.Button(label))
            CurrentGizmoMode = mode;

        if (CurrentGizmoMode == mode)
            ImGui.PopStyleColor();
    }
}

public enum GizmoMode
{
    None,
    Move,
    Rotate,
    Scale
}
