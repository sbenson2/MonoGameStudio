using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Assets;

/// <summary>
/// Caches Texture2D instances loaded from raw image files (bypasses Content Pipeline).
/// </summary>
public class TextureCache : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Texture2D> _cache = new();

    public TextureCache(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public Texture2D? Get(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (_cache.TryGetValue(fullPath, out var cached))
            return cached;

        return Load(fullPath);
    }

    public Texture2D? Load(string fullPath)
    {
        if (!File.Exists(fullPath))
            return null;

        try
        {
            using var stream = File.OpenRead(fullPath);
            var texture = Texture2D.FromStream(_graphicsDevice, stream);
            _cache[fullPath] = texture;
            return texture;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load texture: {fullPath}: {ex.Message}");
            return null;
        }
    }

    public void Reload(string fullPath)
    {
        fullPath = Path.GetFullPath(fullPath);

        if (_cache.TryGetValue(fullPath, out var old))
        {
            old.Dispose();
            _cache.Remove(fullPath);
        }

        Load(fullPath);
    }

    public void Evict(string fullPath)
    {
        fullPath = Path.GetFullPath(fullPath);
        if (_cache.TryGetValue(fullPath, out var tex))
        {
            tex.Dispose();
            _cache.Remove(fullPath);
        }
    }

    public void Clear()
    {
        foreach (var tex in _cache.Values)
            tex.Dispose();
        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
