using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameStudio.Editor.Gizmos;

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

    public void DrawHandle(SpriteBatch spriteBatch, Vector2 position, float size, Color fillColor, Color borderColor)
    {
        // Filled square
        DrawRect(spriteBatch, position, size, fillColor);
        // Border
        var half = size / 2f;
        DrawRectOutline(spriteBatch,
            new Vector2(position.X - half, position.Y - half),
            new Vector2(position.X + half, position.Y + half),
            borderColor, 1);
    }

    public void DrawBoundingBox(SpriteBatch spriteBatch, Vector2 screenMin, Vector2 screenMax,
        BBoxHandle hoveredHandle, Color outlineColor, float handleSize = 7f)
    {
        // Outline
        DrawRectOutline(spriteBatch, screenMin, screenMax, outlineColor, 2);

        var center = (screenMin + screenMax) * 0.5f;
        var handlePositions = GetHandlePositions(screenMin, screenMax);

        for (int i = 0; i < 8; i++)
        {
            var handle = (BBoxHandle)(i + 1); // TopLeft=1 .. Right=8
            bool isHovered = hoveredHandle == handle;
            var fillColor = isHovered ? Color.Yellow : Color.White;
            var borderColor = isHovered ? new Color(200, 180, 0) : new Color(60, 60, 60);
            DrawHandle(spriteBatch, handlePositions[i], handleSize, fillColor, borderColor);
        }
    }

    public static Vector2[] GetHandlePositions(Vector2 min, Vector2 max)
    {
        float midX = (min.X + max.X) * 0.5f;
        float midY = (min.Y + max.Y) * 0.5f;

        return new[]
        {
            new Vector2(min.X, min.Y), // TopLeft
            new Vector2(max.X, min.Y), // TopRight
            new Vector2(min.X, max.Y), // BottomLeft
            new Vector2(max.X, max.Y), // BottomRight
            new Vector2(midX, min.Y),  // Top
            new Vector2(midX, max.Y),  // Bottom
            new Vector2(min.X, midY),  // Left
            new Vector2(max.X, midY),  // Right
        };
    }
}

public enum BBoxHandle
{
    None = 0,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Top,
    Bottom,
    Left,
    Right,
    Body // dragging inside the box
}
