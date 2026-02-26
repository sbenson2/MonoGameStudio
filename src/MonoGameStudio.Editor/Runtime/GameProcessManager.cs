using System.Collections.Concurrent;
using System.Diagnostics;

namespace MonoGameStudio.Editor.Runtime;

public record struct OutputLine(string Text, bool IsError, DateTime Timestamp);

public class GameProcessManager : IDisposable
{
    private Process? _buildProcess;
    private Process? _runProcess;
    private readonly ConcurrentQueue<OutputLine> _outputQueue = new();

    public bool IsBuildRunning => _buildProcess is { HasExited: false };
    public bool IsProcessRunning => _runProcess is { HasExited: false };
    public bool? LastBuildSuccess { get; private set; }

    public event Action? OnBuildStarted;
    public event Action<bool>? OnBuildCompleted;
    public event Action? OnProcessStarted;
    public event Action? OnProcessExited;

    public void Build(string projectPath, string configuration = "Debug")
    {
        if (IsBuildRunning) return;

        LastBuildSuccess = null;
        OnBuildStarted?.Invoke();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c {configuration}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _buildProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _buildProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                _outputQueue.Enqueue(new OutputLine(e.Data, false, DateTime.Now));
        };

        _buildProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                _outputQueue.Enqueue(new OutputLine(e.Data, true, DateTime.Now));
        };

        _buildProcess.Exited += (_, _) =>
        {
            bool success = _buildProcess.ExitCode == 0;
            LastBuildSuccess = success;
            OnBuildCompleted?.Invoke(success);
        };

        _buildProcess.Start();
        _buildProcess.BeginOutputReadLine();
        _buildProcess.BeginErrorReadLine();
    }

    public void Run(string projectPath, string configuration = "Debug")
    {
        if (IsProcessRunning) return;

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -c {configuration}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _runProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _runProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                _outputQueue.Enqueue(new OutputLine(e.Data, false, DateTime.Now));
        };

        _runProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                _outputQueue.Enqueue(new OutputLine(e.Data, true, DateTime.Now));
        };

        _runProcess.Exited += (_, _) =>
        {
            OnProcessExited?.Invoke();
        };

        _runProcess.Start();
        _runProcess.BeginOutputReadLine();
        _runProcess.BeginErrorReadLine();

        OnProcessStarted?.Invoke();
    }

    public void Stop()
    {
        if (_runProcess is { HasExited: false })
        {
            try
            {
                _runProcess.Kill(entireProcessTree: true);
                _runProcess.WaitForExit(3000);
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }
    }

    public List<OutputLine> PollOutput()
    {
        var lines = new List<OutputLine>();
        while (_outputQueue.TryDequeue(out var line))
            lines.Add(line);
        return lines;
    }

    public void Dispose()
    {
        Stop();

        if (_buildProcess is { HasExited: false })
        {
            try { _buildProcess.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }

        _buildProcess?.Dispose();
        _runProcess?.Dispose();

        GC.SuppressFinalize(this);
    }
}
