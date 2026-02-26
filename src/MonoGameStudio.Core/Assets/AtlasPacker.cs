using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Assets;

public class AtlasPacker
{
    private readonly GraphicsDevice _graphicsDevice;

    public AtlasPacker(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Packs multiple textures into a single atlas using shelf-based bin packing.
    /// Returns the packed atlas texture and the entries describing each sub-region.
    /// </summary>
    public (Texture2D atlas, List<AtlasEntry> entries)? Pack(
        List<(string name, Texture2D texture)> sources, int maxSize = 4096, int padding = 1)
    {
        if (sources.Count == 0) return null;

        // Sort by height descending for shelf packing
        var sorted = sources.OrderByDescending(s => s.texture.Height).ToList();

        int atlasWidth = 256;
        int atlasHeight = 256;

        // Try increasing atlas sizes until everything fits
        List<AtlasEntry>? entries = null;
        while (atlasWidth <= maxSize && atlasHeight <= maxSize)
        {
            entries = TryPack(sorted, atlasWidth, atlasHeight, padding);
            if (entries != null) break;

            if (atlasWidth <= atlasHeight)
                atlasWidth *= 2;
            else
                atlasHeight *= 2;
        }

        if (entries == null)
        {
            Log.Error("Atlas packing failed: textures exceed maximum atlas size");
            return null;
        }

        // Blit pixels
        var atlas = new Texture2D(_graphicsDevice, atlasWidth, atlasHeight);
        var atlasData = new Color[atlasWidth * atlasHeight];

        foreach (var entry in entries)
        {
            var source = sorted.First(s => s.name == entry.Name);
            var srcData = new Color[source.texture.Width * source.texture.Height];
            source.texture.GetData(srcData);

            for (int y = 0; y < source.texture.Height; y++)
            {
                for (int x = 0; x < source.texture.Width; x++)
                {
                    int destIdx = (entry.Y + y) * atlasWidth + (entry.X + x);
                    int srcIdx = y * source.texture.Width + x;
                    atlasData[destIdx] = srcData[srcIdx];
                }
            }
        }

        atlas.SetData(atlasData);
        return (atlas, entries);
    }

    public void SaveAtlas(Texture2D atlas, string path)
    {
        using var stream = File.Create(path);
        atlas.SaveAsPng(stream, atlas.Width, atlas.Height);
    }

    private static List<AtlasEntry>? TryPack(
        List<(string name, Texture2D texture)> sorted, int width, int height, int padding)
    {
        var entries = new List<AtlasEntry>();
        var shelves = new List<Shelf>();
        int currentY = 0;

        foreach (var (name, texture) in sorted)
        {
            int w = texture.Width + padding;
            int h = texture.Height + padding;
            bool placed = false;

            // Try to fit in existing shelf
            foreach (var shelf in shelves)
            {
                if (shelf.RemainingWidth >= w && shelf.Height >= texture.Height)
                {
                    entries.Add(new AtlasEntry
                    {
                        Name = name,
                        X = shelf.CurrentX,
                        Y = shelf.Y,
                        Width = texture.Width,
                        Height = texture.Height
                    });
                    shelf.CurrentX += w;
                    shelf.RemainingWidth -= w;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // New shelf
                if (currentY + h > height) return null; // doesn't fit
                if (w > width) return null;

                var shelf = new Shelf { Y = currentY, Height = texture.Height + padding, CurrentX = w, RemainingWidth = width - w };
                shelves.Add(shelf);
                entries.Add(new AtlasEntry
                {
                    Name = name,
                    X = 0,
                    Y = currentY,
                    Width = texture.Width,
                    Height = texture.Height
                });
                currentY += shelf.Height;
            }
        }

        return entries;
    }

    private class Shelf
    {
        public int Y;
        public int Height;
        public int CurrentX;
        public int RemainingWidth;
    }
}

public class AtlasEntry
{
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
