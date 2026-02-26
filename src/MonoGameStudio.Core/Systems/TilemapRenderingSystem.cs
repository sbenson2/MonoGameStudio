using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with TilemapRenderer + Position, loads the associated TilemapDocument,
/// and draws visible tile layers using SpriteBatch with simple frustum culling.
/// </summary>
public class TilemapRenderingSystem
{
    private readonly WorldManager _worldManager;
    private readonly TextureCache _textureCache;

    // Cached tilemap documents by path
    private readonly Dictionary<string, TilemapDocument> _tilemapCache = new();

    public TilemapRenderingSystem(WorldManager worldManager, TextureCache textureCache)
    {
        _worldManager = worldManager;
        _textureCache = textureCache;
    }

    /// <summary>
    /// Draws all tilemap entities. Call within an active SpriteBatch Begin/End block.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with.</param>
    /// <param name="viewBounds">The visible area in world space for frustum culling.</param>
    public void Draw(SpriteBatch spriteBatch, Rectangle viewBounds)
    {
        var world = _worldManager.World;

        var query = new QueryDescription().WithAll<TilemapRenderer, Position>();
        world.Query(query, (Entity entity, ref TilemapRenderer tilemap, ref Position pos) =>
        {
            if (string.IsNullOrEmpty(tilemap.TilemapDataPath)) return;

            var doc = GetTilemap(tilemap.TilemapDataPath);
            if (doc == null) return;

            var mapOrigin = new Vector2(pos.X, pos.Y);
            DrawTilemap(spriteBatch, doc, mapOrigin, viewBounds);
        });
    }

    /// <summary>
    /// Draws all tilemap entities without frustum culling.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, new Rectangle(int.MinValue / 2, int.MinValue / 2, int.MaxValue, int.MaxValue));
    }

    private void DrawTilemap(SpriteBatch spriteBatch, TilemapDocument doc, Vector2 mapOrigin, Rectangle viewBounds)
    {
        int tileW = doc.TileWidth;
        int tileH = doc.TileHeight;

        // Compute visible tile range (frustum culling)
        int startCol = Math.Max(0, (int)Math.Floor((viewBounds.Left - mapOrigin.X) / tileW));
        int startRow = Math.Max(0, (int)Math.Floor((viewBounds.Top - mapOrigin.Y) / tileH));
        int endCol = Math.Min(doc.MapWidth - 1, (int)Math.Ceiling((viewBounds.Right - mapOrigin.X) / tileW));
        int endRow = Math.Min(doc.MapHeight - 1, (int)Math.Ceiling((viewBounds.Bottom - mapOrigin.Y) / tileH));

        // Draw each layer back-to-front
        for (int layerIdx = 0; layerIdx < doc.Layers.Count; layerIdx++)
        {
            var layer = doc.Layers[layerIdx];
            if (!layer.Visible) continue;

            for (int y = startRow; y <= endRow; y++)
            {
                for (int x = startCol; x <= endCol; x++)
                {
                    int tileId = doc.GetTile(layerIdx, x, y);
                    if (tileId < 0) continue;

                    var (tileset, localId) = doc.ResolveTileset(tileId);
                    if (tileset == null) continue;

                    var texture = _textureCache.Get(tileset.TexturePath);
                    if (texture == null) continue;

                    var sourceRect = TilemapDocument.GetTileSourceRect(tileset, localId);
                    var destPos = new Vector2(
                        mapOrigin.X + x * tileW,
                        mapOrigin.Y + y * tileH);

                    spriteBatch.Draw(
                        texture,
                        destPos,
                        sourceRect,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        0f);
                }
            }
        }
    }

    public void ClearCache()
    {
        _tilemapCache.Clear();
    }

    /// <summary>
    /// Invalidates a cached tilemap document so it will be reloaded on next draw.
    /// </summary>
    public void InvalidateCache(string path)
    {
        var fullPath = Path.GetFullPath(path);
        _tilemapCache.Remove(fullPath);
    }

    private TilemapDocument? GetTilemap(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (_tilemapCache.TryGetValue(fullPath, out var cached))
            return cached;

        var doc = TilemapSerializer.Load(fullPath);
        if (doc != null)
            _tilemapCache[fullPath] = doc;
        return doc;
    }
}
