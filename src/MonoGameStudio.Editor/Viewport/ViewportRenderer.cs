using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Editor.ImGuiIntegration;

namespace MonoGameStudio.Editor.Viewport;

public class ViewportRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiManager _imGui;

    private RenderTarget2D? _renderTarget;
    private ImTextureRef _textureRef;
    private bool _hasBoundTexture;
    private int _width;
    private int _height;

    public RenderTarget2D? RenderTarget => _renderTarget;
    public ImTextureRef TextureRef => _textureRef;
    public int Width => _width;
    public int Height => _height;

    public ViewportRenderer(GraphicsDevice graphicsDevice, ImGuiManager imGui)
    {
        _graphicsDevice = graphicsDevice;
        _imGui = imGui;
    }

    public void EnsureSize(int width, int height)
    {
        // Debounce small changes (16px threshold)
        if (_renderTarget != null &&
            Math.Abs(_width - width) < 16 &&
            Math.Abs(_height - height) < 16)
            return;

        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        // Unbind old texture
        if (_hasBoundTexture)
        {
            _imGui.UnbindTexture(_textureRef);
            _hasBoundTexture = false;
        }

        _renderTarget?.Dispose();
        _renderTarget = new RenderTarget2D(_graphicsDevice, width, height,
            false, SurfaceFormat.Color, DepthFormat.None);
        _width = width;
        _height = height;

        _textureRef = _imGui.BindTexture(_renderTarget);
        _hasBoundTexture = true;
    }

    public void Begin()
    {
        if (_renderTarget == null) return;
        _graphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDevice.Clear(new Color(30, 30, 30));
    }

    public void End()
    {
        _graphicsDevice.SetRenderTarget(null);
    }
}
