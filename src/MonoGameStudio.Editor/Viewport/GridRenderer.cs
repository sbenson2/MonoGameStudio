using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameStudio.Editor.Viewport;

public class GridRenderer
{
    private Texture2D _pixel = null!;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, EditorCamera camera, Vector2 viewportSize)
    {
        float zoom = camera.Zoom;
        var viewMatrix = camera.GetViewMatrix(viewportSize);

        // Determine grid spacing based on zoom
        float majorSpacing = 100f;
        float minorSpacing = 20f;

        // Calculate visible area in world space
        var topLeft = camera.ScreenToWorld(Vector2.Zero, viewportSize);
        var bottomRight = camera.ScreenToWorld(viewportSize, viewportSize);

        float minorAlpha = MathHelper.Clamp(zoom * 0.3f, 0f, 0.15f);
        float majorAlpha = MathHelper.Clamp(zoom * 0.5f, 0.1f, 0.3f);

        // Minor grid lines
        if (minorAlpha > 0.01f)
        {
            var minorColor = new Color(1f, 1f, 1f, minorAlpha);
            DrawGridLines(spriteBatch, viewMatrix, topLeft, bottomRight, minorSpacing, minorColor, viewportSize);
        }

        // Major grid lines
        var majorColor = new Color(1f, 1f, 1f, majorAlpha);
        DrawGridLines(spriteBatch, viewMatrix, topLeft, bottomRight, majorSpacing, majorColor, viewportSize);

        // Origin axes
        DrawOriginAxes(spriteBatch, viewMatrix, topLeft, bottomRight, viewportSize);
    }

    private void DrawGridLines(SpriteBatch spriteBatch, Matrix viewMatrix, Vector2 topLeft, Vector2 bottomRight,
        float spacing, Color color, Vector2 viewportSize)
    {
        // Vertical lines
        float startX = MathF.Floor(topLeft.X / spacing) * spacing;
        for (float x = startX; x <= bottomRight.X; x += spacing)
        {
            var screenStart = Vector2.Transform(new Vector2(x, topLeft.Y), viewMatrix);
            var screenEnd = Vector2.Transform(new Vector2(x, bottomRight.Y), viewMatrix);
            DrawLine(spriteBatch, screenStart, screenEnd, color, 1);
        }

        // Horizontal lines
        float startY = MathF.Floor(topLeft.Y / spacing) * spacing;
        for (float y = startY; y <= bottomRight.Y; y += spacing)
        {
            var screenStart = Vector2.Transform(new Vector2(topLeft.X, y), viewMatrix);
            var screenEnd = Vector2.Transform(new Vector2(bottomRight.X, y), viewMatrix);
            DrawLine(spriteBatch, screenStart, screenEnd, color, 1);
        }
    }

    private void DrawOriginAxes(SpriteBatch spriteBatch, Matrix viewMatrix, Vector2 topLeft, Vector2 bottomRight,
        Vector2 viewportSize)
    {
        // X axis (red)
        var xStart = Vector2.Transform(new Vector2(topLeft.X, 0), viewMatrix);
        var xEnd = Vector2.Transform(new Vector2(bottomRight.X, 0), viewMatrix);
        DrawLine(spriteBatch, xStart, xEnd, new Color(200, 60, 60, 180), 2);

        // Y axis (green)
        var yStart = Vector2.Transform(new Vector2(0, topLeft.Y), viewMatrix);
        var yEnd = Vector2.Transform(new Vector2(0, bottomRight.Y), viewMatrix);
        DrawLine(spriteBatch, yStart, yEnd, new Color(60, 200, 60, 180), 2);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
    {
        var delta = end - start;
        float length = delta.Length();
        if (length < 0.5f) return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(_pixel, start, null, color,
            angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
