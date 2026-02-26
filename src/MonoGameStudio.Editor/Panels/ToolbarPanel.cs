using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Editor.ImGuiIntegration;

namespace MonoGameStudio.Editor.Panels;

public class ToolbarPanel
{
    public EditorMode CurrentMode { get; set; } = EditorMode.Edit;
    public GizmoMode CurrentGizmoMode { get; set; } = GizmoMode.BoundingBox;

    /// <summary>Offset from viewport top for non-native menu bar (ImGui menu bar height).</summary>
    public float MenuBarOffset { get; set; }

    /// <summary>Left padding to clear macOS traffic light buttons.</summary>
    public float LeftPadding { get; set; }

    /// <summary>The bottom Y position of the toolbar in screen coordinates (set after Draw).</summary>
    public float BottomY { get; private set; }

    public event Action? OnPlay;
    public event Action? OnPause;
    public event Action? OnStop;

    private const float ToolbarHeightPx = 42f;

    public ToolbarPanel()
    {
        // Default BottomY for first frame before Draw() runs
        BottomY = MenuBarOffset + ToolbarHeightPx;
    }

    public void Draw()
    {
        var viewport = ImGui.GetMainViewport();
        float topY = viewport.Pos.Y + MenuBarOffset;

        ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X, topY));
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, ToolbarHeightPx));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 5));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

        var flags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoBringToFrontOnFocus;

        if (ImGui.Begin("##Toolbar", flags))
        {
            float windowWidth = ImGui.GetWindowWidth();
            float contentStartY = ImGui.GetCursorPosY();

            // === LEFT SECTION: Title + Gizmo tools ===
            if (LeftPadding > 0)
                ImGui.SetCursorPosX(LeftPadding);

            DrawGizmoButton($"{FontAwesomeIcons.MousePointer}##Q", GizmoMode.None);
            ImGui.SameLine();
            DrawGizmoButton($"{FontAwesomeIcons.VectorSquare}##W", GizmoMode.BoundingBox);
            ImGui.SameLine();
            DrawGizmoButton($"{FontAwesomeIcons.ArrowsAlt}##T", GizmoMode.Move);
            ImGui.SameLine();
            DrawGizmoButton($"{FontAwesomeIcons.SyncAlt}##E", GizmoMode.Rotate);
            ImGui.SameLine();
            DrawGizmoButton($"{FontAwesomeIcons.ExpandAlt}##R", GizmoMode.Scale);

            // === RIGHT SECTION: Play controls ===
            float playButtonWidth = ImGui.CalcTextSize(FontAwesomeIcons.Play).X + ImGui.GetStyle().FramePadding.X * 2;
            float pauseButtonWidth = ImGui.CalcTextSize(FontAwesomeIcons.Pause).X + ImGui.GetStyle().FramePadding.X * 2;
            float stopButtonWidth = ImGui.CalcTextSize(FontAwesomeIcons.Stop).X + ImGui.GetStyle().FramePadding.X * 2;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float playPauseWidth = MathF.Max(playButtonWidth, pauseButtonWidth);
            float rightSectionWidth = playPauseWidth + spacing + stopButtonWidth + 8f;
            float rightSectionStart = windowWidth - rightSectionWidth;

            ImGui.SameLine();
            ImGui.SetCursorPosX(rightSectionStart);
            ImGui.SetCursorPosY(contentStartY);

            bool isPlaying = CurrentMode == EditorMode.Play;
            bool isPaused = CurrentMode == EditorMode.Pause;

            if (isPlaying || isPaused)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1f));

            if (ImGui.Button(isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play))
            {
                if (isPlaying) OnPause?.Invoke();
                else OnPlay?.Invoke();
            }

            if (isPlaying || isPaused) ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGui.Button(FontAwesomeIcons.Stop)) OnStop?.Invoke();

            // Bottom separator line
            var drawList = ImGui.GetWindowDrawList();
            var winPos = ImGui.GetWindowPos();
            float winHeight = ImGui.GetWindowHeight();
            float winWidth = ImGui.GetWindowWidth();
            drawList.AddLine(
                new Vector2(winPos.X, winPos.Y + winHeight - 1),
                new Vector2(winPos.X + winWidth, winPos.Y + winHeight - 1),
                ImGui.GetColorU32(ImGuiCol.Border), 2f);

            BottomY = winPos.Y + winHeight;
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawGizmoButton(string label, GizmoMode mode)
    {
        bool isActive = CurrentGizmoMode == mode;
        if (isActive)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.5f, 0.8f, 1f));

        if (ImGui.Button(label))
            CurrentGizmoMode = mode;

        if (isActive)
            ImGui.PopStyleColor();
    }
}

public enum GizmoMode
{
    None,
    Move,
    Rotate,
    Scale,
    BoundingBox
}
