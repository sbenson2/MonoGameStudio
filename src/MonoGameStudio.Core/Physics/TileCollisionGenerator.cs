using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Core.Physics;

/// <summary>
/// Generates collision rectangles from tilemap layers using greedy meshing.
/// Merges adjacent solid tiles into larger rectangles to reduce collision shape count.
/// </summary>
public static class TileCollisionGenerator
{
    /// <summary>
    /// Generates a list of merged collision rectangles from a tilemap layer.
    /// Any tile with an ID >= 0 is treated as solid.
    /// </summary>
    /// <param name="layer">The tilemap layer to scan.</param>
    /// <param name="mapWidth">Map width in tiles.</param>
    /// <param name="mapHeight">Map height in tiles.</param>
    /// <param name="tileWidth">Width of each tile in pixels.</param>
    /// <param name="tileHeight">Height of each tile in pixels.</param>
    /// <returns>A list of world-space collision rectangles.</returns>
    public static List<Rectangle> Generate(TilemapLayer layer, int mapWidth, int mapHeight,
        int tileWidth, int tileHeight)
    {
        return Generate(layer, mapWidth, mapHeight, tileWidth, tileHeight, tileId => tileId >= 0);
    }

    /// <summary>
    /// Generates a list of merged collision rectangles from a tilemap layer,
    /// using a custom predicate to determine which tiles are solid.
    /// </summary>
    /// <param name="layer">The tilemap layer to scan.</param>
    /// <param name="mapWidth">Map width in tiles.</param>
    /// <param name="mapHeight">Map height in tiles.</param>
    /// <param name="tileWidth">Width of each tile in pixels.</param>
    /// <param name="tileHeight">Height of each tile in pixels.</param>
    /// <param name="isSolid">Predicate that returns true for tile IDs that should generate collision.</param>
    /// <returns>A list of world-space collision rectangles.</returns>
    public static List<Rectangle> Generate(TilemapLayer layer, int mapWidth, int mapHeight,
        int tileWidth, int tileHeight, Func<int, bool> isSolid)
    {
        var results = new List<Rectangle>();

        // Track which tiles have already been merged
        var visited = new bool[mapWidth * mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int index = y * mapWidth + x;
                if (visited[index]) continue;

                int tileId = layer.Tiles[index];
                if (!isSolid(tileId)) continue;

                // Greedy expand: first expand right as far as possible
                int rectWidth = 1;
                while (x + rectWidth < mapWidth)
                {
                    int nextIndex = y * mapWidth + (x + rectWidth);
                    if (visited[nextIndex] || !isSolid(layer.Tiles[nextIndex]))
                        break;
                    rectWidth++;
                }

                // Then expand downward, checking that the entire row span is solid and unvisited
                int rectHeight = 1;
                while (y + rectHeight < mapHeight)
                {
                    bool rowValid = true;
                    for (int dx = 0; dx < rectWidth; dx++)
                    {
                        int checkIndex = (y + rectHeight) * mapWidth + (x + dx);
                        if (visited[checkIndex] || !isSolid(layer.Tiles[checkIndex]))
                        {
                            rowValid = false;
                            break;
                        }
                    }
                    if (!rowValid) break;
                    rectHeight++;
                }

                // Mark all tiles in the merged rectangle as visited
                for (int dy = 0; dy < rectHeight; dy++)
                {
                    for (int dx = 0; dx < rectWidth; dx++)
                    {
                        visited[(y + dy) * mapWidth + (x + dx)] = true;
                    }
                }

                // Create the collision rectangle in pixel coordinates
                results.Add(new Rectangle(
                    x * tileWidth,
                    y * tileHeight,
                    rectWidth * tileWidth,
                    rectHeight * tileHeight));
            }
        }

        return results;
    }

    /// <summary>
    /// Generates collision rectangles from a TilemapDocument for a specific layer.
    /// </summary>
    /// <param name="doc">The tilemap document.</param>
    /// <param name="layerIndex">The layer index to generate collisions from.</param>
    /// <returns>A list of world-space collision rectangles, or empty if the layer is invalid.</returns>
    public static List<Rectangle> Generate(TilemapDocument doc, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= doc.Layers.Count)
            return new List<Rectangle>();

        return Generate(doc.Layers[layerIndex], doc.MapWidth, doc.MapHeight,
            doc.TileWidth, doc.TileHeight);
    }

    /// <summary>
    /// Generates collision rectangles from a TilemapDocument for a specific layer,
    /// using a custom predicate and applying a world-space offset.
    /// </summary>
    public static List<Rectangle> Generate(TilemapDocument doc, int layerIndex,
        Func<int, bool> isSolid, Point offset)
    {
        if (layerIndex < 0 || layerIndex >= doc.Layers.Count)
            return new List<Rectangle>();

        var rects = Generate(doc.Layers[layerIndex], doc.MapWidth, doc.MapHeight,
            doc.TileWidth, doc.TileHeight, isSolid);

        // Apply offset
        if (offset != Point.Zero)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                rects[i] = new Rectangle(r.X + offset.X, r.Y + offset.Y, r.Width, r.Height);
            }
        }

        return rects;
    }
}
