using System.Runtime.Versioning;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
internal static class MacMenuBar
{
    public static EditorState? EditorState { get; private set; }

    private static nint _layoutsSubmenu;
    private static LayoutProfileManager? _layoutProfileManager;

    public static void Install(EditorState editorState, LayoutProfileManager? layoutProfileManager = null)
    {
        EditorState = editorState;
        _layoutProfileManager = layoutProfileManager;

        var app = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSApplication"),
            ObjCRuntime.SelRegisterName("sharedApplication"));

        var mainMenu = CreateMenu("");

        // App menu (first menu is always the app menu on macOS)
        var appMenu = CreateMenu("MonoGame.Studio");
        AddItem(appMenu, "About MonoGame.Studio", "orderFrontStandardAboutPanel:", "", null);
        AddSeparator(appMenu);
        AddServicesMenu(appMenu, app);
        AddSeparator(appMenu);
        AddItem(appMenu, "Hide MonoGame.Studio", "hide:", "h", null);
        AddItemWithModifiers(appMenu, "Hide Others", "hideOtherApplications:", "h",
            (1 << 20) | (1 << 19)); // Cmd+Option
        AddItem(appMenu, "Show All", "unhideAllApplications:", "", null);
        AddSeparator(appMenu);
        AddItem(appMenu, "Quit MonoGame.Studio", "terminate:", "q", null);
        AddSubmenu(mainMenu, "MonoGame.Studio", appMenu);

        // File menu
        var fileMenu = CreateMenu("File");
        AddItem(fileMenu, "New Scene", "newScene:", "n", MacMenuCallbacks.TargetInstance);
        AddItem(fileMenu, "Open Scene...", "openScene:", "o", MacMenuCallbacks.TargetInstance);
        AddSeparator(fileMenu);
        AddItem(fileMenu, "Save Scene", "saveScene:", "s", MacMenuCallbacks.TargetInstance);
        AddItemWithModifiers(fileMenu, "Save Scene As...", "saveSceneAs:", "s",
            (1 << 20) | (1 << 17), MacMenuCallbacks.TargetInstance); // Cmd+Shift
        AddSeparator(fileMenu);
        AddItem(fileMenu, "Close Project", "closeProject:", "", MacMenuCallbacks.TargetInstance);
        AddSubmenu(mainMenu, "File", fileMenu);

        // Edit menu
        var editMenu = CreateMenu("Edit");
        AddItem(editMenu, "Undo", "undoAction:", "z", MacMenuCallbacks.TargetInstance);
        AddItemWithModifiers(editMenu, "Redo", "redoAction:", "z",
            (1 << 20) | (1 << 17), MacMenuCallbacks.TargetInstance); // Cmd+Shift
        AddSeparator(editMenu);
        AddItem(editMenu, "Cut", "cut:", "x", null);
        AddItem(editMenu, "Copy", "copy:", "c", null);
        AddItem(editMenu, "Paste", "paste:", "v", null);
        AddItem(editMenu, "Select All", "selectAll:", "a", null);
        AddSubmenu(mainMenu, "Edit", editMenu);

        // View menu
        var viewMenu = CreateMenu("View");
        AddItem(viewMenu, "Hierarchy", "toggleHierarchy:", "1", MacMenuCallbacks.TargetInstance);
        AddItem(viewMenu, "Inspector", "toggleInspector:", "2", MacMenuCallbacks.TargetInstance);
        AddItem(viewMenu, "Game Viewport", "toggleViewport:", "3", MacMenuCallbacks.TargetInstance);
        AddItem(viewMenu, "Console", "toggleConsole:", "4", MacMenuCallbacks.TargetInstance);
        AddItem(viewMenu, "Asset Browser", "toggleAssetBrowser:", "5", MacMenuCallbacks.TargetInstance);
        AddItem(viewMenu, "Settings", "toggleSettings:", ",", MacMenuCallbacks.TargetInstance);
        AddSeparator(viewMenu);

        // Layouts submenu
        _layoutsSubmenu = CreateMenu("Layouts");
        BuildLayoutsSubmenuItems();
        AddSubmenu(viewMenu, "Layouts", _layoutsSubmenu);

        AddSeparator(viewMenu);
        AddItem(viewMenu, "Change Theme...", "changeTheme:", "", MacMenuCallbacks.TargetInstance);
        AddSubmenu(mainMenu, "View", viewMenu);

        // Window menu
        var windowMenu = CreateMenu("Window");
        AddItem(windowMenu, "Minimize", "performMiniaturize:", "m", null);
        AddItem(windowMenu, "Zoom", "performZoom:", "", null);
        AddSeparator(windowMenu);
        AddItem(windowMenu, "Bring All to Front", "arrangeInFront:", "", null);
        AddSubmenu(mainMenu, "Window", windowMenu);

        // Set as the Window menu for macOS window list behavior
        ObjCRuntime.MsgSendVoid(app, ObjCRuntime.SelRegisterName("setWindowsMenu:"), windowMenu);

        // Help menu
        var helpMenu = CreateMenu("Help");
        AddItem(helpMenu, "MonoGame.Studio Help", "orderFrontStandardAboutPanel:", "", null);
        AddSubmenu(mainMenu, "Help", helpMenu);

        // Install the menu bar
        ObjCRuntime.MsgSendVoid(app, ObjCRuntime.SelRegisterName("setMainMenu:"), mainMenu);
    }

    public static void RebuildLayoutsMenu(LayoutProfileManager layoutProfileManager)
    {
        _layoutProfileManager = layoutProfileManager;
        if (_layoutsSubmenu == 0) return;

        // Remove all items from the submenu
        ObjCRuntime.MsgSendVoid(_layoutsSubmenu, ObjCRuntime.SelRegisterName("removeAllItems"));

        BuildLayoutsSubmenuItems();
    }

    private static void BuildLayoutsSubmenuItems()
    {
        if (_layoutProfileManager == null)
        {
            // No manager yet — just show Default
            AddItem(_layoutsSubmenu, "Default", "loadLayout_0:", "", MacMenuCallbacks.TargetInstance);
            return;
        }

        var profiles = _layoutProfileManager.ProfileNames;

        // Add each profile as a loadLayout_N: item
        for (int i = 0; i < profiles.Count; i++)
        {
            AddItem(_layoutsSubmenu, profiles[i], $"loadLayout_{i}:", "", MacMenuCallbacks.TargetInstance);
        }

        AddSeparator(_layoutsSubmenu);
        AddItem(_layoutsSubmenu, "Save Layout...", "saveLayout:", "", MacMenuCallbacks.TargetInstance);

        // Delete Layout submenu (only if there are deletable profiles)
        var deletable = _layoutProfileManager.GetDeletableProfiles();
        if (deletable.Count > 0)
        {
            var deleteMenu = CreateMenu("Delete Layout");
            for (int i = 0; i < deletable.Count; i++)
            {
                // Index into the deletable list (offset by 0 since these are non-Default profiles)
                AddItem(deleteMenu, deletable[i], $"deleteLayout_{i}:", "", MacMenuCallbacks.TargetInstance);
            }
            AddSubmenu(_layoutsSubmenu, "Delete Layout", deleteMenu);
        }
    }

    private static nint CreateMenu(string title)
    {
        var nsMenu = ObjCRuntime.GetClass("NSMenu");
        var menu = ObjCRuntime.Alloc(nsMenu);
        var titleStr = ObjCRuntime.ToNSString(title);
        return ObjCRuntime.MsgSend(menu,
            ObjCRuntime.SelRegisterName("initWithTitle:"), titleStr);
    }

    private static void AddItem(nint menu, string title, string action, string keyEquivalent, nint? target)
    {
        var nsMenuItem = ObjCRuntime.GetClass("NSMenuItem");
        var item = ObjCRuntime.Alloc(nsMenuItem);
        item = ObjCRuntime.MsgSend(item,
            ObjCRuntime.SelRegisterName("initWithTitle:action:keyEquivalent:"),
            ObjCRuntime.ToNSString(title),
            ObjCRuntime.SelRegisterName(action),
            ObjCRuntime.ToNSString(keyEquivalent));

        if (target.HasValue)
        {
            ObjCRuntime.MsgSendVoid(item,
                ObjCRuntime.SelRegisterName("setTarget:"), target.Value);
        }

        ObjCRuntime.MsgSendVoid(menu,
            ObjCRuntime.SelRegisterName("addItem:"), item);
    }

    private static void AddItemWithModifiers(nint menu, string title, string action,
        string keyEquivalent, long modifierMask, nint? target = null)
    {
        var nsMenuItem = ObjCRuntime.GetClass("NSMenuItem");
        var item = ObjCRuntime.Alloc(nsMenuItem);
        item = ObjCRuntime.MsgSend(item,
            ObjCRuntime.SelRegisterName("initWithTitle:action:keyEquivalent:"),
            ObjCRuntime.ToNSString(title),
            ObjCRuntime.SelRegisterName(action),
            ObjCRuntime.ToNSString(keyEquivalent));

        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setKeyEquivalentModifierMask:"), modifierMask);

        if (target.HasValue)
        {
            ObjCRuntime.MsgSendVoid(item,
                ObjCRuntime.SelRegisterName("setTarget:"), target.Value);
        }

        ObjCRuntime.MsgSendVoid(menu,
            ObjCRuntime.SelRegisterName("addItem:"), item);
    }

    private static void AddSeparator(nint menu)
    {
        var separator = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSMenuItem"),
            ObjCRuntime.SelRegisterName("separatorItem"));
        ObjCRuntime.MsgSendVoid(menu,
            ObjCRuntime.SelRegisterName("addItem:"), separator);
    }

    private static void AddSubmenu(nint parentMenu, string title, nint submenu)
    {
        var nsMenuItem = ObjCRuntime.GetClass("NSMenuItem");
        var item = ObjCRuntime.Alloc(nsMenuItem);
        item = ObjCRuntime.MsgSend(item, ObjCRuntime.SelRegisterName("init"));

        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setTitle:"), ObjCRuntime.ToNSString(title));
        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setSubmenu:"), submenu);

        ObjCRuntime.MsgSendVoid(parentMenu,
            ObjCRuntime.SelRegisterName("addItem:"), item);
    }

    private static void AddServicesMenu(nint appMenu, nint app)
    {
        var servicesMenu = CreateMenu("Services");
        ObjCRuntime.MsgSendVoid(app,
            ObjCRuntime.SelRegisterName("setServicesMenu:"), servicesMenu);

        var nsMenuItem = ObjCRuntime.GetClass("NSMenuItem");
        var item = ObjCRuntime.Alloc(nsMenuItem);
        item = ObjCRuntime.MsgSend(item,
            ObjCRuntime.SelRegisterName("initWithTitle:action:keyEquivalent:"),
            ObjCRuntime.ToNSString("Services"),
            0,
            ObjCRuntime.ToNSString(""));
        ObjCRuntime.MsgSendVoid(item,
            ObjCRuntime.SelRegisterName("setSubmenu:"), servicesMenu);
        ObjCRuntime.MsgSendVoid(appMenu,
            ObjCRuntime.SelRegisterName("addItem:"), item);
    }
}
