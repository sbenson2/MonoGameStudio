namespace MonoGameStudio.Editor.Editor;

public class EditorPreferences
{
    // Font sizes (before DPI scaling)
    public float UIFontSize { get; set; } = 16f;
    public float ConsoleFontSize { get; set; } = 15f;

    // Style - Rounding
    public float WindowRounding { get; set; } = 12f;
    public float FrameRounding { get; set; } = 8f;
    public float GrabRounding { get; set; } = 8f;
    public float ScrollbarRounding { get; set; } = 10f;
    public float TabRounding { get; set; } = 8f;
    public float PopupRounding { get; set; } = 10f;
    public float ChildRounding { get; set; } = 8f;

    // Style - Spacing
    public float WindowPaddingX { get; set; } = 14f;
    public float WindowPaddingY { get; set; } = 14f;
    public float FramePaddingX { get; set; } = 12f;
    public float FramePaddingY { get; set; } = 7f;
    public float ItemSpacingX { get; set; } = 12f;
    public float ItemSpacingY { get; set; } = 8f;
    public float IndentSpacing { get; set; } = 22f;
    public float ScrollbarSize { get; set; } = 13f;

    // Style - Borders
    public float WindowBorderSize { get; set; } = 1f;
    public float ChildBorderSize { get; set; } = 1f;
    public float FrameBorderSize { get; set; } = 0f;
    public float TabBorderSize { get; set; } = 0f;

    // Theme
    public string ThemeName { get; set; } = "Catppuccin.Mocha";

    public static EditorPreferences Default() => new();

    public void CopyFrom(EditorPreferences other)
    {
        UIFontSize = other.UIFontSize;
        ConsoleFontSize = other.ConsoleFontSize;
        WindowRounding = other.WindowRounding;
        FrameRounding = other.FrameRounding;
        GrabRounding = other.GrabRounding;
        ScrollbarRounding = other.ScrollbarRounding;
        TabRounding = other.TabRounding;
        PopupRounding = other.PopupRounding;
        ChildRounding = other.ChildRounding;
        WindowPaddingX = other.WindowPaddingX;
        WindowPaddingY = other.WindowPaddingY;
        FramePaddingX = other.FramePaddingX;
        FramePaddingY = other.FramePaddingY;
        ItemSpacingX = other.ItemSpacingX;
        ItemSpacingY = other.ItemSpacingY;
        IndentSpacing = other.IndentSpacing;
        ScrollbarSize = other.ScrollbarSize;
        WindowBorderSize = other.WindowBorderSize;
        ChildBorderSize = other.ChildBorderSize;
        FrameBorderSize = other.FrameBorderSize;
        TabBorderSize = other.TabBorderSize;
        ThemeName = other.ThemeName;
    }
}
