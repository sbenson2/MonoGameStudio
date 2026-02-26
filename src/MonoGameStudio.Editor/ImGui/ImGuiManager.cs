using System.Runtime.InteropServices;
using System.Text;
using Hexa.NET.ImGui;
using ktsu.ImGuiStyler;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Editor.Editor;

namespace MonoGameStudio.Editor.ImGuiIntegration;

public class ImGuiManager
{
    private readonly ImGuiRenderer _renderer;
    private bool _initialized;
    private IntPtr _iniPathPtr;
    private float _dpiScale = 1f;

    public ImGuiRenderer Renderer => _renderer;

    /// <summary>Console/code font (JetBrains Mono). Null until Initialize() is called.</summary>
    public ImFontPtr ConsoleFont { get; private set; }

    /// <summary>Base font size before DPI scaling.</summary>
    public float ConsoleFontSize { get; private set; } = 15.0f;

    /// <summary>Base UI font size before DPI scaling.</summary>
    public float UIFontSize { get; private set; } = 16.0f;

    /// <summary>Whether fonts need to be rebuilt next frame.</summary>
    public bool FontsNeedRebuild { get; private set; }

    public ImGuiManager(Game game)
    {
        _renderer = new ImGuiRenderer(game);
    }

    public void Initialize(string iniPath, EditorPreferences? prefs = null)
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // IniFilename needs a stable native string pointer
        var bytes = Encoding.UTF8.GetBytes(iniPath + '\0');
        _iniPathPtr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, _iniPathPtr, bytes.Length);
        unsafe { io.Handle->IniFilename = (byte*)_iniPathPtr; }

        _dpiScale = _renderer.GetDpiScale();

        // Apply saved font sizes if prefs provided
        if (prefs != null)
        {
            UIFontSize = prefs.UIFontSize;
            ConsoleFontSize = prefs.ConsoleFontSize;
        }

        // Load fonts
        LoadFonts(io, _dpiScale);

        _initialized = true;

        // Apply theme and style
        ApplyPreferences(prefs ?? new EditorPreferences());
    }

    public void ApplyPreferences(EditorPreferences prefs)
    {
        // Apply theme
        Theme.Apply(prefs.ThemeName);

        // Modern style tweaks on top of theme
        var style = ImGui.GetStyle();

        // Rounding
        style.WindowRounding = prefs.WindowRounding;
        style.FrameRounding = prefs.FrameRounding;
        style.GrabRounding = prefs.GrabRounding;
        style.ScrollbarRounding = prefs.ScrollbarRounding;
        style.TabRounding = prefs.TabRounding;
        style.PopupRounding = prefs.PopupRounding;
        style.ChildRounding = prefs.ChildRounding;

        // Spacing
        style.WindowPadding = new System.Numerics.Vector2(prefs.WindowPaddingX, prefs.WindowPaddingY);
        style.FramePadding = new System.Numerics.Vector2(prefs.FramePaddingX, prefs.FramePaddingY);
        style.ItemSpacing = new System.Numerics.Vector2(prefs.ItemSpacingX, prefs.ItemSpacingY);
        style.ItemInnerSpacing = new System.Numerics.Vector2(8, 6);
        style.IndentSpacing = prefs.IndentSpacing;
        style.ScrollbarSize = prefs.ScrollbarSize;
        style.GrabMinSize = 10f;

        // Borders
        style.WindowBorderSize = prefs.WindowBorderSize;
        style.ChildBorderSize = prefs.ChildBorderSize;
        style.FrameBorderSize = prefs.FrameBorderSize;
        style.TabBorderSize = prefs.TabBorderSize;
        style.SeparatorTextBorderSize = 2f;

        // Anti-aliasing
        style.AntiAliasedLines = true;
        style.AntiAliasedLinesUseTex = true;
        style.AntiAliasedFill = true;

        style.ScaleAllSizes(_dpiScale);
    }

    public void RequestFontRebuild(float uiFontSize, float consoleFontSize)
    {
        UIFontSize = uiFontSize;
        ConsoleFontSize = consoleFontSize;
        FontsNeedRebuild = true;
    }

    public void RebuildFontsIfNeeded()
    {
        if (!FontsNeedRebuild) return;
        FontsNeedRebuild = false;

        var io = ImGui.GetIO();
        io.Fonts.Clear();
        LoadFonts(io, _dpiScale);
        // Hexa.NET.ImGui 2.x texture protocol handles atlas rebuild automatically
        // via ProcessTextureUpdates (WantCreate/WantDestroy) on next render
    }

    private unsafe void LoadFonts(ImGuiIOPtr io, float dpiScale)
    {
        string fontDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Fonts");

        float scaledUISize = UIFontSize * dpiScale;
        float scaledConsoleSize = ConsoleFontSize * dpiScale;

        // 1. Inter as default UI font with oversampling for crisp Retina text
        string interPath = Path.Combine(fontDir, "Inter-Regular.ttf");
        if (File.Exists(interPath))
        {
            var uiConfig = ImGui.ImFontConfig();
            uiConfig.OversampleH = 2;
            uiConfig.OversampleV = 2;
            uiConfig.PixelSnapH = false;

            io.Fonts.AddFontFromFileTTF(interPath, scaledUISize, uiConfig);

            // 2. Merge FontAwesome icons into UI font
            string faPath = Path.Combine(fontDir, "fa-solid-900.ttf");
            if (File.Exists(faPath))
            {
                var iconConfig = ImGui.ImFontConfig();
                iconConfig.MergeMode = true;
                iconConfig.PixelSnapH = true;
                iconConfig.OversampleH = 2;
                iconConfig.OversampleV = 2;
                iconConfig.GlyphMinAdvanceX = scaledUISize;

                uint* glyphRanges = stackalloc uint[3];
                glyphRanges[0] = 0xe005;
                glyphRanges[1] = 0xf8ff;
                glyphRanges[2] = 0;

                io.Fonts.AddFontFromFileTTF(faPath, scaledUISize, iconConfig, glyphRanges);
            }
        }

        // 3. JetBrains Mono as separate console font with oversampling
        string jbPath = Path.Combine(fontDir, "JetBrainsMono-Regular.ttf");
        if (File.Exists(jbPath))
        {
            var consoleConfig = ImGui.ImFontConfig();
            consoleConfig.OversampleH = 2;
            consoleConfig.OversampleV = 2;
            consoleConfig.PixelSnapH = false;

            ConsoleFont = io.Fonts.AddFontFromFileTTF(jbPath, scaledConsoleSize, consoleConfig);
        }
    }

    public void BeginFrame(GameTime gameTime)
    {
        if (!_initialized) return;
        RebuildFontsIfNeeded();
        _renderer.BeforeLayout(gameTime);
    }

    public void EndFrame()
    {
        if (!_initialized) return;

        // Render the theme selector popup if open
        Theme.RenderThemeSelector();

        _renderer.AfterLayout();
    }

    public ImTextureRef BindTexture(Texture2D texture) => _renderer.BindTexture(texture);
    public void UnbindTexture(ImTextureRef id) => _renderer.UnbindTexture(id);
}
