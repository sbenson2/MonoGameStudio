namespace MonoGameStudio.Editor.Runtime;

public class HotReloadWatcher : IDisposable
{
    private FileSystemWatcher? _watcher;
    private System.Timers.Timer? _debounceTimer;
    private bool _enabled;
    private bool _disposed;

    private const double DebounceMilliseconds = 1500;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (_watcher is not null)
                _watcher.EnableRaisingEvents = _enabled;
        }
    }

    public event Action? OnReloadTriggered;

    public HotReloadWatcher(string projectDirectory)
    {
        if (!Directory.Exists(projectDirectory))
            return;

        _debounceTimer = new System.Timers.Timer(DebounceMilliseconds)
        {
            AutoReset = false
        };
        _debounceTimer.Elapsed += (_, _) => OnReloadTriggered?.Invoke();

        _watcher = new FileSystemWatcher(projectDirectory, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;

        _watcher.EnableRaisingEvents = _enabled;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_enabled || _debounceTimer is null)
            return;

        // Reset the debounce timer on each change
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_debounceTimer is not null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Dispose();
            _debounceTimer = null;
        }

        GC.SuppressFinalize(this);
    }
}
