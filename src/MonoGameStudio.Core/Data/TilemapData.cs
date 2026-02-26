using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

/// <summary>
/// Complete tilemap document format. Contains map dimensions, layers, and tileset references.
/// Tiles are stored as flat int arrays where -1 means empty.
/// </summary>
public class TilemapDocument
{
    [JsonPropertyName("tileWidth")]
    public int TileWidth { get; set; } = 16;

    [JsonPropertyName("tileHeight")]
    public int TileHeight { get; set; } = 16;

    [JsonPropertyName("mapWidth")]
    public int MapWidth { get; set; } = 32;

    [JsonPropertyName("mapHeight")]
    public int MapHeight { get; set; } = 32;

    [JsonPropertyName("layers")]
    public List<TilemapLayer> Layers { get; set; } = new();

    [JsonPropertyName("tilesets")]
    public List<TilesetReference> Tilesets { get; set; } = new();

    /// <summary>
    /// Gets the tile at (x, y) in the given layer, or -1 if out of bounds.
    /// </summary>
    public int GetTile(int layerIndex, int x, int y)
    {
        if (layerIndex < 0 || layerIndex >= Layers.Count) return -1;
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return -1;
        return Layers[layerIndex].Tiles[y * MapWidth + x];
    }

    /// <summary>
    /// Sets the tile at (x, y) in the given layer.
    /// </summary>
    public void SetTile(int layerIndex, int x, int y, int tileId)
    {
        if (layerIndex < 0 || layerIndex >= Layers.Count) return;
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return;
        Layers[layerIndex].Tiles[y * MapWidth + x] = tileId;
    }

    /// <summary>
    /// Resolves which tileset owns a given global tile ID.
    /// Returns the tileset reference and the local tile index within that tileset.
    /// </summary>
    public (TilesetReference? tileset, int localId) ResolveTileset(int globalTileId)
    {
        if (globalTileId < 0) return (null, -1);

        TilesetReference? best = null;
        foreach (var ts in Tilesets)
        {
            if (ts.FirstGid <= globalTileId && (best == null || ts.FirstGid > best.FirstGid))
                best = ts;
        }

        if (best == null) return (null, -1);
        return (best, globalTileId - best.FirstGid);
    }

    /// <summary>
    /// Computes the source rectangle in the tileset texture for a local tile index.
    /// </summary>
    public static Microsoft.Xna.Framework.Rectangle GetTileSourceRect(TilesetReference tileset, int localId)
    {
        int col = localId % tileset.Columns;
        int row = localId / tileset.Columns;
        return new Microsoft.Xna.Framework.Rectangle(
            col * tileset.TileWidth,
            row * tileset.TileHeight,
            tileset.TileWidth,
            tileset.TileHeight);
    }
}

/// <summary>
/// A single layer in a tilemap. Tiles are stored as a flat int array (row-major),
/// where -1 indicates an empty tile.
/// </summary>
public class TilemapLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Layer";

    [JsonPropertyName("visible")]
    [JsonInclude]
    public bool Visible = true;

    [JsonPropertyName("tiles")]
    public int[] Tiles { get; set; } = [];

    /// <summary>
    /// Initializes the tile array for the given map dimensions, filled with -1 (empty).
    /// </summary>
    public void Initialize(int mapWidth, int mapHeight)
    {
        Tiles = new int[mapWidth * mapHeight];
        Array.Fill(Tiles, -1);
    }
}

/// <summary>
/// References an external tileset image and describes how to index into it.
/// </summary>
public class TilesetReference
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("texturePath")]
    public string TexturePath { get; set; } = "";

    [JsonPropertyName("firstGid")]
    public int FirstGid { get; set; }

    [JsonPropertyName("tileWidth")]
    public int TileWidth { get; set; } = 16;

    [JsonPropertyName("tileHeight")]
    public int TileHeight { get; set; } = 16;

    [JsonPropertyName("columns")]
    public int Columns { get; set; } = 1;

    /// <summary>
    /// Total number of tiles in this tileset (rows * columns), computed from texture dimensions.
    /// </summary>
    [JsonPropertyName("tileCount")]
    public int TileCount { get; set; }
}

public static class TilemapSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static TilemapDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TilemapDocument>(json, _options);
    }

    public static void Save(string path, TilemapDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, _options);
        File.WriteAllText(path, json);
    }
}
