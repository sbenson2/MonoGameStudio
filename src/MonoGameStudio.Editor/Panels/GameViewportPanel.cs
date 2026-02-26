using System.Numerics;
using System.Text;
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

    /// <summary>
    /// Fired when an asset is dropped onto the viewport.
    /// Parameters: asset path, drop position (screen-local to viewport).
    /// </summary>
    public event Action<string, Vector2>? OnAssetDropped;

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

                    // Accept drag-drop from asset browser
                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("ASSET_PATH");
                        unsafe
                        {
                            if (payload.Handle != null && payload.Data != null)
                            {
                                var assetPath = Encoding.UTF8.GetString(
                                    (byte*)payload.Data, (int)payload.DataSize).TrimEnd('\0');
                                var mousePos = ImGui.GetMousePos();
                                var localPos = mousePos - ViewportOrigin;
                                OnAssetDropped?.Invoke(assetPath, localPos);
                            }
                        }
                        ImGui.EndDragDropTarget();
                    }
                }
            }
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }
}
