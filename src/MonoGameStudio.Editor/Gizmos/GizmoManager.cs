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

    // Bounding box state
    private BBoxHandle _hoveredHandle = BBoxHandle.None;
    private BBoxHandle _activeHandle = BBoxHandle.None;
    private const float DefaultEntitySize = 32f;
    private const float HandleHitSize = 10f;
    private const float HandleDrawSize = 7f;

    // Multi-select drag state
    private Vector2[] _multiDragStartPositions = Array.Empty<Vector2>();
    private Entity[] _multiDragEntities = Array.Empty<Entity>();

    // For undo: snapshot at start of drag
    public Vector2 DragStartPosition => _dragStartEntityPos;
    public float DragStartRotation => _dragStartRotation;
    public Vector2 DragStartScale => _dragStartScale;
    public bool WasDragging { get; private set; }
    public Vector2[] MultiDragStartPositions => _multiDragStartPositions;
    public Entity[] MultiDragEntities => _multiDragEntities;

    public GizmoMode CurrentMode { get; set; } = GizmoMode.BoundingBox;
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

        if (CurrentMode == GizmoMode.BoundingBox)
            return UpdateBoundingBox(mouse, entity, localMouse, viewportSize);

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
                    SnapshotMultiSelectPositions();
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

    private bool UpdateBoundingBox(MouseState mouse, Entity entity, Vector2 localMouse, Vector2 viewportSize)
    {
        var pos = _worldManager.World.Get<Position>(entity);
        var scale = _worldManager.World.Get<Scale>(entity);
        var entitySize = GetEntitySize(entity);
        var (screenMin, screenMax) = GetScreenBounds(pos, scale, entitySize, viewportSize);

        if (mouse.LeftButton == ButtonState.Pressed)
        {
            if (!_isDragging)
            {
                // Hit test handles, then body
                var hit = HitTestBoundingBox(localMouse, screenMin, screenMax);
                if (hit != BBoxHandle.None)
                {
                    _isDragging = true;
                    _activeHandle = hit;
                    _dragStartWorld = _camera.ScreenToWorld(localMouse, viewportSize);
                    _dragStartEntityPos = new Vector2(pos.X, pos.Y);
                    _dragStartRotation = _worldManager.World.Get<Rotation>(entity).Angle;
                    _dragStartScale = new Vector2(scale.X, scale.Y);
                    SnapshotMultiSelectPositions();
                }
            }

            if (_isDragging)
            {
                var worldMouse = _camera.ScreenToWorld(localMouse, viewportSize);
                ApplyBoundingBox(entity, worldMouse, entitySize);
                return true;
            }
        }
        else
        {
            if (_isDragging)
            {
                WasDragging = true;
                _isDragging = false;
                _activeHandle = BBoxHandle.None;
            }
            // Hover detection
            _hoveredHandle = HitTestBoundingBox(localMouse, screenMin, screenMax);
        }

        return _isDragging;
    }

    private Vector2 GetEntitySize(Entity entity)
    {
        var world = _worldManager.World;
        if (world.Has<SpriteRenderer>(entity))
        {
            var sprite = world.Get<SpriteRenderer>(entity);
            if (sprite.SourceRect.Width > 0 && sprite.SourceRect.Height > 0)
                return new Vector2(sprite.SourceRect.Width, sprite.SourceRect.Height);
        }
        return new Vector2(DefaultEntitySize, DefaultEntitySize);
    }

    private (Vector2 min, Vector2 max) GetScreenBounds(Position pos, Scale scale, Vector2 entitySize, Vector2 viewportSize)
    {
        float halfW = entitySize.X * scale.X * 0.5f;
        float halfH = entitySize.Y * scale.Y * 0.5f;

        var worldMin = new Vector2(pos.X - halfW, pos.Y - halfH);
        var worldMax = new Vector2(pos.X + halfW, pos.Y + halfH);

        var screenMin = _camera.WorldToScreen(worldMin, viewportSize);
        var screenMax = _camera.WorldToScreen(worldMax, viewportSize);

        // Ensure min < max in screen space
        return (
            new Vector2(MathF.Min(screenMin.X, screenMax.X), MathF.Min(screenMin.Y, screenMax.Y)),
            new Vector2(MathF.Max(screenMin.X, screenMax.X), MathF.Max(screenMin.Y, screenMax.Y))
        );
    }

    private BBoxHandle HitTestBoundingBox(Vector2 mousePos, Vector2 screenMin, Vector2 screenMax)
    {
        var handles = GizmoRenderer.GetHandlePositions(screenMin, screenMax);

        // Check corners first (higher priority)
        for (int i = 0; i < 4; i++)
        {
            if (Vector2.Distance(mousePos, handles[i]) < HandleHitSize)
                return (BBoxHandle)(i + 1);
        }

        // Edge midpoints
        for (int i = 4; i < 8; i++)
        {
            if (Vector2.Distance(mousePos, handles[i]) < HandleHitSize)
                return (BBoxHandle)(i + 1);
        }

        // Body (inside the box)
        if (mousePos.X >= screenMin.X && mousePos.X <= screenMax.X &&
            mousePos.Y >= screenMin.Y && mousePos.Y <= screenMax.Y)
        {
            return BBoxHandle.Body;
        }

        return BBoxHandle.None;
    }

    private void ApplyBoundingBox(Entity entity, Vector2 worldMouse, Vector2 entitySize)
    {
        var world = _worldManager.World;
        var worldDelta = worldMouse - _dragStartWorld;

        if (_activeHandle == BBoxHandle.Body)
        {
            // Move all selected entities
            for (int i = 0; i < _multiDragEntities.Length; i++)
            {
                if (world.IsAlive(_multiDragEntities[i]))
                {
                    var newPos = _multiDragStartPositions[i] + worldDelta;
                    world.Set(_multiDragEntities[i], new Position(newPos.X, newPos.Y));
                }
            }
        }
        else
        {
            // Scale based on handle
            ApplyScaleFromHandle(entity, worldDelta, entitySize);
        }
    }

    private void ApplyScaleFromHandle(Entity entity, Vector2 worldDelta, Vector2 entitySize)
    {
        var world = _worldManager.World;
        var newScale = _dragStartScale;
        var newPos = _dragStartEntityPos;

        // Determine which axes to affect based on handle
        bool affectsX = _activeHandle is BBoxHandle.TopLeft or BBoxHandle.TopRight
            or BBoxHandle.BottomLeft or BBoxHandle.BottomRight
            or BBoxHandle.Left or BBoxHandle.Right;
        bool affectsY = _activeHandle is BBoxHandle.TopLeft or BBoxHandle.TopRight
            or BBoxHandle.BottomLeft or BBoxHandle.BottomRight
            or BBoxHandle.Top or BBoxHandle.Bottom;

        // Determine direction sign (which side of the box the handle is on)
        float xSign = _activeHandle is BBoxHandle.TopRight or BBoxHandle.BottomRight or BBoxHandle.Right ? 1f : -1f;
        float ySign = _activeHandle is BBoxHandle.BottomLeft or BBoxHandle.BottomRight or BBoxHandle.Bottom ? 1f : -1f;

        // Check if Shift is held for proportional scaling on corners
        var keyboard = Keyboard.GetState();
        bool shiftHeld = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        bool isCorner = _activeHandle is BBoxHandle.TopLeft or BBoxHandle.TopRight
            or BBoxHandle.BottomLeft or BBoxHandle.BottomRight;

        if (affectsX)
        {
            float scaleDelta = (worldDelta.X * xSign) / (entitySize.X * 0.5f);
            newScale.X = MathF.Max(0.05f, _dragStartScale.X + scaleDelta * _dragStartScale.X * 0.5f);
            // Offset position to keep opposite edge anchored
            float posOffsetX = (newScale.X - _dragStartScale.X) * entitySize.X * 0.5f * xSign * 0.5f;
            newPos.X = _dragStartEntityPos.X + posOffsetX;
        }

        if (affectsY)
        {
            float scaleDelta = (worldDelta.Y * ySign) / (entitySize.Y * 0.5f);
            newScale.Y = MathF.Max(0.05f, _dragStartScale.Y + scaleDelta * _dragStartScale.Y * 0.5f);
            float posOffsetY = (newScale.Y - _dragStartScale.Y) * entitySize.Y * 0.5f * ySign * 0.5f;
            newPos.Y = _dragStartEntityPos.Y + posOffsetY;
        }

        // Proportional scaling with Shift on corners
        if (shiftHeld && isCorner && affectsX && affectsY)
        {
            float avgRatio = ((newScale.X / _dragStartScale.X) + (newScale.Y / _dragStartScale.Y)) * 0.5f;
            newScale.X = _dragStartScale.X * avgRatio;
            newScale.Y = _dragStartScale.Y * avgRatio;
            float posOffsetX = (newScale.X - _dragStartScale.X) * entitySize.X * 0.5f * xSign * 0.5f;
            float posOffsetY = (newScale.Y - _dragStartScale.Y) * entitySize.Y * 0.5f * ySign * 0.5f;
            newPos.X = _dragStartEntityPos.X + posOffsetX;
            newPos.Y = _dragStartEntityPos.Y + posOffsetY;
        }

        world.Set(entity, new Scale(newScale.X, newScale.Y));
        world.Set(entity, new Position(newPos.X, newPos.Y));
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
            case GizmoMode.BoundingBox:
                DrawBoundingBoxGizmo(spriteBatch, entity, viewportSize);
                break;
        }
    }

    private void DrawBoundingBoxGizmo(SpriteBatch spriteBatch, Entity entity, Vector2 viewportSize)
    {
        var pos = _worldManager.World.Get<Position>(entity);
        var scale = _worldManager.World.Get<Scale>(entity);
        var entitySize = GetEntitySize(entity);
        var (screenMin, screenMax) = GetScreenBounds(pos, scale, entitySize, viewportSize);

        var outlineColor = new Color(100, 180, 255);
        var activeOrHovered = _isDragging ? _activeHandle : _hoveredHandle;

        _renderer.DrawBoundingBox(spriteBatch, screenMin, screenMax, activeOrHovered, outlineColor, HandleDrawSize);
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
                var delta = Vector2.Zero;
                if (_activeAxis == GizmoAxis.X || _activeAxis == GizmoAxis.Center)
                    delta.X = worldDelta.X;
                if (_activeAxis == GizmoAxis.Y || _activeAxis == GizmoAxis.Center)
                    delta.Y = worldDelta.Y;

                // Move all selected entities
                for (int i = 0; i < _multiDragEntities.Length; i++)
                {
                    if (world.IsAlive(_multiDragEntities[i]))
                    {
                        var newPos = _multiDragStartPositions[i] + delta;
                        world.Set(_multiDragEntities[i], new Position(newPos.X, newPos.Y));
                    }
                }
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

    private void SnapshotMultiSelectPositions()
    {
        var selected = _editorState.SelectedEntities;
        _multiDragEntities = new Entity[selected.Count];
        _multiDragStartPositions = new Vector2[selected.Count];
        var world = _worldManager.World;

        for (int i = 0; i < selected.Count; i++)
        {
            _multiDragEntities[i] = selected[i];
            if (world.IsAlive(selected[i]))
            {
                var p = world.Get<Position>(selected[i]);
                _multiDragStartPositions[i] = new Vector2(p.X, p.Y);
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
