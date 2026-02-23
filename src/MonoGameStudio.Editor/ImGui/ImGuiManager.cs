using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameStudio.Editor.ImGuiIntegration;

/// <summary>
/// High-level wrapper managing ImGui lifecycle, docking, and texture binding.
/// </summary>
public class ImGuiManager
{
    private readonly ImGuiRenderer _renderer;
    private bool _initialized;
    private IntPtr _iniPathPtr;

    public ImGuiRenderer Renderer => _renderer;

    public ImGuiManager(Game game)
    {
        _renderer = new ImGuiRenderer(game);
    }

    public void Initialize(string iniPath)
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // IniFilename needs a stable native string pointer
        var bytes = Encoding.UTF8.GetBytes(iniPath + '\0');
        _iniPathPtr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, _iniPathPtr, bytes.Length);
        unsafe { io.NativePtr->IniFilename = (byte*)_iniPathPtr; }

        _renderer.RebuildFontAtlas();
        _initialized = true;

        // Set dark theme
        ImGui.StyleColorsDark();

        // Tweak style
        var style = ImGui.GetStyle();
        style.WindowRounding = 2f;
        style.FrameRounding = 2f;
        style.GrabRounding = 2f;
        style.ScrollbarRounding = 2f;
        style.TabRounding = 2f;
    }

    public void BeginFrame(GameTime gameTime)
    {
        if (!_initialized) return;
        _renderer.BeforeLayout(gameTime);
    }

    public void EndFrame()
    {
        if (!_initialized) return;
        _renderer.AfterLayout();
    }

    public IntPtr BindTexture(Texture2D texture) => _renderer.BindTexture(texture);
    public void UnbindTexture(IntPtr id) => _renderer.UnbindTexture(id);
}
