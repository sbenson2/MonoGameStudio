using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class ObjCRuntime
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";
    private const string DlLib = "/usr/lib/libSystem.B.dylib";

    [DllImport(ObjCLib, EntryPoint = "objc_getClass")]
    public static extern nint GetClass(string name);

    [DllImport(ObjCLib, EntryPoint = "sel_getName")]
    public static extern nint SelGetName(nint sel);

    [DllImport(DlLib)]
    public static extern nint dlopen(string? path, int mode);

    [DllImport(DlLib)]
    public static extern nint dlsym(nint handle, string symbol);

    [DllImport(ObjCLib, EntryPoint = "sel_registerName")]
    public static extern nint SelRegisterName(string name);

    [DllImport(ObjCLib, EntryPoint = "objc_allocateClassPair")]
    public static extern nint AllocateClassPair(nint superclass, string name, nint extraBytes);

    [DllImport(ObjCLib, EntryPoint = "objc_registerClassPair")]
    public static extern void RegisterClassPair(nint cls);

    [DllImport(ObjCLib, EntryPoint = "class_addMethod")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool ClassAddMethod(nint cls, nint sel, nint imp, string types);

    // objc_msgSend overloads
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, nint arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, nint arg1, nint arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, nint arg1, nint arg2, nint arg3);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, nint arg1, ulong arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector, nint arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector, nint arg1, nint arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector, long arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern long MsgSendLong(nint receiver, nint selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool MsgSendBool(nint receiver, nint selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool MsgSendBool(nint receiver, nint selector, nint arg1);

    public static nint ToNSString(string str)
    {
        var nsString = GetClass("NSString");
        var sel = SelRegisterName("stringWithUTF8String:");
        return MsgSend(nsString, sel, Marshal.StringToHGlobalAnsi(str));
    }

    public static nint Alloc(nint cls)
    {
        return MsgSend(cls, SelRegisterName("alloc"));
    }

    public static nint Init(nint obj)
    {
        return MsgSend(obj, SelRegisterName("init"));
    }

    public static nint AllocInit(string className)
    {
        return Init(Alloc(GetClass(className)));
    }

    public static string? MarshalNSString(nint nsString)
    {
        if (nsString == 0) return null;
        var utf8 = MsgSend(nsString, SelRegisterName("UTF8String"));
        return Marshal.PtrToStringAnsi(utf8);
    }

    public static string? SelNameToString(nint sel)
    {
        return Marshal.PtrToStringAnsi(SelGetName(sel));
    }

    /// <summary>
    /// Loads a named AppKit constant string (e.g. NSToolbarFlexibleSpaceItemIdentifier).
    /// </summary>
    public static nint GetAppKitConstant(string name)
    {
        const int RTLD_LAZY = 1;
        var handle = dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RTLD_LAZY);
        if (handle == 0) return 0;
        var ptr = dlsym(handle, name);
        if (ptr == 0) return 0;
        return Marshal.ReadIntPtr(ptr);
    }

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern double MsgSendDouble(nint receiver, nint selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector, double arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, double arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, long arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, bool arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void MsgSendVoid(nint receiver, nint selector, double arg1, nint arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern nint MsgSend(nint receiver, nint selector, nint arg1, nint arg2, nint arg3, nint arg4);

    /// <summary>
    /// Reliably gets the NSWindow handle, even during LoadContent when mainWindow is nil.
    /// Tries mainWindow → keyWindow → first object in the windows array.
    /// </summary>
    public static nint GetNSWindow()
    {
        var app = MsgSend(GetClass("NSApplication"), SelRegisterName("sharedApplication"));

        // Try mainWindow first (works once window is fully registered)
        var window = MsgSend(app, SelRegisterName("mainWindow"));
        if (window != 0) return window;

        // Try keyWindow (often set before mainWindow)
        window = MsgSend(app, SelRegisterName("keyWindow"));
        if (window != 0) return window;

        // Fallback: first object in the windows array
        var windows = MsgSend(app, SelRegisterName("windows"));
        var count = MsgSendLong(windows, SelRegisterName("count"));
        if (count > 0)
            window = MsgSend(windows, SelRegisterName("firstObject"));

        return window;
    }
}
