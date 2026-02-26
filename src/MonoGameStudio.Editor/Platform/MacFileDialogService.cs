using System.Runtime.Versioning;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Editor.Platform;

[SupportedOSPlatform("macos")]
public class MacFileDialogService : IFileDialogService
{
    private const long NSModalResponseOK = 1;

    public string? OpenFileDialog(string? title = null, string? defaultPath = null, FileFilter[]? filters = null)
    {
        var panel = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSOpenPanel"), ObjCRuntime.SelRegisterName("openPanel"));

        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setCanChooseFiles:"), (nint)1);
        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setCanChooseDirectories:"), (nint)0);
        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setAllowsMultipleSelection:"), (nint)0);

        ConfigurePanel(panel, title, defaultPath, filters);

        var result = ObjCRuntime.MsgSendLong(panel, ObjCRuntime.SelRegisterName("runModal"));
        if (result != NSModalResponseOK) return null;

        return GetPanelURL(panel);
    }

    public string? OpenFolderDialog(string? title = null, string? defaultPath = null)
    {
        var panel = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSOpenPanel"), ObjCRuntime.SelRegisterName("openPanel"));

        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setCanChooseFiles:"), (nint)0);
        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setCanChooseDirectories:"), (nint)1);
        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setAllowsMultipleSelection:"), (nint)0);

        ConfigurePanel(panel, title, defaultPath, null);

        var result = ObjCRuntime.MsgSendLong(panel, ObjCRuntime.SelRegisterName("runModal"));
        if (result != NSModalResponseOK) return null;

        return GetPanelURL(panel);
    }

    public string? SaveFileDialog(string? title = null, string? defaultName = null, string? defaultPath = null, FileFilter[]? filters = null)
    {
        var panel = ObjCRuntime.MsgSend(ObjCRuntime.GetClass("NSSavePanel"), ObjCRuntime.SelRegisterName("savePanel"));

        if (defaultName != null)
        {
            var nsName = ObjCRuntime.ToNSString(defaultName);
            ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setNameFieldStringValue:"), nsName);
        }

        ConfigurePanel(panel, title, defaultPath, filters);

        var result = ObjCRuntime.MsgSendLong(panel, ObjCRuntime.SelRegisterName("runModal"));
        if (result != NSModalResponseOK) return null;

        return GetPanelURL(panel);
    }

    private static void ConfigurePanel(nint panel, string? title, string? defaultPath, FileFilter[]? filters)
    {
        if (title != null)
        {
            var nsTitle = ObjCRuntime.ToNSString(title);
            ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setTitle:"), nsTitle);
        }

        if (defaultPath != null && Directory.Exists(defaultPath))
        {
            var nsPath = ObjCRuntime.ToNSString(defaultPath);
            var nsUrl = ObjCRuntime.MsgSend(
                ObjCRuntime.GetClass("NSURL"),
                ObjCRuntime.SelRegisterName("fileURLWithPath:isDirectory:"),
                nsPath, (nint)1);
            ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setDirectoryURL:"), nsUrl);
        }

        if (filters is { Length: > 0 })
        {
            SetAllowedContentTypes(panel, filters);
        }
    }

    private static void SetAllowedContentTypes(nint panel, FileFilter[] filters)
    {
        // Collect all extensions from all filters
        var allExtensions = new List<string>();
        foreach (var filter in filters)
        {
            foreach (var ext in filter.Extensions)
                allExtensions.Add(ext.TrimStart('.'));
        }

        if (allExtensions.Count == 0) return;

        // Build NSArray of UTType objects
        var utTypes = new List<nint>();
        var utTypeCls = ObjCRuntime.GetClass("UTType");
        var typeWithExtSel = ObjCRuntime.SelRegisterName("typeWithFilenameExtension:");

        foreach (var ext in allExtensions)
        {
            var nsExt = ObjCRuntime.ToNSString(ext);
            var utType = ObjCRuntime.MsgSend(utTypeCls, typeWithExtSel, nsExt);
            if (utType != 0)
                utTypes.Add(utType);
        }

        if (utTypes.Count == 0) return;

        var nsArray = CreateNSArray(utTypes);
        ObjCRuntime.MsgSendVoid(panel, ObjCRuntime.SelRegisterName("setAllowedContentTypes:"), nsArray);
    }

    private static nint CreateNSArray(List<nint> objects)
    {
        var nsArrayCls = ObjCRuntime.GetClass("NSMutableArray");
        var array = ObjCRuntime.MsgSend(nsArrayCls, ObjCRuntime.SelRegisterName("arrayWithCapacity:"), (nint)objects.Count);

        var addSel = ObjCRuntime.SelRegisterName("addObject:");
        foreach (var obj in objects)
            ObjCRuntime.MsgSendVoid(array, addSel, obj);

        return array;
    }

    private static string? GetPanelURL(nint panel)
    {
        var url = ObjCRuntime.MsgSend(panel, ObjCRuntime.SelRegisterName("URL"));
        if (url == 0) return null;

        var path = ObjCRuntime.MsgSend(url, ObjCRuntime.SelRegisterName("path"));
        return ObjCRuntime.MarshalNSString(path);
    }
}
