using System.Text.Json;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Editor.Layout;

public class LayoutProfileManager
{
    private const string DefaultProfileName = "Default";
    private const string IndexFileName = "layouts.json";

    private readonly string _layoutsDirectory;
    private readonly DockingLayout _dockingLayout;
    private readonly List<ProfileEntry> _profiles = new();

    public string ActiveProfile { get; private set; } = DefaultProfileName;
    public IReadOnlyList<string> ProfileNames => _profiles.Select(p => p.Name).Prepend(DefaultProfileName).ToList();

    public event Action? OnProfilesChanged;

    public LayoutProfileManager(string layoutsDirectory, DockingLayout dockingLayout)
    {
        _layoutsDirectory = layoutsDirectory;
        _dockingLayout = dockingLayout;
        Directory.CreateDirectory(_layoutsDirectory);
        LoadIndex();
    }

    public void SaveCurrentLayout(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == DefaultProfileName)
            return;

        var fileName = SanitizeFileName(name) + ".ini";
        var filePath = Path.Combine(_layoutsDirectory, fileName);

        ImGui.SaveIniSettingsToDisk(filePath);

        // Update or add profile entry
        var existing = _profiles.FindIndex(p => p.Name == name);
        if (existing >= 0)
        {
            _profiles[existing] = new ProfileEntry { Name = name, FileName = fileName };
        }
        else
        {
            _profiles.Add(new ProfileEntry { Name = name, FileName = fileName });
        }

        ActiveProfile = name;
        SaveIndex();
        OnProfilesChanged?.Invoke();
        Log.Info($"Layout saved: {name}");
    }

    public void LoadProfile(string name)
    {
        if (name == DefaultProfileName)
        {
            _dockingLayout.ResetLayout();
            ActiveProfile = DefaultProfileName;
            SaveIndex();
            OnProfilesChanged?.Invoke();
            return;
        }

        var entry = _profiles.Find(p => p.Name == name);
        if (entry == null) return;

        var filePath = Path.Combine(_layoutsDirectory, entry.FileName);
        if (!File.Exists(filePath))
        {
            Log.Warn($"Layout file not found: {filePath}");
            return;
        }

        ImGui.LoadIniSettingsFromDisk(filePath);
        _dockingLayout.ResetLayout();

        ActiveProfile = name;
        SaveIndex();
        OnProfilesChanged?.Invoke();
        Log.Info($"Layout loaded: {name}");
    }

    public void DeleteProfile(string name)
    {
        if (name == DefaultProfileName) return;

        var entry = _profiles.Find(p => p.Name == name);
        if (entry == null) return;

        var filePath = Path.Combine(_layoutsDirectory, entry.FileName);
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); }
            catch (Exception ex) { Log.Warn($"Failed to delete layout file: {ex.Message}"); }
        }

        _profiles.Remove(entry);

        if (ActiveProfile == name)
        {
            ActiveProfile = DefaultProfileName;
            _dockingLayout.ResetLayout();
        }

        SaveIndex();
        OnProfilesChanged?.Invoke();
        Log.Info($"Layout deleted: {name}");
    }

    /// <summary>Get profile names excluding Default (for delete menu).</summary>
    public IReadOnlyList<string> GetDeletableProfiles()
    {
        return _profiles.Select(p => p.Name).ToList();
    }

    private void SaveIndex()
    {
        try
        {
            var index = new LayoutIndex
            {
                Profiles = _profiles.ToList(),
                ActiveProfile = ActiveProfile
            };
            var json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_layoutsDirectory, IndexFileName), json);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save layout index: {ex.Message}");
        }
    }

    private void LoadIndex()
    {
        var indexPath = Path.Combine(_layoutsDirectory, IndexFileName);
        if (!File.Exists(indexPath)) return;

        try
        {
            var json = File.ReadAllText(indexPath);
            var index = JsonSerializer.Deserialize<LayoutIndex>(json);
            if (index != null)
            {
                _profiles.Clear();
                _profiles.AddRange(index.Profiles);
                ActiveProfile = index.ActiveProfile ?? DefaultProfileName;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load layout index: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.ToLowerInvariant().Replace(' ', '_');
    }

    private class ProfileEntry
    {
        public string Name { get; set; } = "";
        public string FileName { get; set; } = "";
    }

    private class LayoutIndex
    {
        public List<ProfileEntry> Profiles { get; set; } = new();
        public string? ActiveProfile { get; set; }
    }
}
