using System.Xml.Linq;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Data;

/// <summary>
/// Imports Tiled .tmx files and converts them to TilemapDocument format.
/// Supports orthogonal maps with CSV-encoded tile data.
/// </summary>
public static class TiledImporter
{
    /// <summary>
    /// Imports a .tmx file and returns a TilemapDocument, or null on failure.
    /// Tileset image paths are resolved relative to the .tmx file location.
    /// </summary>
    public static TilemapDocument? Import(string tmxPath)
    {
        if (!File.Exists(tmxPath))
        {
            Log.Warn($"Tiled import: file not found: {tmxPath}");
            return null;
        }

        try
        {
            var xml = XDocument.Load(tmxPath);
            var mapElement = xml.Element("map");
            if (mapElement == null)
            {
                Log.Warn("Tiled import: no <map> element found");
                return null;
            }

            var doc = new TilemapDocument
            {
                MapWidth = (int)mapElement.Attribute("width")!,
                MapHeight = (int)mapElement.Attribute("height")!,
                TileWidth = (int)mapElement.Attribute("tilewidth")!,
                TileHeight = (int)mapElement.Attribute("tileheight")!
            };

            string tmxDir = Path.GetDirectoryName(Path.GetFullPath(tmxPath)) ?? "";

            // Parse tilesets
            foreach (var tsElement in mapElement.Elements("tileset"))
            {
                var tileset = ParseTileset(tsElement, tmxDir);
                if (tileset != null)
                    doc.Tilesets.Add(tileset);
            }

            // Parse layers
            foreach (var layerElement in mapElement.Elements("layer"))
            {
                var layer = ParseLayer(layerElement, doc.MapWidth, doc.MapHeight);
                if (layer != null)
                    doc.Layers.Add(layer);
            }

            Log.Info($"Tiled import: loaded {tmxPath} ({doc.MapWidth}x{doc.MapHeight}, " +
                     $"{doc.Layers.Count} layers, {doc.Tilesets.Count} tilesets)");
            return doc;
        }
        catch (Exception ex)
        {
            Log.Warn($"Tiled import failed: {ex.Message}");
            return null;
        }
    }

    private static TilesetReference? ParseTileset(XElement element, string tmxDir)
    {
        // Handle external tilesets (.tsx) — load the external file
        var source = (string?)element.Attribute("source");
        if (source != null)
        {
            return ParseExternalTileset(element, source, tmxDir);
        }

        // Inline tileset
        var firstGid = (int)element.Attribute("firstgid")!;
        var name = (string?)element.Attribute("name") ?? "Tileset";
        var tileWidth = (int?)element.Attribute("tilewidth") ?? 16;
        var tileHeight = (int?)element.Attribute("tileheight") ?? 16;
        var tileCount = (int?)element.Attribute("tilecount") ?? 0;
        var columns = (int?)element.Attribute("columns") ?? 1;

        var imageElement = element.Element("image");
        var imagePath = (string?)imageElement?.Attribute("source") ?? "";

        // Resolve image path relative to .tmx directory
        if (!string.IsNullOrEmpty(imagePath) && !Path.IsPathRooted(imagePath))
            imagePath = Path.GetFullPath(Path.Combine(tmxDir, imagePath));

        return new TilesetReference
        {
            Name = name,
            TexturePath = imagePath,
            FirstGid = firstGid,
            TileWidth = tileWidth,
            TileHeight = tileHeight,
            Columns = columns,
            TileCount = tileCount
        };
    }

    private static TilesetReference? ParseExternalTileset(XElement element, string source, string tmxDir)
    {
        var firstGid = (int)element.Attribute("firstgid")!;
        var tsxPath = Path.GetFullPath(Path.Combine(tmxDir, source));

        if (!File.Exists(tsxPath))
        {
            Log.Warn($"Tiled import: external tileset not found: {tsxPath}");
            return null;
        }

        try
        {
            var tsxDir = Path.GetDirectoryName(tsxPath) ?? tmxDir;
            var tsx = XDocument.Load(tsxPath);
            var tsxRoot = tsx.Element("tileset");
            if (tsxRoot == null) return null;

            var name = (string?)tsxRoot.Attribute("name") ?? "Tileset";
            var tileWidth = (int?)tsxRoot.Attribute("tilewidth") ?? 16;
            var tileHeight = (int?)tsxRoot.Attribute("tileheight") ?? 16;
            var tileCount = (int?)tsxRoot.Attribute("tilecount") ?? 0;
            var columns = (int?)tsxRoot.Attribute("columns") ?? 1;

            var imageElement = tsxRoot.Element("image");
            var imagePath = (string?)imageElement?.Attribute("source") ?? "";

            if (!string.IsNullOrEmpty(imagePath) && !Path.IsPathRooted(imagePath))
                imagePath = Path.GetFullPath(Path.Combine(tsxDir, imagePath));

            return new TilesetReference
            {
                Name = name,
                TexturePath = imagePath,
                FirstGid = firstGid,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                Columns = columns,
                TileCount = tileCount
            };
        }
        catch (Exception ex)
        {
            Log.Warn($"Tiled import: failed to load external tileset {tsxPath}: {ex.Message}");
            return null;
        }
    }

    private static TilemapLayer? ParseLayer(XElement element, int mapWidth, int mapHeight)
    {
        var name = (string?)element.Attribute("name") ?? "Layer";
        var visible = ((int?)element.Attribute("visible") ?? 1) != 0;

        var dataElement = element.Element("data");
        if (dataElement == null) return null;

        var encoding = (string?)dataElement.Attribute("encoding");

        int[]? tiles = encoding switch
        {
            "csv" => ParseCsvData(dataElement.Value, mapWidth, mapHeight),
            null => ParseXmlData(dataElement, mapWidth, mapHeight), // XML tile elements
            _ => null
        };

        if (tiles == null)
        {
            Log.Warn($"Tiled import: unsupported encoding '{encoding}' for layer '{name}'");
            return null;
        }

        return new TilemapLayer
        {
            Name = name,
            Visible = visible,
            Tiles = tiles
        };
    }

    private static int[] ParseCsvData(string csvText, int mapWidth, int mapHeight)
    {
        var tiles = new int[mapWidth * mapHeight];
        var values = csvText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tiles.Length && i < values.Length; i++)
        {
            // Tiled uses 0 for empty tiles; we use -1
            if (int.TryParse(values[i], out int val))
                tiles[i] = val == 0 ? -1 : val;
            else
                tiles[i] = -1;
        }

        return tiles;
    }

    private static int[] ParseXmlData(XElement dataElement, int mapWidth, int mapHeight)
    {
        var tiles = new int[mapWidth * mapHeight];
        int index = 0;

        foreach (var tileElement in dataElement.Elements("tile"))
        {
            if (index >= tiles.Length) break;

            var gid = (int?)tileElement.Attribute("gid") ?? 0;
            tiles[index++] = gid == 0 ? -1 : gid;
        }

        return tiles;
    }
}
