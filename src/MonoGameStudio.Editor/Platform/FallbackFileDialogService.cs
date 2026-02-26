using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Editor.Platform;

public class FallbackFileDialogService : IFileDialogService
{
    public string? OpenFileDialog(string? title = null, string? defaultPath = null, FileFilter[]? filters = null)
    {
        Log.Warn("Native file dialogs not available on this platform.");
        return null;
    }

    public string? OpenFolderDialog(string? title = null, string? defaultPath = null)
    {
        Log.Warn("Native file dialogs not available on this platform.");
        return null;
    }

    public string? SaveFileDialog(string? title = null, string? defaultName = null, string? defaultPath = null, FileFilter[]? filters = null)
    {
        Log.Warn("Native file dialogs not available on this platform.");
        return null;
    }
}
