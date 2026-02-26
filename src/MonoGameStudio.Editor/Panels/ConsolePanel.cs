using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class ConsolePanel
{
    private readonly ImGuiManager? _imGuiManager;

    private bool _showInfo = true;
    private bool _showWarnings = true;
    private bool _showErrors = true;
    private string _searchFilter = "";
    private bool _autoScroll = true;
    private bool _scrollToBottom;

    public ConsolePanel() : this(null) { }

    public ConsolePanel(ImGuiManager? imGuiManager)
    {
        _imGuiManager = imGuiManager;
        Log.OnLog += _ => _scrollToBottom = true;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.Console, ref isOpen))
        {
            // Filter toolbar
            if (ImGui.Button("Clear")) Log.Clear();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, _showInfo ? new Vector4(0.3f, 0.5f, 0.8f, 1f) : new Vector4(0.3f, 0.3f, 0.3f, 1f));
            if (ImGui.Button("Info")) _showInfo = !_showInfo;
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, _showWarnings ? new Vector4(0.8f, 0.7f, 0.2f, 1f) : new Vector4(0.3f, 0.3f, 0.3f, 1f));
            if (ImGui.Button("Warn")) _showWarnings = !_showWarnings;
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, _showErrors ? new Vector4(0.8f, 0.2f, 0.2f, 1f) : new Vector4(0.3f, 0.3f, 0.3f, 1f));
            if (ImGui.Button("Error")) _showErrors = !_showErrors;
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##search", ref _searchFilter, 256);

            ImGui.Separator();

            // Log entries
            if (ImGui.BeginChild("LogEntries", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                // Use console font if available
                bool pushedFont = false;
                unsafe
                {
                    if (_imGuiManager != null && _imGuiManager.ConsoleFont.Handle != null)
                    {
                        ImGui.PushFont(_imGuiManager.ConsoleFont, _imGuiManager.ConsoleFontSize);
                        pushedFont = true;
                    }
                }

                for (int i = 0; i < Log.Count; i++)
                {
                    var entry = Log.GetEntry(i);

                    if (!ShouldShow(entry)) continue;
                    if (!string.IsNullOrEmpty(_searchFilter) &&
                        !entry.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var color = entry.Level switch
                    {
                        LogLevel.Warning => new Vector4(1f, 0.9f, 0.4f, 1f),
                        LogLevel.Error => new Vector4(1f, 0.3f, 0.3f, 1f),
                        _ => new Vector4(0.9f, 0.9f, 0.9f, 1f)
                    };

                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    ImGui.TextUnformatted($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}");
                    ImGui.PopStyleColor();
                }

                if (pushedFont) ImGui.PopFont();

                if (_scrollToBottom && _autoScroll)
                {
                    ImGui.SetScrollHereY(1.0f);
                    _scrollToBottom = false;
                }

                // Track if user scrolled up
                if (ImGui.GetScrollY() < ImGui.GetScrollMaxY() - 10)
                    _autoScroll = false;
                else
                    _autoScroll = true;
            }
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private bool ShouldShow(LogEntry entry)
    {
        return entry.Level switch
        {
            LogLevel.Info => _showInfo,
            LogLevel.Warning => _showWarnings,
            LogLevel.Error => _showErrors,
            _ => true
        };
    }
}
