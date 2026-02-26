using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Runtime;

namespace MonoGameStudio.Editor.Panels;

public class GameRunPanel
{
    private readonly GameProcessManager _processManager;
    private readonly List<BuildProfileConfig> _profiles;
    private int _selectedProfileIndex;

    private bool _autoReload;
    private readonly List<string> _buildOutput = new();
    private const int MaxBuildOutputLines = 500;

    private enum RunStatus { Idle, Building, Running }
    private RunStatus _status = RunStatus.Idle;

    public bool AutoReloadEnabled => _autoReload;
    public event Action? OnBuildRequested;
    public event Action? OnBuildAndRunRequested;
    public event Action? OnStopRequested;

    public GameRunPanel(GameProcessManager processManager, List<BuildProfileConfig>? profiles = null)
    {
        _processManager = processManager;
        _profiles = profiles ?? [
            new BuildProfileConfig { Name = "Debug", Configuration = "Debug" },
            new BuildProfileConfig { Name = "Release", Configuration = "Release" }
        ];

        _processManager.OnBuildStarted += () =>
        {
            _status = RunStatus.Building;
            _buildOutput.Clear();
        };

        _processManager.OnBuildCompleted += success =>
        {
            if (!_processManager.IsProcessRunning)
                _status = RunStatus.Idle;
        };

        _processManager.OnProcessStarted += () => _status = RunStatus.Running;
        _processManager.OnProcessExited += () => _status = RunStatus.Idle;
    }

    public BuildProfileConfig SelectedProfile =>
        _selectedProfileIndex >= 0 && _selectedProfileIndex < _profiles.Count
            ? _profiles[_selectedProfileIndex]
            : _profiles[0];

    public void Update()
    {
        // Drain process output into build log and Log system
        var lines = _processManager.PollOutput();
        foreach (var line in lines)
        {
            if (_buildOutput.Count >= MaxBuildOutputLines)
                _buildOutput.RemoveAt(0);
            _buildOutput.Add(line.Text);

            var source = _processManager.IsBuildRunning ? LogSource.Build
                : line.IsError ? LogSource.GameStdErr
                : LogSource.GameStdOut;

            if (line.IsError)
                Log.Error(line.Text, source);
            else
                Log.Info(line.Text, source);
        }
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;

        if (ImGui.Begin("Game Run", ref isOpen))
        {
            // === Action Buttons ===
            bool canBuild = !_processManager.IsBuildRunning;
            bool canRun = !_processManager.IsProcessRunning && !_processManager.IsBuildRunning;
            bool canStop = _processManager.IsProcessRunning;

            if (!canBuild) ImGui.BeginDisabled();
            if (ImGui.Button($"{FontAwesomeIcons.Hammer} Build"))
                OnBuildRequested?.Invoke();
            if (!canBuild) ImGui.EndDisabled();

            ImGui.SameLine();

            if (!canRun) ImGui.BeginDisabled();
            if (ImGui.Button($"{FontAwesomeIcons.Play} Build & Run"))
                OnBuildAndRunRequested?.Invoke();
            if (!canRun) ImGui.EndDisabled();

            ImGui.SameLine();

            if (!canStop) ImGui.BeginDisabled();
            if (ImGui.Button($"{FontAwesomeIcons.Stop} Stop"))
                OnStopRequested?.Invoke();
            if (!canStop) ImGui.EndDisabled();

            ImGui.Separator();

            // === Profile Dropdown ===
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Profile:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);

            var profileNames = _profiles.Select(p => p.Name).ToArray();
            var currentName = SelectedProfile.Name;
            if (ImGui.BeginCombo("##Profile", currentName))
            {
                for (int i = 0; i < profileNames.Length; i++)
                {
                    bool isSelected = i == _selectedProfileIndex;
                    if (ImGui.Selectable(profileNames[i], isSelected))
                        _selectedProfileIndex = i;
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // === Status Indicator ===
            var (statusColor, statusText) = _status switch
            {
                RunStatus.Building => (new Vector4(0.9f, 0.8f, 0.2f, 1f), "Building"),
                RunStatus.Running => (new Vector4(0.2f, 0.9f, 0.3f, 1f), "Running"),
                _ => (new Vector4(0.5f, 0.5f, 0.5f, 1f), "Idle")
            };

            var drawList = ImGui.GetWindowDrawList();
            var cursorScreen = ImGui.GetCursorScreenPos();
            float dotRadius = 5f;
            float textOffsetY = (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) * 0.5f;
            drawList.AddCircleFilled(
                new Vector2(cursorScreen.X + dotRadius, cursorScreen.Y + ImGui.GetFrameHeight() * 0.5f),
                dotRadius,
                ImGui.ColorConvertFloat4ToU32(statusColor));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + dotRadius * 2 + 6);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(statusText);

            // === Auto-Reload Toggle ===
            ImGui.Checkbox("Auto-reload on save", ref _autoReload);

            ImGui.Separator();

            // === Build Output ===
            ImGui.Text("Build Output:");

            float availableHeight = ImGui.GetContentRegionAvail().Y;
            if (ImGui.BeginChild("BuildOutput", new Vector2(0, availableHeight), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar))
            {
                for (int i = 0; i < _buildOutput.Count; i++)
                {
                    ImGui.TextUnformatted(_buildOutput[i]);
                }

                // Auto-scroll to bottom when building
                if (_status == RunStatus.Building)
                    ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();
        }
        ImGui.End();
    }
}
