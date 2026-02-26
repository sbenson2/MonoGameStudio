using System.Text.Json;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Project;
using MonoGameStudio.Editor.Editor;

namespace MonoGameStudio.Editor.Project;

public class UserDataManager
{
    private const int MaxRecentProjects = 20;
    private readonly string _recentProjectsPath;
    private readonly string _preferencesPath;
    private RecentProjectsList _recentProjects = new();

    public IReadOnlyList<RecentProject> RecentProjects => _recentProjects.Projects;

    public string LayoutsDirectory { get; }

    public UserDataManager()
    {
        var appDataDir = GetAppDataDirectory();
        Directory.CreateDirectory(appDataDir);
        _recentProjectsPath = Path.Combine(appDataDir, "recent-projects.json");
        _preferencesPath = Path.Combine(appDataDir, "editor_preferences.json");
        LayoutsDirectory = Path.Combine(appDataDir, "layouts");
        Load();
    }

    public void AddRecentProject(string name, string path)
    {
        var fullPath = Path.GetFullPath(path);

        // Remove existing entry for this path
        _recentProjects.Projects.RemoveAll(p =>
            string.Equals(p.Path, fullPath, StringComparison.OrdinalIgnoreCase));

        // Insert at the top
        _recentProjects.Projects.Insert(0, new RecentProject
        {
            Name = name,
            Path = fullPath,
            LastOpened = DateTime.UtcNow
        });

        // Trim to max
        if (_recentProjects.Projects.Count > MaxRecentProjects)
            _recentProjects.Projects.RemoveRange(MaxRecentProjects,
                _recentProjects.Projects.Count - MaxRecentProjects);

        Save();
    }

    public void RemoveRecentProject(string path)
    {
        _recentProjects.Projects.RemoveAll(p =>
            string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_recentProjectsPath))
            {
                var json = File.ReadAllText(_recentProjectsPath);
                _recentProjects = JsonSerializer.Deserialize<RecentProjectsList>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load recent projects: {ex.Message}");
            _recentProjects = new RecentProjectsList();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_recentProjects, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_recentProjectsPath, json);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save recent projects: {ex.Message}");
        }
    }

    public EditorPreferences LoadPreferences()
    {
        try
        {
            if (File.Exists(_preferencesPath))
            {
                var json = File.ReadAllText(_preferencesPath);
                return JsonSerializer.Deserialize<EditorPreferences>(json) ?? new EditorPreferences();
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load preferences: {ex.Message}");
        }
        return new EditorPreferences();
    }

    public void SavePreferences(EditorPreferences prefs)
    {
        try
        {
            var json = JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_preferencesPath, json);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save preferences: {ex.Message}");
        }
    }

    private static string GetAppDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "MonoGameStudio");

        if (OperatingSystem.IsWindows())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MonoGameStudio");

        // Linux
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "MonoGameStudio");
    }
}
