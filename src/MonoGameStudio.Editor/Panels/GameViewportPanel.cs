using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Editor.Layout;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Panels;

public class GameViewportPanel
{
    private readonly ViewportRenderer _viewportRenderer;

    public Vector2 ViewportOrigin { get; private set; }
    public Vector2 ViewportSize { get; private set; }
    public bool IsHovered { get; private set; }
    public bool IsFocused { get; private set; }

    public GameViewportPanel(ViewportRenderer viewportRenderer)
    {
        _viewportRenderer = viewportRenderer;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin(LayoutDefinitions.Viewport, ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            var avail = ImGui.GetContentRegionAvail();
            ViewportSize = avail;
            ViewportOrigin = ImGui.GetCursorScreenPos();
            IsHovered = ImGui.IsWindowHovered();
            IsFocused = ImGui.IsWindowFocused();

            int w = (int)avail.X;
            int h = (int)avail.Y;

            if (w > 0 && h > 0)
            {
                _viewportRenderer.EnsureSize(w, h);

                if (_viewportRenderer.RenderTarget != null)
                {
                    // OpenGL Y-flip: uv0=(0,1), uv1=(1,0)
                    ImGui.Image(_viewportRenderer.TextureRef, avail,
                        new Vector2(0, 1), new Vector2(1, 0));
                }
            }
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }
}
