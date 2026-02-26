using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class MacToolbarCallbacks
{
    public static event Action<string>? OnToolbarAction;
    public static nint TargetInstance { get; private set; }

    private static readonly string[] GizmoModeNames = ["GizmoNone", "GizmoBoundingBox", "GizmoMove", "GizmoRotate", "GizmoScale"];

    public static void Initialize()
    {
        var nsObject = ObjCRuntime.GetClass("NSObject");
        var cls = ObjCRuntime.AllocateClassPair(nsObject, "ToolbarTarget", 0);
        if (cls == 0) return; // Already registered

        unsafe
        {
            delegate* unmanaged[Cdecl]<nint, nint, nint, void> gizmoPtr = &HandleGizmoModeChanged;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("gizmoModeChanged:"),
                (nint)gizmoPtr, "v@:@");

            delegate* unmanaged[Cdecl]<nint, nint, nint, void> playPtr = &HandlePlayAction;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("playAction:"),
                (nint)playPtr, "v@:@");

            delegate* unmanaged[Cdecl]<nint, nint, nint, void> stopPtr = &HandleStopAction;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("stopAction:"),
                (nint)stopPtr, "v@:@");
        }

        ObjCRuntime.RegisterClassPair(cls);
        TargetInstance = ObjCRuntime.Init(ObjCRuntime.Alloc(cls));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void HandleGizmoModeChanged(nint self, nint sel, nint sender)
    {
        // sender is the NSSegmentedControl — read selectedSegment
        var segment = ObjCRuntime.MsgSendLong(sender, ObjCRuntime.SelRegisterName("selectedSegment"));
        if (segment >= 0 && segment < GizmoModeNames.Length)
        {
            OnToolbarAction?.Invoke(GizmoModeNames[segment]);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void HandlePlayAction(nint self, nint sel, nint sender)
    {
        OnToolbarAction?.Invoke("PlayPause");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void HandleStopAction(nint self, nint sel, nint sender)
    {
        OnToolbarAction?.Invoke("Stop");
    }
}
