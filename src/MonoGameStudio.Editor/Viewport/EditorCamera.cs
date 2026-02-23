using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGameStudio.Editor.Viewport;

public class EditorCamera
{
    private Vector2 _position;
    private float _zoom = 1f;
    private bool _isPanning;
    private Vector2 _lastMousePos;

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Zoom
    {
        get => _zoom;
        set => _zoom = MathHelper.Clamp(value, 0.1f, 10f);
    }

    public Matrix GetViewMatrix()
    {
        return Matrix.CreateTranslation(-_position.X, -_position.Y, 0f) *
               Matrix.CreateScale(_zoom, _zoom, 1f);
    }

    public Matrix GetViewMatrix(Vector2 viewportSize)
    {
        return Matrix.CreateTranslation(-_position.X, -_position.Y, 0f) *
               Matrix.CreateScale(_zoom, _zoom, 1f) *
               Matrix.CreateTranslation(viewportSize.X / 2f, viewportSize.Y / 2f, 0f);
    }

    public Vector2 ScreenToWorld(Vector2 screenPos, Vector2 viewportSize)
    {
        var invView = Matrix.Invert(GetViewMatrix(viewportSize));
        return Vector2.Transform(screenPos, invView);
    }

    public Vector2 WorldToScreen(Vector2 worldPos, Vector2 viewportSize)
    {
        return Vector2.Transform(worldPos, GetViewMatrix(viewportSize));
    }

    public void Update(MouseState mouse, Vector2 viewportOrigin, Vector2 viewportSize, bool isViewportHovered)
    {
        if (!isViewportHovered) { _isPanning = false; return; }

        var mousePos = new Vector2(mouse.X, mouse.Y);
        var localMouse = mousePos - viewportOrigin;

        // Middle mouse pan
        if (mouse.MiddleButton == ButtonState.Pressed)
        {
            if (_isPanning)
            {
                var delta = (mousePos - _lastMousePos) / _zoom;
                _position -= delta;
            }
            _isPanning = true;
            _lastMousePos = mousePos;
        }
        else
        {
            _isPanning = false;
        }

        // Scroll zoom toward cursor
        int scrollDelta = mouse.ScrollWheelValue;
        if (_lastScrollValue != scrollDelta)
        {
            float scrollAmount = (scrollDelta - _lastScrollValue) / 120f;
            float oldZoom = _zoom;

            Zoom *= 1f + scrollAmount * 0.1f;

            // Zoom toward cursor position
            var worldBeforeZoom = ScreenToWorld(localMouse, viewportSize);
            var worldAfterZoom = ScreenToWorld(localMouse, viewportSize);
            _position += worldBeforeZoom - worldAfterZoom;
        }
        _lastScrollValue = scrollDelta;
    }

    private int _lastScrollValue;
}
