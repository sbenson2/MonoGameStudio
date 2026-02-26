using System.Runtime.Versioning;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class MacTitleBar
{
    /// <summary>
    /// Configures the NSWindow for a transparent, hidden title bar.
    /// NSToolbar handles the actual title bar content (buttons, controls).
    /// </summary>
    public static bool Configure()
    {
        var window = ObjCRuntime.GetNSWindow();
        if (window == 0) return false;

        // Make the title bar transparent
        ObjCRuntime.MsgSendVoid(window,
            ObjCRuntime.SelRegisterName("setTitlebarAppearsTransparent:"), 1);

        // Hide the window title text
        // titleVisibility = NSWindowTitleHidden (1)
        ObjCRuntime.MsgSendVoid(window,
            ObjCRuntime.SelRegisterName("setTitleVisibility:"), 1);

        return true;
    }
}
