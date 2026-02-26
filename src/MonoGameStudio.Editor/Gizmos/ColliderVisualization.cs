using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Gizmos;

/// <summary>
/// Draws wireframe outlines for BoxCollider and CircleCollider components
/// in the editor viewport (edit mode only).
/// </summary>
public class ColliderVisualization
{
    private readonly WorldManager _worldManager;
    private readonly GizmoRenderer _renderer;
    private readonly EditorCamera _camera;

    private static readonly Color ColliderColor = new(0, 255, 0, 160);

    public bool Enabled { get; set; } = true;

    public ColliderVisualization(WorldManager worldManager, GizmoRenderer renderer, EditorCamera camera)
    {
        _worldManager = worldManager;
        _renderer = renderer;
        _camera = camera;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 viewportSize)
    {
        if (!Enabled) return;

        var world = _worldManager.World;

        // Draw box colliders
        var boxQuery = new QueryDescription().WithAll<BoxCollider, Position>();
        world.Query(boxQuery, (Entity entity, ref BoxCollider box, ref Position pos) =>
        {
            var scale = world.Has<Scale>(entity) ? world.Get<Scale>(entity) : new Scale(1, 1);
            float halfW = box.Width * scale.X * 0.5f;
            float halfH = box.Height * scale.Y * 0.5f;
            float cx = pos.X + box.Offset.X;
            float cy = pos.Y + box.Offset.Y;

            var worldMin = new Vector2(cx - halfW, cy - halfH);
            var worldMax = new Vector2(cx + halfW, cy + halfH);

            var screenMin = _camera.WorldToScreen(worldMin, viewportSize);
            var screenMax = _camera.WorldToScreen(worldMax, viewportSize);

            _renderer.DrawRectOutline(spriteBatch, screenMin, screenMax, ColliderColor, 1);
        });

        // Draw circle colliders
        var circleQuery = new QueryDescription().WithAll<CircleCollider, Position>();
        world.Query(circleQuery, (Entity entity, ref CircleCollider circle, ref Position pos) =>
        {
            var scale = world.Has<Scale>(entity) ? world.Get<Scale>(entity) : new Scale(1, 1);
            float maxScale = MathF.Max(MathF.Abs(scale.X), MathF.Abs(scale.Y));
            float worldRadius = circle.Radius * maxScale;
            float cx = pos.X + circle.Offset.X;
            float cy = pos.Y + circle.Offset.Y;

            var screenCenter = _camera.WorldToScreen(new Vector2(cx, cy), viewportSize);
            // Convert world radius to screen radius
            var screenEdge = _camera.WorldToScreen(new Vector2(cx + worldRadius, cy), viewportSize);
            float screenRadius = MathF.Abs(screenEdge.X - screenCenter.X);

            _renderer.DrawCircle(spriteBatch, screenCenter, screenRadius, ColliderColor, 32, 1);
        });
    }
}
