using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Panels;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Gizmos;

public enum GizmoAxis
{
    None,
    X,
    Y,
    Center
}

public class GizmoManager
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private readonly GizmoRenderer _renderer;
    private readonly EditorCamera _camera;

    private GizmoAxis _hoveredAxis = GizmoAxis.None;
    private GizmoAxis _activeAxis = GizmoAxis.None;
    private bool _isDragging;
    private Vector2 _dragStartWorld;
    private Vector2 _dragStartEntityPos;
    private float _dragStartRotation;
    private Vector2 _dragStartScale;

    // For undo: snapshot at start of drag
    public Vector2 DragStartPosition => _dragStartEntityPos;
    public float DragStartRotation => _dragStartRotation;
    public Vector2 DragStartScale => _dragStartScale;
    public bool WasDragging { get; private set; }

    public GizmoMode CurrentMode { get; set; } = GizmoMode.Move;
    public bool IsActive => _isDragging;

    private const float ArrowLength = 60f;
    private const float HitThreshold = 12f;
    private const float CenterSquareSize = 10f;
    private const float RotateRadius = 50f;
    private const float ScaleHandleSize = 8f;

    public GizmoManager(WorldManager worldManager, EditorState editorState,
        GizmoRenderer renderer, EditorCamera camera)
    {
        _worldManager = worldManager;
        _editorState = editorState;
        _renderer = renderer;
        _camera = camera;
    }

    public bool Update(MouseState mouse, Vector2 viewportOrigin, Vector2 viewportSize)
    {
        WasDragging = false;
        var primary = _editorState.PrimarySelection;
        if (!primary.HasValue || !_worldManager.World.IsAlive(primary.Value))
        {
            _isDragging = false;
            return false;
        }

        if (CurrentMode == GizmoMode.None) return false;

        var entity = primary.Value;
        var pos = _worldManager.World.Get<Position>(entity);
        var screenPos = _camera.WorldToScreen(new Vector2(pos.X, pos.Y), viewportSize);
        var localMouse = new Vector2(mouse.X, mouse.Y) - viewportOrigin;
        float zoom = _camera.Zoom;

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            if (!_isDragging)
            {
                // Hit test
                _hoveredAxis = HitTest(localMouse, screenPos, zoom);
                if (_hoveredAxis != GizmoAxis.None)
                {
                    _isDragging = true;
                    _activeAxis = _hoveredAxis;
                    _dragStartWorld = _camera.ScreenToWorld(localMouse, viewportSize);
                    _dragStartEntityPos = new Vector2(pos.X, pos.Y);
                    _dragStartRotation = _worldManager.World.Get<Rotation>(entity).Angle;
                    var s = _worldManager.World.Get<Scale>(entity);
                    _dragStartScale = new Vector2(s.X, s.Y);
                }
            }

            if (_isDragging)
            {
                var worldMouse = _camera.ScreenToWorld(localMouse, viewportSize);
                ApplyGizmo(entity, worldMouse);
                return true; // Consumed input
            }
        }
        else
        {
            if (_isDragging)
            {
                WasDragging = true;
                _isDragging = false;
                _activeAxis = GizmoAxis.None;
            }
            // Hover detection
            _hoveredAxis = HitTest(localMouse, screenPos, zoom);
        }

        return _isDragging;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 viewportSize)
    {
        var primary = _editorState.PrimarySelection;
        if (!primary.HasValue || !_worldManager.World.IsAlive(primary.Value)) return;
        if (CurrentMode == GizmoMode.None) return;

        var entity = primary.Value;
        var pos = _worldManager.World.Get<Position>(entity);
        var screenPos = _camera.WorldToScreen(new Vector2(pos.X, pos.Y), viewportSize);
        float zoom = _camera.Zoom;

        switch (CurrentMode)
        {
            case GizmoMode.Move:
                DrawMoveGizmo(spriteBatch, screenPos, zoom);
                break;
            case GizmoMode.Rotate:
                DrawRotateGizmo(spriteBatch, screenPos, zoom);
                break;
            case GizmoMode.Scale:
                DrawScaleGizmo(spriteBatch, screenPos, zoom);
                break;
        }
    }

    private void DrawMoveGizmo(SpriteBatch spriteBatch, Vector2 center, float zoom)
    {
        var xColor = _hoveredAxis == GizmoAxis.X || _activeAxis == GizmoAxis.X
            ? Color.Yellow : new Color(220, 60, 60);
        var yColor = _hoveredAxis == GizmoAxis.Y || _activeAxis == GizmoAxis.Y
            ? Color.Yellow : new Color(60, 220, 60);
        var cColor = _hoveredAxis == GizmoAxis.Center || _activeAxis == GizmoAxis.Center
            ? Color.Yellow : new Color(200, 200, 200);

        _renderer.DrawArrow(spriteBatch, center, center + new Vector2(ArrowLength, 0), xColor, 2, 10);
        _renderer.DrawArrow(spriteBatch, center, center + new Vector2(0, -ArrowLength), yColor, 2, 10);
        _renderer.DrawRect(spriteBatch, center, CenterSquareSize, cColor);
    }

    private void DrawRotateGizmo(SpriteBatch spriteBatch, Vector2 center, float zoom)
    {
        var color = _hoveredAxis == GizmoAxis.Center || _activeAxis == GizmoAxis.Center
            ? Color.Yellow : new Color(100, 180, 255);
        _renderer.DrawCircle(spriteBatch, center, RotateRadius, color, 48, 2);
    }

    private void DrawScaleGizmo(SpriteBatch spriteBatch, Vector2 center, float zoom)
    {
        var xColor = _hoveredAxis == GizmoAxis.X || _activeAxis == GizmoAxis.X
            ? Color.Yellow : new Color(220, 60, 60);
        var yColor = _hoveredAxis == GizmoAxis.Y || _activeAxis == GizmoAxis.Y
            ? Color.Yellow : new Color(60, 220, 60);
        var cColor = _hoveredAxis == GizmoAxis.Center || _activeAxis == GizmoAxis.Center
            ? Color.Yellow : new Color(200, 200, 200);

        // X axis with square handle
        _renderer.DrawLine(spriteBatch, center, center + new Vector2(ArrowLength, 0), xColor, 2);
        _renderer.DrawRect(spriteBatch, center + new Vector2(ArrowLength, 0), ScaleHandleSize, xColor);

        // Y axis with square handle
        _renderer.DrawLine(spriteBatch, center, center + new Vector2(0, -ArrowLength), yColor, 2);
        _renderer.DrawRect(spriteBatch, center + new Vector2(0, -ArrowLength), ScaleHandleSize, yColor);

        // Center handle
        _renderer.DrawRect(spriteBatch, center, CenterSquareSize, cColor);
    }

    private GizmoAxis HitTest(Vector2 mousePos, Vector2 gizmoCenter, float zoom)
    {
        switch (CurrentMode)
        {
            case GizmoMode.Move:
            case GizmoMode.Scale:
            {
                // Center square
                if (Vector2.Distance(mousePos, gizmoCenter) < CenterSquareSize)
                    return GizmoAxis.Center;

                // X axis
                var xEnd = gizmoCenter + new Vector2(ArrowLength, 0);
                if (DistanceToSegment(mousePos, gizmoCenter, xEnd) < HitThreshold)
                    return GizmoAxis.X;

                // Y axis
                var yEnd = gizmoCenter + new Vector2(0, -ArrowLength);
                if (DistanceToSegment(mousePos, gizmoCenter, yEnd) < HitThreshold)
                    return GizmoAxis.Y;

                break;
            }
            case GizmoMode.Rotate:
            {
                float dist = Vector2.Distance(mousePos, gizmoCenter);
                if (Math.Abs(dist - RotateRadius) < HitThreshold)
                    return GizmoAxis.Center;
                break;
            }
        }

        return GizmoAxis.None;
    }

    private void ApplyGizmo(Entity entity, Vector2 worldMouse)
    {
        var world = _worldManager.World;
        var worldDelta = worldMouse - _dragStartWorld;

        switch (CurrentMode)
        {
            case GizmoMode.Move:
            {
                var newPos = _dragStartEntityPos;
                if (_activeAxis == GizmoAxis.X || _activeAxis == GizmoAxis.Center)
                    newPos.X += worldDelta.X;
                if (_activeAxis == GizmoAxis.Y || _activeAxis == GizmoAxis.Center)
                    newPos.Y += worldDelta.Y;

                world.Set(entity, new Position(newPos.X, newPos.Y));
                break;
            }
            case GizmoMode.Rotate:
            {
                var entityPos = _dragStartEntityPos;
                var startAngle = MathF.Atan2(_dragStartWorld.Y - entityPos.Y, _dragStartWorld.X - entityPos.X);
                var currentAngle = MathF.Atan2(worldMouse.Y - entityPos.Y, worldMouse.X - entityPos.X);
                var delta = currentAngle - startAngle;

                world.Set(entity, new Rotation(_dragStartRotation + delta));
                break;
            }
            case GizmoMode.Scale:
            {
                float sensitivity = 0.01f;
                var newScale = _dragStartScale;

                if (_activeAxis == GizmoAxis.X || _activeAxis == GizmoAxis.Center)
                    newScale.X = MathF.Max(0.01f, _dragStartScale.X + worldDelta.X * sensitivity);
                if (_activeAxis == GizmoAxis.Y || _activeAxis == GizmoAxis.Center)
                    newScale.Y = MathF.Max(0.01f, _dragStartScale.Y - worldDelta.Y * sensitivity);

                world.Set(entity, new Scale(newScale.X, newScale.Y));
                break;
            }
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq < 0.001f) return Vector2.Distance(point, a);

        float t = MathHelper.Clamp(Vector2.Dot(point - a, ab) / lengthSq, 0f, 1f);
        var closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }
}
