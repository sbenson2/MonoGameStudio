using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Gizmos;

/// <summary>
/// Draws range circles around AudioSource entities in the editor viewport (edit mode only).
/// Circle radius is proportional to the AudioSource volume.
/// </summary>
public class AudioVisualization
{
    private readonly WorldManager _worldManager;
    private readonly GizmoRenderer _renderer;
    private readonly EditorCamera _camera;

    private static readonly Color AudioRangeColor = new(80, 220, 120, 100);

    /// <summary>
    /// Base world-unit radius for an AudioSource at volume 1.0.
    /// </summary>
    public float BaseRange { get; set; } = 128f;

    public bool Enabled { get; set; } = true;

    public AudioVisualization(WorldManager worldManager, GizmoRenderer renderer, EditorCamera camera)
    {
        _worldManager = worldManager;
        _renderer = renderer;
        _camera = camera;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 viewportSize)
    {
        if (!Enabled) return;

        var world = _worldManager.World;

        var query = new QueryDescription().WithAll<AudioSource, Position>();
        world.Query(query, (Entity entity, ref AudioSource audio, ref Position pos) =>
        {
            float worldRadius = BaseRange * MathF.Max(0.05f, audio.Volume);
            float cx = pos.X;
            float cy = pos.Y;

            var screenCenter = _camera.WorldToScreen(new Vector2(cx, cy), viewportSize);
            var screenEdge = _camera.WorldToScreen(new Vector2(cx + worldRadius, cy), viewportSize);
            float screenRadius = MathF.Abs(screenEdge.X - screenCenter.X);

            // Outer range circle
            _renderer.DrawCircle(spriteBatch, screenCenter, screenRadius, AudioRangeColor, 48, 1);

            // Inner icon indicator (small filled circle at center)
            _renderer.DrawCircle(spriteBatch, screenCenter, 4f, new Color(80, 220, 120, 200), 12, 2);
        });
    }
}
