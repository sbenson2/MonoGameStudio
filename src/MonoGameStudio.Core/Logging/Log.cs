namespace MonoGameStudio.Core.Logging;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

public enum LogSource
{
    Editor,
    GameStdOut,
    GameStdErr,
    Build
}

public readonly record struct LogEntry(string Message, LogLevel Level, DateTime Timestamp, LogSource Source = LogSource.Editor);

public static class Log
{
    private const int MaxEntries = 1024;
    private static readonly LogEntry[] _buffer = new LogEntry[MaxEntries];
    private static int _head;
    private static int _count;

    public static event Action<LogEntry>? OnLog;

    public static int Count => _count;

    public static LogEntry GetEntry(int index)
    {
        if (index < 0 || index >= _count)
            throw new IndexOutOfRangeException();
        int bufferIndex = (_head - _count + index + MaxEntries) % MaxEntries;
        return _buffer[bufferIndex];
    }

    public static void Info(string message) => AddEntry(message, LogLevel.Info, LogSource.Editor);
    public static void Warn(string message) => AddEntry(message, LogLevel.Warning, LogSource.Editor);
    public static void Error(string message) => AddEntry(message, LogLevel.Error, LogSource.Editor);

    public static void Info(string message, LogSource source) => AddEntry(message, LogLevel.Info, source);
    public static void Warn(string message, LogSource source) => AddEntry(message, LogLevel.Warning, source);
    public static void Error(string message, LogSource source) => AddEntry(message, LogLevel.Error, source);

    public static void Clear()
    {
        _count = 0;
        _head = 0;
    }

    private static void AddEntry(string message, LogLevel level, LogSource source)
    {
        var entry = new LogEntry(message, level, DateTime.Now, source);
        _buffer[_head] = entry;
        _head = (_head + 1) % MaxEntries;
        if (_count < MaxEntries) _count++;
        OnLog?.Invoke(entry);
    }
}
