using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Editor.Panels;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class MacToolbar
{
    private static nint _segmentedControl;
    private static nint _playButton;
    private static nint _toolbar;

    // Track last-known state to avoid redundant ObjC calls
    private static GizmoMode _lastGizmoMode = (GizmoMode)(-1);
    private static EditorMode _lastEditorMode = (EditorMode)(-1);

    // Keep delegate class alive
    private static nint _delegateInstance;

    // SF Symbol names for gizmo segments
    private static readonly string[] GizmoSymbols =
    [
        "cursorarrow",
        "rectangle.dashed",
        "arrow.up.and.down.and.arrow.left.and.right",
        "arrow.triangle.2.circlepath",
        "arrow.up.left.and.arrow.down.right.and.arrow.up.right.and.arrow.down.left"
    ];

    // Custom item identifiers
    private static readonly string[] ItemIdentifiers = ["GizmoModes", "PlayPause", "Stop"];

    public static void Install()
    {
        var window = ObjCRuntime.GetNSWindow();
        if (window == 0)
        {
            Console.WriteLine("[MacToolbar] ERROR: Could not obtain NSWindow handle");
            return;
        }
        Console.WriteLine($"[MacToolbar] Window handle obtained: 0x{window:X}");

        // Create toolbar
        var nsToolbar = ObjCRuntime.GetClass("NSToolbar");
        _toolbar = ObjCRuntime.Alloc(nsToolbar);
        _toolbar = ObjCRuntime.MsgSend(_toolbar,
            ObjCRuntime.SelRegisterName("initWithIdentifier:"),
            ObjCRuntime.ToNSString("EditorToolbar"));

        // Create and set delegate
        _delegateInstance = CreateDelegate();
        Console.WriteLine($"[MacToolbar] Delegate created: 0x{_delegateInstance:X}");
        ObjCRuntime.MsgSendVoid(_toolbar,
            ObjCRuntime.SelRegisterName("setDelegate:"), _delegateInstance);

        // Log toolbar class for diagnostics
        var toolbarClassName = ObjCRuntime.MsgSend(_toolbar, ObjCRuntime.SelRegisterName("className"));
        Console.WriteLine($"[MacToolbar] Toolbar class: {ObjCRuntime.MarshalNSString(toolbarClassName)}, handle: 0x{_toolbar:X}");

        // displayMode = NSToolbarDisplayModeIconOnly (1)
        ObjCRuntime.MsgSendVoid(_toolbar,
            ObjCRuntime.SelRegisterName("setDisplayMode:"), (nint)1);

        // allowsUserCustomization = NO
        ObjCRuntime.MsgSendVoid(_toolbar,
            ObjCRuntime.SelRegisterName("setAllowsUserCustomization:"), (nint)0);

        // Install on window
        ObjCRuntime.MsgSendVoid(window,
            ObjCRuntime.SelRegisterName("setToolbar:"), _toolbar);
        Console.WriteLine("[MacToolbar] Toolbar set on window");
    }

    public static void UpdateState(GizmoMode gizmoMode, EditorMode editorMode)
    {
        // Sync segmented control selection
        if (gizmoMode != _lastGizmoMode && _segmentedControl != 0)
        {
            int segment = gizmoMode switch
            {
                GizmoMode.None => 0,
                GizmoMode.BoundingBox => 1,
                GizmoMode.Move => 2,
                GizmoMode.Rotate => 3,
                GizmoMode.Scale => 4,
                _ => 1
            };
            ObjCRuntime.MsgSendVoid(_segmentedControl,
                ObjCRuntime.SelRegisterName("setSelectedSegment:"), segment);
            _lastGizmoMode = gizmoMode;
        }

        // Swap play button image between play.fill / pause.fill
        if (editorMode != _lastEditorMode && _playButton != 0)
        {
            bool isPlaying = editorMode == EditorMode.Play;
            var symbolName = isPlaying ? "pause.fill" : "play.fill";
            var image = CreateSFSymbol(symbolName);
            if (image != 0)
            {
                ObjCRuntime.MsgSendVoid(_playButton,
                    ObjCRuntime.SelRegisterName("setImage:"), image);
            }
            _lastEditorMode = editorMode;
        }
    }

    private static nint CreateDelegate()
    {
        var nsObject = ObjCRuntime.GetClass("NSObject");
        var cls = ObjCRuntime.AllocateClassPair(nsObject, "ToolbarDelegate", 0);
        if (cls == 0) return 0;

        unsafe
        {
            // toolbarDefaultItemIdentifiers:
            delegate* unmanaged[Cdecl]<nint, nint, nint, nint> defaultItemsPtr = &DefaultItemIdentifiers;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("toolbarDefaultItemIdentifiers:"),
                (nint)defaultItemsPtr, "@@:@");

            // toolbarAllowedItemIdentifiers:
            delegate* unmanaged[Cdecl]<nint, nint, nint, nint> allowedItemsPtr = &AllowedItemIdentifiers;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("toolbarAllowedItemIdentifiers:"),
                (nint)allowedItemsPtr, "@@:@");

            // toolbar:itemForItemIdentifier:willBeInsertedIntoToolbar:
            delegate* unmanaged[Cdecl]<nint, nint, nint, nint, byte, nint> itemForIdPtr = &ItemForIdentifier;
            ObjCRuntime.ClassAddMethod(cls,
                ObjCRuntime.SelRegisterName("toolbar:itemForItemIdentifier:willBeInsertedIntoToolbar:"),
                (nint)itemForIdPtr, "@@:@@c");
        }

        ObjCRuntime.RegisterClassPair(cls);
        return ObjCRuntime.Init(ObjCRuntime.Alloc(cls));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint DefaultItemIdentifiers(nint self, nint sel, nint toolbar)
    {
        Console.WriteLine("[MacToolbar] DefaultItemIdentifiers called");
        return BuildIdentifierArray();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint AllowedItemIdentifiers(nint self, nint sel, nint toolbar)
    {
        return BuildIdentifierArray();
    }

    private static nint BuildIdentifierArray()
    {
        var flexSpace = ObjCRuntime.GetAppKitConstant("NSToolbarFlexibleSpaceItemIdentifier");

        var array = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSMutableArray"),
            ObjCRuntime.SelRegisterName("array"));

        var addSel = ObjCRuntime.SelRegisterName("addObject:");

        // GizmoModes
        ObjCRuntime.MsgSendVoid(array, addSel, ObjCRuntime.ToNSString("GizmoModes"));
        // Flexible space
        if (flexSpace != 0)
            ObjCRuntime.MsgSendVoid(array, addSel, flexSpace);
        // PlayPause
        ObjCRuntime.MsgSendVoid(array, addSel, ObjCRuntime.ToNSString("PlayPause"));
        // Stop
        ObjCRuntime.MsgSendVoid(array, addSel, ObjCRuntime.ToNSString("Stop"));

        return array;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint ItemForIdentifier(nint self, nint sel, nint toolbar,
        nint itemIdentifier, byte willBeInserted)
    {
        var identifier = ObjCRuntime.MarshalNSString(itemIdentifier);
        Console.WriteLine($"[MacToolbar] ItemForIdentifier called: '{identifier}'");

        return identifier switch
        {
            "GizmoModes" => CreateGizmoItem(itemIdentifier),
            "PlayPause" => CreatePlayPauseItem(itemIdentifier),
            "Stop" => CreateStopItem(itemIdentifier),
            _ => 0
        };
    }

    private static nint CreateGizmoItem(nint identifier)
    {
        var item = CreateToolbarItem(identifier);

        // Create NSSegmentedControl
        var segClass = ObjCRuntime.GetClass("NSSegmentedControl");
        _segmentedControl = ObjCRuntime.Alloc(segClass);
        _segmentedControl = ObjCRuntime.MsgSend(_segmentedControl,
            ObjCRuntime.SelRegisterName("init"));

        // Set segment count
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setSegmentCount:"), GizmoSymbols.Length);

        // Set tracking mode = selectOne (0)
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setTrackingMode:"), 0);

        // Set segment style = separated (3) for modern look
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setSegmentStyle:"), 3);

        // Configure each segment with SF Symbol
        for (int i = 0; i < GizmoSymbols.Length; i++)
        {
            var image = CreateSFSymbol(GizmoSymbols[i]);
            if (image != 0)
            {
                ObjCRuntime.MsgSend(_segmentedControl,
                    ObjCRuntime.SelRegisterName("setImage:forSegment:"),
                    image, (nint)i);
            }

            // Set width for each segment
            ObjCRuntime.MsgSendVoid(_segmentedControl,
                ObjCRuntime.SelRegisterName("setWidth:forSegment:"),
                36.0, (nint)i);
        }

        // Select BoundingBox segment by default
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setSelectedSegment:"), 1);

        // Set action target
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setTarget:"), MacToolbarCallbacks.TargetInstance);
        ObjCRuntime.MsgSendVoid(_segmentedControl,
            ObjCRuntime.SelRegisterName("setAction:"),
            ObjCRuntime.SelRegisterName("gizmoModeChanged:"));

        // Set the view on the toolbar item
        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setView:"), _segmentedControl);

        return item;
    }

    private static nint CreatePlayPauseItem(nint identifier)
    {
        var item = CreateToolbarItem(identifier);

        _playButton = CreateButton("play.fill", "playAction:");

        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setView:"), _playButton);

        return item;
    }

    private static nint CreateStopItem(nint identifier)
    {
        var item = CreateToolbarItem(identifier);

        var button = CreateButton("stop.fill", "stopAction:");

        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setView:"), button);

        return item;
    }

    private static nint CreateToolbarItem(nint identifier)
    {
        var nsToolbarItem = ObjCRuntime.GetClass("NSToolbarItem");
        var item = ObjCRuntime.Alloc(nsToolbarItem);
        item = ObjCRuntime.MsgSend(item,
            ObjCRuntime.SelRegisterName("initWithItemIdentifier:"), identifier);
        return item;
    }

    private static nint CreateButton(string sfSymbolName, string actionSelector)
    {
        var nsButton = ObjCRuntime.GetClass("NSButton");

        var image = CreateSFSymbol(sfSymbolName);

        // [NSButton buttonWithImage:target:action:]
        var button = ObjCRuntime.MsgSend(nsButton,
            ObjCRuntime.SelRegisterName("buttonWithImage:target:action:"),
            image,
            MacToolbarCallbacks.TargetInstance,
            ObjCRuntime.SelRegisterName(actionSelector));

        // Set bezel style to toolbar (12 = NSBezelStyleToolbar, available macOS 14+;
        // fallback: 0 = NSBezelStyleRounded which also works in toolbars)
        ObjCRuntime.MsgSendVoid(button,
            ObjCRuntime.SelRegisterName("setBezelStyle:"), 0);

        // Make it borderless for a cleaner toolbar look
        ObjCRuntime.MsgSendVoid(button,
            ObjCRuntime.SelRegisterName("setBordered:"), 0);

        return button;
    }

    private static nint CreateSFSymbol(string name)
    {
        var nsImage = ObjCRuntime.GetClass("NSImage");
        var image = ObjCRuntime.MsgSend(nsImage,
            ObjCRuntime.SelRegisterName("imageWithSystemSymbolName:accessibilityDescription:"),
            ObjCRuntime.ToNSString(name), 0);
        return image;
    }
}
