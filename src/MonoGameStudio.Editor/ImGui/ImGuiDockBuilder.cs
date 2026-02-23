using System.Runtime.InteropServices;
using System.Numerics;
using ImGuiNET;

namespace MonoGameStudio.Editor.ImGuiIntegration;

/// <summary>
/// P/Invoke bindings for DockBuilder functions not exposed by ImGui.NET.
/// </summary>
public static class ImGuiDockBuilder
{
    private const string CImGuiLib = "cimgui";

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderDockWindow")]
    private static extern void igDockBuilderDockWindow(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string window_name, uint node_id);

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderAddNode")]
    private static extern uint igDockBuilderAddNode(uint node_id, int flags);

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderRemoveNode")]
    private static extern void igDockBuilderRemoveNode(uint node_id);

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderSetNodeSize")]
    private static extern void igDockBuilderSetNodeSize(uint node_id, Vector2 size);

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderSplitNode")]
    private static extern uint igDockBuilderSplitNode(
        uint node_id, int split_dir, float size_ratio_for_node_at_dir,
        out uint out_id_at_dir, out uint out_id_at_opposite_dir);

    [DllImport(CImGuiLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igDockBuilderFinish")]
    private static extern void igDockBuilderFinish(uint node_id);

    // ImGuiDockNodeFlags_DockSpace = 1 << 2 = 4
    private const int DockNodeFlagsDockSpace = 1 << 2;

    public static void DockWindow(string windowName, uint nodeId) =>
        igDockBuilderDockWindow(windowName, nodeId);

    public static uint AddNode(uint nodeId, bool isDockSpace = false) =>
        igDockBuilderAddNode(nodeId, isDockSpace ? DockNodeFlagsDockSpace : 0);

    public static void RemoveNode(uint nodeId) =>
        igDockBuilderRemoveNode(nodeId);

    public static void SetNodeSize(uint nodeId, Vector2 size) =>
        igDockBuilderSetNodeSize(nodeId, size);

    public static uint SplitNode(uint nodeId, ImGuiDir dir, float sizeRatio, out uint outDir, out uint outOpposite) =>
        igDockBuilderSplitNode(nodeId, (int)dir, sizeRatio, out outDir, out outOpposite);

    public static void Finish(uint nodeId) =>
        igDockBuilderFinish(nodeId);
}
