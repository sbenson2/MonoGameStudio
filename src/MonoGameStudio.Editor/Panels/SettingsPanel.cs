using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGuiStyler;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Project;

namespace MonoGameStudio.Editor.Panels;

public class SettingsPanel
{
    private readonly ImGuiManager _imGui;
    private readonly UserDataManager _userData;
    private readonly EditorPreferences _prefs;
    private EditorState? _editorState;

    private float _uiFontSize;
    private float _consoleFontSize;
    private bool _fontSizeChanged;

    public SettingsPanel(ImGuiManager imGui, UserDataManager userData, EditorPreferences prefs)
    {
        _imGui = imGui;
        _userData = userData;
        _prefs = prefs;

        _uiFontSize = prefs.UIFontSize;
        _consoleFontSize = prefs.ConsoleFontSize;
    }

    public void SetEditorState(EditorState editorState)
    {
        _editorState = editorState;
    }

    public void Draw(ref bool show)
    {
        if (!show) return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(340, 400), new Vector2(600, 800));
        if (ImGui.Begin("Settings", ref show))
        {
            DrawFontSection();
            ImGui.Spacing();
            DrawStyleSection();
            ImGui.Spacing();
            DrawThemeSection();
            ImGui.Spacing();
            DrawVirtualResolutionSection();
            ImGui.Spacing();
            DrawSafeAreaSection();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawResetButton();
        }
        ImGui.End();
    }

    private void DrawFontSection()
    {
        if (ImGui.CollapsingHeader("Fonts", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent(8);

            ImGui.Text("UI Font Size");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##UIFontSize", ref _uiFontSize, 12f, 28f, "%.0f px"))
            {
                _fontSizeChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit() && _fontSizeChanged)
            {
                _prefs.UIFontSize = _uiFontSize;
                _prefs.ConsoleFontSize = _consoleFontSize;
                _imGui.RequestFontRebuild(_uiFontSize, _consoleFontSize);
                SavePreferences();
                _fontSizeChanged = false;
            }

            ImGui.Spacing();

            ImGui.Text("Console Font Size");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##ConsoleFontSize", ref _consoleFontSize, 10f, 24f, "%.0f px"))
            {
                _fontSizeChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit() && _fontSizeChanged)
            {
                _prefs.UIFontSize = _uiFontSize;
                _prefs.ConsoleFontSize = _consoleFontSize;
                _imGui.RequestFontRebuild(_uiFontSize, _consoleFontSize);
                SavePreferences();
                _fontSizeChanged = false;
            }

            ImGui.Unindent(8);
        }
    }

    private void DrawStyleSection()
    {
        if (ImGui.CollapsingHeader("Style", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent(8);

            bool changed = false;

            // Rounding
            ImGui.Text("Rounding");
            float windowRounding = _prefs.WindowRounding;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##WindowRounding", ref windowRounding, 0f, 16f, "Window: %.0f"))
            {
                _prefs.WindowRounding = windowRounding;
                changed = true;
            }

            float frameRounding = _prefs.FrameRounding;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##FrameRounding", ref frameRounding, 0f, 16f, "Frame: %.0f"))
            {
                _prefs.FrameRounding = frameRounding;
                changed = true;
            }

            float tabRounding = _prefs.TabRounding;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##TabRounding", ref tabRounding, 0f, 16f, "Tab: %.0f"))
            {
                _prefs.TabRounding = tabRounding;
                changed = true;
            }

            ImGui.Spacing();

            // Spacing
            ImGui.Text("Spacing");
            float windowPaddingX = _prefs.WindowPaddingX;
            float windowPaddingY = _prefs.WindowPaddingY;
            var windowPadding = new Vector2(windowPaddingX, windowPaddingY);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat2("##WindowPadding", ref windowPadding, 0f, 24f, "%.0f"))
            {
                _prefs.WindowPaddingX = windowPadding.X;
                _prefs.WindowPaddingY = windowPadding.Y;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Win Pad");

            float itemSpacingX = _prefs.ItemSpacingX;
            float itemSpacingY = _prefs.ItemSpacingY;
            var itemSpacing = new Vector2(itemSpacingX, itemSpacingY);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat2("##ItemSpacing", ref itemSpacing, 0f, 20f, "%.0f"))
            {
                _prefs.ItemSpacingX = itemSpacing.X;
                _prefs.ItemSpacingY = itemSpacing.Y;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Item Sp");

            float indentSpacing = _prefs.IndentSpacing;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##IndentSpacing", ref indentSpacing, 8f, 40f, "Indent: %.0f"))
            {
                _prefs.IndentSpacing = indentSpacing;
                changed = true;
            }

            float scrollbarSize = _prefs.ScrollbarSize;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##ScrollbarSize", ref scrollbarSize, 6f, 24f, "Scrollbar: %.0f"))
            {
                _prefs.ScrollbarSize = scrollbarSize;
                changed = true;
            }

            if (changed)
            {
                _imGui.ApplyPreferences(_prefs);
                SavePreferences();
            }

            ImGui.Unindent(8);
        }
    }

    private void DrawThemeSection()
    {
        if (ImGui.CollapsingHeader("Theme", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent(8);

            if (ImGui.Button("Change Theme...", new Vector2(-1, 0)))
            {
                Theme.ShowThemeSelector("Select a Theme");
            }

            ImGui.Unindent(8);
        }
    }

    private void DrawVirtualResolutionSection()
    {
        if (_editorState == null) return;

        if (ImGui.CollapsingHeader("Virtual Resolution", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent(8);

            ImGui.Checkbox("Show Resolution Overlay", ref _editorState.ShowVirtualResolution);

            if (_editorState.ShowVirtualResolution)
            {
                ImGui.SetNextItemWidth(-1);
                var presets = EditorState.VirtualResolutionPresets;
                var labels = new string[presets.Length];
                for (int i = 0; i < presets.Length; i++)
                    labels[i] = presets[i].Label;

                int current = _editorState.VirtualResolutionPreset;
                if (ImGui.Combo("##Resolution", ref current, labels, labels.Length))
                {
                    _editorState.VirtualResolutionPreset = current;
                }

                var res = _editorState.CurrentVirtualResolution;
                ImGui.TextDisabled($"Target: {res.Width} x {res.Height}");
            }

            ImGui.Unindent(8);
        }
    }

    private void DrawSafeAreaSection()
    {
        if (_editorState == null) return;

        if (ImGui.CollapsingHeader("Safe Area"))
        {
            ImGui.Indent(8);

            ImGui.Checkbox("Show Safe Area Overlay", ref _editorState.ShowSafeArea);

            if (_editorState.ShowSafeArea)
            {
                ImGui.SetNextItemWidth(-1);
                var presets = EditorState.SafeAreaPresets;
                var labels = new string[presets.Length];
                for (int i = 0; i < presets.Length; i++)
                    labels[i] = presets[i].Label;

                int current = _editorState.SafeAreaPreset;
                if (ImGui.Combo("##SafeArea", ref current, labels, labels.Length))
                {
                    _editorState.SafeAreaPreset = current;
                }
            }

            ImGui.Checkbox("Integer Scaling (Pixel Art)", ref _editorState.UseIntegerScaling);

            ImGui.Unindent(8);
        }
    }

    private void DrawResetButton()
    {
        if (ImGui.Button("Reset to Defaults", new Vector2(-1, 30)))
        {
            var defaults = EditorPreferences.Default();
            _prefs.CopyFrom(defaults);
            _uiFontSize = defaults.UIFontSize;
            _consoleFontSize = defaults.ConsoleFontSize;
            _imGui.RequestFontRebuild(_uiFontSize, _consoleFontSize);
            _imGui.ApplyPreferences(_prefs);
            SavePreferences();
        }
    }

    private void SavePreferences()
    {
        _userData.SavePreferences(_prefs);
    }
}
