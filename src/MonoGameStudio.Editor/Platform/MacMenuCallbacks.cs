using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class MacMenuCallbacks
{
    public static event Action<string>? OnMenuAction;
    public static nint TargetInstance { get; private set; }
    public static LayoutProfileManager? LayoutProfileManager { get; set; }

    private const int MaxLayoutSlots = 20;

    // Map selectors to action names
    private static readonly Dictionary<string, string> _selectorToAction = new()
    {
        ["newScene:"] = "NewScene",
        ["openScene:"] = "OpenScene",
        ["saveScene:"] = "SaveScene",
        ["saveSceneAs:"] = "SaveSceneAs",
        ["undoAction:"] = "Undo",
        ["redoAction:"] = "Redo",
        ["toggleHierarchy:"] = "ToggleHierarchy",
        ["toggleInspector:"] = "ToggleInspector",
        ["toggleViewport:"] = "ToggleViewport",
        ["toggleConsole:"] = "ToggleConsole",
        ["toggleAssetBrowser:"] = "ToggleAssetBrowser",
        ["closeProject:"] = "CloseProject",
        ["toggleSettings:"] = "ToggleSettings",
        ["changeTheme:"] = "ChangeTheme",
        ["saveLayout:"] = "SaveLayout",
    };

    // Must keep delegate references alive to prevent GC
    private static readonly List<Delegate> _pinnedDelegates = new();

    public static void Initialize()
    {
        var nsObject = ObjCRuntime.GetClass("NSObject");
        var menuTargetClass = ObjCRuntime.AllocateClassPair(nsObject, "MenuTarget", 0);
        if (menuTargetClass == 0) return; // Already registered

        // Register a method for each selector
        foreach (var sel in _selectorToAction.Keys)
        {
            RegisterAction(menuTargetClass, sel);
        }

        // Pre-register loadLayout_N: and deleteLayout_N: selectors
        for (int i = 0; i < MaxLayoutSlots; i++)
        {
            RegisterAction(menuTargetClass, $"loadLayout_{i}:");
            RegisterAction(menuTargetClass, $"deleteLayout_{i}:");
        }

        // Register validateMenuItem: for checkmarks
        unsafe
        {
            delegate* unmanaged[Cdecl]<nint, nint, nint, byte> validatePtr = &ValidateMenuItem;
            ObjCRuntime.ClassAddMethod(menuTargetClass,
                ObjCRuntime.SelRegisterName("validateMenuItem:"),
                (nint)validatePtr, "c@:@");
        }

        ObjCRuntime.RegisterClassPair(menuTargetClass);

        TargetInstance = ObjCRuntime.Init(ObjCRuntime.Alloc(menuTargetClass));
    }

    private static void RegisterAction(nint cls, string selectorName)
    {
        unsafe
        {
            delegate* unmanaged[Cdecl]<nint, nint, nint, void> actionPtr = &HandleMenuAction;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName(selectorName),
                (nint)actionPtr, "v@:@");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void HandleMenuAction(nint self, nint sel, nint sender)
    {
        var selName = Marshal.PtrToStringAnsi(sel_getName(sel));
        if (selName == null) return;

        // Check static selector map first
        if (_selectorToAction.TryGetValue(selName, out var action))
        {
            OnMenuAction?.Invoke(action);
            return;
        }

        // Dynamic layout selectors: loadLayout_N: and deleteLayout_N:
        if (selName.StartsWith("loadLayout_") && selName.EndsWith(":"))
        {
            var indexStr = selName["loadLayout_".Length..^1];
            if (int.TryParse(indexStr, out var index))
            {
                OnMenuAction?.Invoke($"LoadLayout:{index}");
            }
            return;
        }

        if (selName.StartsWith("deleteLayout_") && selName.EndsWith(":"))
        {
            var indexStr = selName["deleteLayout_".Length..^1];
            if (int.TryParse(indexStr, out var index))
            {
                OnMenuAction?.Invoke($"DeleteLayout:{index}");
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static byte ValidateMenuItem(nint self, nint sel, nint menuItem)
    {
        var actionSel = ObjCRuntime.MsgSend(menuItem, ObjCRuntime.SelRegisterName("action"));
        var actionName = Marshal.PtrToStringAnsi(sel_getName(actionSel));

        if (actionName != null && MacMenuBar.EditorState != null)
        {
            var state = MacMenuBar.EditorState;

            // Panel toggle checkmarks
            long checkState = actionName switch
            {
                "toggleHierarchy:" => state.ShowHierarchy ? 1 : 0,
                "toggleInspector:" => state.ShowInspector ? 1 : 0,
                "toggleViewport:" => state.ShowViewport ? 1 : 0,
                "toggleConsole:" => state.ShowConsole ? 1 : 0,
                "toggleAssetBrowser:" => state.ShowAssetBrowser ? 1 : 0,
                "toggleSettings:" => state.ShowSettings ? 1 : 0,
                _ => -1
            };

            if (checkState >= 0)
            {
                ObjCRuntime.MsgSendVoid(menuItem,
                    ObjCRuntime.SelRegisterName("setState:"), checkState);
                return 1;
            }

            // Layout profile checkmarks
            if (LayoutProfileManager != null && actionName.StartsWith("loadLayout_") && actionName.EndsWith(":"))
            {
                var indexStr = actionName["loadLayout_".Length..^1];
                if (int.TryParse(indexStr, out var index))
                {
                    var profiles = LayoutProfileManager.ProfileNames;
                    if (index >= 0 && index < profiles.Count)
                    {
                        long isActive = profiles[index] == LayoutProfileManager.ActiveProfile ? 1 : 0;
                        ObjCRuntime.MsgSendVoid(menuItem,
                            ObjCRuntime.SelRegisterName("setState:"), isActive);
                    }
                }
            }
        }

        return 1; // YES — enable the menu item
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern nint sel_getName(nint sel);
}
