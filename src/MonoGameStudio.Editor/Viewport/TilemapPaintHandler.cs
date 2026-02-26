using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Editor.Commands;
using MonoGameStudio.Editor.Panels;

namespace MonoGameStudio.Editor.Viewport;

/// <summary>
/// Handles viewport interaction for tilemap painting. Converts mouse position to
/// world coordinates, then to tile coordinates, and applies the current tool.
/// Shows a ghost preview at the cursor position.
/// </summary>
public class TilemapPaintHandler
{
    private readonly TilemapEditorPanel _editorPanel;
    private readonly CommandHistory _commandHistory;

    private bool _isMouseDown;
    private int _lastTileX = -1;
    private int _lastTileY = -1;

    // Ghost preview state
    private int _ghostTileX = -1;
    private int _ghostTileY = -1;
    private bool _showGhost;

    public TilemapPaintHandler(TilemapEditorPanel editorPanel, CommandHistory commandHistory)
    {
        _editorPanel = editorPanel;
        _commandHistory = commandHistory;
    }

    /// <summary>
    /// Updates the paint handler with the current mouse state. Call each frame when the
    /// tilemap editor is active.
    /// </summary>
    /// <param name="mouse">Current mouse state.</param>
    /// <param name="camera">The editor camera for screen-to-world conversion.</param>
    /// <param name="viewportOrigin">Top-left of the viewport in screen space.</param>
    /// <param name="viewportSize">Size of the viewport in screen space.</param>
    /// <param name="mapOrigin">The tilemap entity's world position.</param>
    /// <param name="isViewportHovered">Whether the mouse is over the viewport.</param>
    public void Update(MouseState mouse, EditorCamera camera, Vector2 viewportOrigin,
        Vector2 viewportSize, Vector2 mapOrigin, bool isViewportHovered)
    {
        var doc = _editorPanel.Document;
        if (doc == null || !isViewportHovered)
        {
            _showGhost = false;
            if (_isMouseDown)
                FinishStroke();
            return;
        }

        // Convert screen mouse position to world coordinates
        var screenPos = new Vector2(mouse.X, mouse.Y) - viewportOrigin;
        var worldPos = camera.ScreenToWorld(screenPos, viewportSize);

        // Convert world position to tile coordinates
        int tileX = (int)Math.Floor((worldPos.X - mapOrigin.X) / doc.TileWidth);
        int tileY = (int)Math.Floor((worldPos.Y - mapOrigin.Y) / doc.TileHeight);

        // Update ghost preview
        bool inBounds = tileX >= 0 && tileX < doc.MapWidth && tileY >= 0 && tileY < doc.MapHeight;
        _showGhost = inBounds;
        _ghostTileX = tileX;
        _ghostTileY = tileY;

        int layerIndex = _editorPanel.SelectedLayerIndex;
        int selectedTileId = _editorPanel.SelectedTileId;
        var tool = _editorPanel.CurrentTool;

        // Left mouse button handling
        if (mouse.LeftButton == ButtonState.Pressed && inBounds)
        {
            if (!_isMouseDown)
            {
                // Start stroke
                _isMouseDown = true;
                _lastTileX = tileX;
                _lastTileY = tileY;
                tool.OnMouseDown(doc, layerIndex, tileX, tileY, selectedTileId);
            }
            else if (tileX != _lastTileX || tileY != _lastTileY)
            {
                // Continue stroke at new tile
                _lastTileX = tileX;
                _lastTileY = tileY;
                tool.OnMouseDrag(doc, layerIndex, tileX, tileY, selectedTileId);
            }
        }
        else if (_isMouseDown)
        {
            // End stroke
            FinishStroke();
        }
    }

    private void FinishStroke()
    {
        if (!_isMouseDown) return;
        _isMouseDown = false;

        var doc = _editorPanel.Document;
        if (doc == null) return;

        int layerIndex = _editorPanel.SelectedLayerIndex;
        int selectedTileId = _editorPanel.SelectedTileId;
        var tool = _editorPanel.CurrentTool;

        var changes = tool.OnMouseUp(doc, layerIndex, _lastTileX, _lastTileY, selectedTileId);
        if (changes.Length == 0) return;

        // Undo the tool's immediate changes, then re-apply via command for undo/redo support
        foreach (var change in changes)
            doc.SetTile(layerIndex, change.X, change.Y, change.OldTileId);

        ICommand command;
        if (tool is FillTool)
            command = new FillTilesCommand(doc, layerIndex, changes);
        else if (changes.Length == 1)
            command = new PaintTileCommand(doc, layerIndex, changes[0].X, changes[0].Y, changes[0].NewTileId);
        else
            command = new PaintTilesCommand(doc, layerIndex, changes, $"{tool.Name} {changes.Length} tiles");

        _commandHistory.Execute(command);
    }

    /// <summary>
    /// Draws the ghost preview tile at the cursor position.
    /// Call within an active SpriteBatch Begin/End block.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with.</param>
    /// <param name="doc">The tilemap document.</param>
    /// <param name="mapOrigin">The tilemap entity's world position.</param>
    /// <param name="textureCache">Texture cache for loading tileset textures.</param>
    public void DrawGhostPreview(SpriteBatch spriteBatch, TilemapDocument doc,
        Vector2 mapOrigin, Core.Assets.TextureCache textureCache)
    {
        if (!_showGhost || _editorPanel.SelectedTileId < 0) return;
        if (_ghostTileX < 0 || _ghostTileX >= doc.MapWidth) return;
        if (_ghostTileY < 0 || _ghostTileY >= doc.MapHeight) return;

        int tileId = _editorPanel.SelectedTileId;
        var (tileset, localId) = doc.ResolveTileset(tileId);
        if (tileset == null) return;

        var texture = textureCache.Get(tileset.TexturePath);
        if (texture == null) return;

        var sourceRect = TilemapDocument.GetTileSourceRect(tileset, localId);
        var destPos = new Vector2(
            mapOrigin.X + _ghostTileX * doc.TileWidth,
            mapOrigin.Y + _ghostTileY * doc.TileHeight);

        // Draw ghost with transparency
        spriteBatch.Draw(texture, destPos, sourceRect,
            Color.White * 0.5f, 0f, Vector2.Zero, Vector2.One,
            SpriteEffects.None, 0f);

        // Draw rect tool preview
        if (_editorPanel.CurrentTool is RectTool rectTool && rectTool.IsDragging)
        {
            var (minX, minY, maxX, maxY) = rectTool.CurrentRect;
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(doc.MapWidth - 1, maxX);
            maxY = Math.Min(doc.MapHeight - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x == _ghostTileX && y == _ghostTileY) continue; // already drawn

                    var previewPos = new Vector2(
                        mapOrigin.X + x * doc.TileWidth,
                        mapOrigin.Y + y * doc.TileHeight);

                    spriteBatch.Draw(texture, previewPos, sourceRect,
                        Color.White * 0.3f, 0f, Vector2.Zero, Vector2.One,
                        SpriteEffects.None, 0f);
                }
            }
        }
    }

    /// <summary>
    /// Draws a highlight rectangle around the tile under the cursor.
    /// Call this with a SpriteBatch in immediate mode for outline rendering.
    /// </summary>
    public void DrawCursorHighlight(SpriteBatch spriteBatch, TilemapDocument doc,
        Vector2 mapOrigin, Texture2D pixelTexture)
    {
        if (!_showGhost) return;
        if (_ghostTileX < 0 || _ghostTileX >= doc.MapWidth) return;
        if (_ghostTileY < 0 || _ghostTileY >= doc.MapHeight) return;

        var tileRect = new Rectangle(
            (int)(mapOrigin.X + _ghostTileX * doc.TileWidth),
            (int)(mapOrigin.Y + _ghostTileY * doc.TileHeight),
            doc.TileWidth,
            doc.TileHeight);

        var highlightColor = Color.Yellow * 0.6f;
        int thickness = 1;

        // Top
        spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Y, tileRect.Width, thickness), highlightColor);
        // Bottom
        spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Bottom - thickness, tileRect.Width, thickness), highlightColor);
        // Left
        spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Y, thickness, tileRect.Height), highlightColor);
        // Right
        spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.Right - thickness, tileRect.Y, thickness, tileRect.Height), highlightColor);
    }
}
