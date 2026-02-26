using System.Numerics;
using Hexa.NET.ImGui;

namespace MonoGameStudio.Editor.Layout;

public class DockingLayout
{
    private bool _firstTime = true;
    private uint _dockspaceId;

    /// <summary>Vertical offset from viewport top (toolbar height).</summary>
    public float TopOffset { get; set; }

    public void SetupDockSpace()
    {
        var viewport = ImGui.GetMainViewport();

        var hostPos = new Vector2(viewport.Pos.X, viewport.Pos.Y + TopOffset);
        var hostSize = new Vector2(viewport.Size.X, viewport.Size.Y - TopOffset);

        ImGui.SetNextWindowPos(hostPos);
        ImGui.SetNextWindowSize(hostSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        var hostFlags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoNavFocus
            | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoBackground;

        ImGui.Begin("##DockSpaceHost", hostFlags);
        ImGui.PopStyleVar(3);

        _dockspaceId = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(_dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);

        ImGui.End();

        if (_firstTime)
        {
            _firstTime = false;
            BuildDefaultLayout(hostSize);
        }
    }

    public void ResetLayout()
    {
        _firstTime = true;
    }

    private unsafe void BuildDefaultLayout(Vector2 availableSize)
    {
        ImGuiP.DockBuilderRemoveNode(_dockspaceId);
        ImGuiP.DockBuilderAddNode(_dockspaceId, ImGuiDockNodeFlags.None);
        ImGuiP.DockBuilderSetNodeSize(_dockspaceId, availableSize);

        // Split: left 20% | center+bottom | right 25%
        uint dockLeft, dockCenter, dockRight;
        ImGuiP.DockBuilderSplitNode(_dockspaceId, ImGuiDir.Left, 0.20f, &dockLeft, &dockCenter);
        ImGuiP.DockBuilderSplitNode(dockCenter, ImGuiDir.Right, 0.25f, &dockRight, &dockCenter);

        // Split left into hierarchy (top 60%) and assets (bottom 40%)
        uint dockLeftBottom, dockLeftTop;
        ImGuiP.DockBuilderSplitNode(dockLeft, ImGuiDir.Down, 0.40f, &dockLeftBottom, &dockLeftTop);

        // Split center: bottom 25% for console
        uint dockBottom;
        ImGuiP.DockBuilderSplitNode(dockCenter, ImGuiDir.Down, 0.25f, &dockBottom, &dockCenter);

        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.Hierarchy, dockLeftTop);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.AssetBrowser, dockLeftBottom);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.Inspector, dockRight);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.Console, dockBottom);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.SpriteSheetEditor, dockBottom);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.Animation, dockBottom);
        ImGuiP.DockBuilderDockWindow(LayoutDefinitions.Viewport, dockCenter);

        ImGuiP.DockBuilderFinish(_dockspaceId);
    }
}
