using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameStudio.Editor.Gizmos;

/// <summary>
/// Primitive drawing utilities for gizmos using a shared 1x1 white texture.
/// </summary>
public class GizmoRenderer
{
    private Texture2D _pixel = null!;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness = 2)
    {
        var delta = end - start;
        float length = delta.Length();
        if (length < 0.5f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(_pixel, start, null, color,
            angle, new Vector2(0, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    public void DrawArrow(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color, int thickness = 2, float headSize = 8f)
    {
        DrawLine(spriteBatch, from, to, color, thickness);

        var dir = Vector2.Normalize(to - from);
        var perp = new Vector2(-dir.Y, dir.X);

        var tip = to;
        var left = tip - dir * headSize + perp * headSize * 0.5f;
        var right = tip - dir * headSize - perp * headSize * 0.5f;

        DrawLine(spriteBatch, tip, left, color, thickness);
        DrawLine(spriteBatch, tip, right, color, thickness);
    }

    public void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments = 32, int thickness = 2)
    {
        float step = MathF.Tau / segments;
        var prev = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;
            var next = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(spriteBatch, prev, next, color, thickness);
            prev = next;
        }
    }

    public void DrawRect(SpriteBatch spriteBatch, Vector2 position, float size, Color color)
    {
        var halfSize = size / 2f;
        spriteBatch.Draw(_pixel, new Rectangle(
            (int)(position.X - halfSize),
            (int)(position.Y - halfSize),
            (int)size, (int)size), color);
    }

    public void DrawRectOutline(SpriteBatch spriteBatch, Vector2 min, Vector2 max, Color color, int thickness = 1)
    {
        DrawLine(spriteBatch, new Vector2(min.X, min.Y), new Vector2(max.X, min.Y), color, thickness);
        DrawLine(spriteBatch, new Vector2(max.X, min.Y), new Vector2(max.X, max.Y), color, thickness);
        DrawLine(spriteBatch, new Vector2(max.X, max.Y), new Vector2(min.X, max.Y), color, thickness);
        DrawLine(spriteBatch, new Vector2(min.X, max.Y), new Vector2(min.X, min.Y), color, thickness);
    }
}
