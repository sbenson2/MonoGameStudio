namespace MonoGameStudio.Editor.Platform;

public record struct FileFilter(string Description, string[] Extensions);

public interface IFileDialogService
{
    string? OpenFileDialog(string? title = null, string? defaultPath = null, FileFilter[]? filters = null);
    string? OpenFolderDialog(string? title = null, string? defaultPath = null);
    string? SaveFileDialog(string? title = null, string? defaultName = null, string? defaultPath = null, FileFilter[]? filters = null);
}
