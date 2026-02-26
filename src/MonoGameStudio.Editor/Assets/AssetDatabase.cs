using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Editor.Assets;

public class AssetDatabase : IDisposable
{
    private string? _rootDirectory;
    private FileSystemWatcher? _watcher;
    private readonly List<AssetEntry> _allEntries = new();
    private readonly Dictionary<string, List<AssetEntry>> _entriesByDirectory = new();
    private bool _needsRefresh;

    public IReadOnlyList<AssetEntry> AllEntries => _allEntries;
    public string? RootDirectory => _rootDirectory;
    public bool HasRoot => _rootDirectory != null;

    public event Action? OnChanged;

    public void SetRoot(string directory)
    {
        StopWatching();
        _rootDirectory = directory;

        if (Directory.Exists(directory))
        {
            Scan();
            StartWatching();
        }
    }

    public void Clear()
    {
        StopWatching();
        _rootDirectory = null;
        _allEntries.Clear();
        _entriesByDirectory.Clear();
    }

    /// <summary>
    /// Call from Update() each frame to process pending refreshes on the main thread.
    /// </summary>
    public void PollChanges()
    {
        if (_needsRefresh)
        {
            _needsRefresh = false;
            Scan();
            OnChanged?.Invoke();
        }
    }

    public void Refresh() => _needsRefresh = true;

    public List<AssetEntry> GetEntriesInDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        return _entriesByDirectory.TryGetValue(fullPath, out var entries)
            ? entries
            : new List<AssetEntry>();
    }

    public List<AssetEntry> Search(string query, AssetType? typeFilter = null)
    {
        var results = new List<AssetEntry>();
        foreach (var entry in _allEntries)
        {
            if (typeFilter.HasValue && entry.Type != typeFilter.Value)
                continue;
            if (!string.IsNullOrEmpty(query) &&
                !entry.FileName.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;
            results.Add(entry);
        }
        return results;
    }

    public List<string> GetSubdirectories(string directory)
    {
        if (!Directory.Exists(directory)) return new();

        try
        {
            return Directory.GetDirectories(directory)
                .Where(d => !Path.GetFileName(d).StartsWith('.'))
                .OrderBy(d => Path.GetFileName(d))
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    private void Scan()
    {
        _allEntries.Clear();
        _entriesByDirectory.Clear();

        if (_rootDirectory == null || !Directory.Exists(_rootDirectory))
            return;

        try
        {
            ScanDirectory(_rootDirectory);
            Log.Info($"Asset database: scanned {_allEntries.Count} files");
        }
        catch (Exception ex)
        {
            Log.Error($"Asset scan failed: {ex.Message}");
        }
    }

    private void ScanDirectory(string directory)
    {
        var dirKey = Path.GetFullPath(directory);
        if (!_entriesByDirectory.ContainsKey(dirKey))
            _entriesByDirectory[dirKey] = new List<AssetEntry>();

        try
        {
            foreach (var filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.StartsWith('.')) continue; // skip hidden files

                var info = new FileInfo(filePath);
                var entry = new AssetEntry
                {
                    FullPath = Path.GetFullPath(filePath),
                    RelativePath = Path.GetRelativePath(_rootDirectory!, filePath),
                    FileName = fileName,
                    Extension = Path.GetExtension(filePath),
                    Type = AssetEntry.ClassifyFile(fileName),
                    Size = info.Length,
                    LastModified = info.LastWriteTimeUtc,
                    IsDirectory = false
                };

                _allEntries.Add(entry);
                _entriesByDirectory[dirKey].Add(entry);
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var dirName = Path.GetFileName(subDir);
                if (dirName.StartsWith('.')) continue;

                ScanDirectory(subDir);
            }
        }
        catch (UnauthorizedAccessException) { }
    }

    private void StartWatching()
    {
        if (_rootDirectory == null || !Directory.Exists(_rootDirectory)) return;

        _watcher = new FileSystemWatcher(_rootDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                           NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Created += OnFileSystemChanged;
        _watcher.Deleted += OnFileSystemChanged;
        _watcher.Changed += OnFileSystemChanged;
        _watcher.Renamed += OnFileSystemRenamed;
        _watcher.EnableRaisingEvents = true;
    }

    private void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        _needsRefresh = true;
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        _needsRefresh = true;
    }

    public void Dispose()
    {
        StopWatching();
    }
}
