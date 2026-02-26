namespace MonoGameStudio.Core.Data;

/// <summary>
/// Auto-tiling using a 4-neighbor bitmask (up, right, down, left).
/// Each neighbor contributes a bit: Up=1, Right=2, Down=4, Left=8.
/// This gives 16 possible tile variants (0..15).
/// </summary>
public class AutoTileRules
{
    /// <summary>
    /// Maps each 4-bit bitmask (0..15) to a tile index in the tileset.
    /// Default mapping assumes a standard 4-neighbor auto-tile layout.
    /// Set individual entries to customize the mapping.
    /// </summary>
    public int[] BitmaskToTileIndex { get; set; } = new int[16];

    /// <summary>
    /// The base tile ID (first GID) for this auto-tile group.
    /// The resolved tile ID = BaseTileId + BitmaskToTileIndex[bitmask].
    /// </summary>
    public int BaseTileId { get; set; }

    public AutoTileRules()
    {
        // Default identity mapping: bitmask value = tile index
        for (int i = 0; i < 16; i++)
            BitmaskToTileIndex[i] = i;
    }

    /// <summary>
    /// Constructs an AutoTileRules with a specific base tile ID and optional custom mapping.
    /// </summary>
    public AutoTileRules(int baseTileId, int[]? customMapping = null)
    {
        BaseTileId = baseTileId;
        if (customMapping != null && customMapping.Length == 16)
        {
            Array.Copy(customMapping, BitmaskToTileIndex, 16);
        }
        else
        {
            for (int i = 0; i < 16; i++)
                BitmaskToTileIndex[i] = i;
        }
    }

    /// <summary>
    /// Resolves the global tile ID based on which neighbors are present.
    /// </summary>
    /// <param name="up">True if the tile above is the same type.</param>
    /// <param name="right">True if the tile to the right is the same type.</param>
    /// <param name="down">True if the tile below is the same type.</param>
    /// <param name="left">True if the tile to the left is the same type.</param>
    /// <returns>The global tile ID to use for rendering.</returns>
    public int ResolveTile(bool up, bool right, bool down, bool left)
    {
        int bitmask = ComputeBitmask(up, right, down, left);
        return BaseTileId + BitmaskToTileIndex[bitmask];
    }

    /// <summary>
    /// Computes the 4-bit bitmask from neighbor flags.
    /// Bit 0 (1) = Up, Bit 1 (2) = Right, Bit 2 (4) = Down, Bit 3 (8) = Left.
    /// </summary>
    public static int ComputeBitmask(bool up, bool right, bool down, bool left)
    {
        int mask = 0;
        if (up) mask |= Up;
        if (right) mask |= Right;
        if (down) mask |= Down;
        if (left) mask |= Left;
        return mask;
    }

    /// <summary>
    /// Resolves auto-tile for an entire tilemap layer. For each non-empty tile matching
    /// the specified tileGroup, neighbors are checked and the tile is replaced with the
    /// correct auto-tile variant.
    /// </summary>
    /// <param name="doc">The tilemap document.</param>
    /// <param name="layerIndex">Which layer to process.</param>
    /// <param name="tileGroup">The set of tile IDs that count as "same type" for neighbor checks.</param>
    public void ApplyToLayer(TilemapDocument doc, int layerIndex, HashSet<int> tileGroup)
    {
        if (layerIndex < 0 || layerIndex >= doc.Layers.Count) return;

        int w = doc.MapWidth;
        int h = doc.MapHeight;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int current = doc.GetTile(layerIndex, x, y);
                if (current < 0 || !tileGroup.Contains(current)) continue;

                bool up = y > 0 && tileGroup.Contains(doc.GetTile(layerIndex, x, y - 1));
                bool right = x < w - 1 && tileGroup.Contains(doc.GetTile(layerIndex, x + 1, y));
                bool down = y < h - 1 && tileGroup.Contains(doc.GetTile(layerIndex, x, y + 1));
                bool left = x > 0 && tileGroup.Contains(doc.GetTile(layerIndex, x - 1, y));

                int resolved = ResolveTile(up, right, down, left);
                doc.SetTile(layerIndex, x, y, resolved);
            }
        }
    }

    // Bitmask constants
    public const int Up = 1;
    public const int Right = 2;
    public const int Down = 4;
    public const int Left = 8;

    // Named bitmask combinations for convenience
    public const int None = 0;                              // 0  — isolated
    public const int UpOnly = Up;                           // 1
    public const int RightOnly = Right;                     // 2
    public const int UpRight = Up | Right;                  // 3
    public const int DownOnly = Down;                       // 4
    public const int UpDown = Up | Down;                    // 5  — vertical corridor
    public const int RightDown = Right | Down;              // 6
    public const int UpRightDown = Up | Right | Down;       // 7
    public const int LeftOnly = Left;                       // 8
    public const int UpLeft = Up | Left;                    // 9
    public const int LeftRight = Left | Right;              // 10 — horizontal corridor
    public const int UpLeftRight = Up | Left | Right;       // 11
    public const int DownLeft = Down | Left;                // 12
    public const int UpDownLeft = Up | Down | Left;         // 13
    public const int DownLeftRight = Down | Left | Right;   // 14
    public const int All = Up | Right | Down | Left;        // 15 — fully surrounded
}
