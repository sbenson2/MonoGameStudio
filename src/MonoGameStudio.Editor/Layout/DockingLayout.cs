using ImGuiNET;
using MonoGameStudio.Editor.ImGuiIntegration;

namespace MonoGameStudio.Editor.Layout;

public class DockingLayout
{
    private bool _firstTime = true;
    private uint _dockspaceId;

    public void SetupDockSpace()
    {
        _dockspaceId = ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        if (_firstTime)
        {
            _firstTime = false;
            BuildDefaultLayout();
        }
    }

    public void ResetLayout()
    {
        _firstTime = true;
    }

    private void BuildDefaultLayout()
    {
        ImGuiDockBuilder.RemoveNode(_dockspaceId);
        ImGuiDockBuilder.AddNode(_dockspaceId, isDockSpace: true);
        ImGuiDockBuilder.SetNodeSize(_dockspaceId, ImGui.GetMainViewport().Size);

        // Split: left 20% | center+bottom | right 25%
        ImGuiDockBuilder.SplitNode(_dockspaceId, ImGuiDir.Left, 0.20f, out uint dockLeft, out uint dockCenter);
        ImGuiDockBuilder.SplitNode(dockCenter, ImGuiDir.Right, 0.25f, out uint dockRight, out dockCenter);

        // Split left into hierarchy (top 60%) and assets (bottom 40%)
        ImGuiDockBuilder.SplitNode(dockLeft, ImGuiDir.Down, 0.40f, out uint dockLeftBottom, out uint dockLeftTop);

        // Split center: bottom 25% for console
        ImGuiDockBuilder.SplitNode(dockCenter, ImGuiDir.Down, 0.25f, out uint dockBottom, out dockCenter);

        ImGuiDockBuilder.DockWindow(LayoutDefinitions.Hierarchy, dockLeftTop);
        ImGuiDockBuilder.DockWindow(LayoutDefinitions.AssetBrowser, dockLeftBottom);
        ImGuiDockBuilder.DockWindow(LayoutDefinitions.Inspector, dockRight);
        ImGuiDockBuilder.DockWindow(LayoutDefinitions.Console, dockBottom);
        ImGuiDockBuilder.DockWindow(LayoutDefinitions.Viewport, dockCenter);

        ImGuiDockBuilder.Finish(_dockspaceId);
    }
}
