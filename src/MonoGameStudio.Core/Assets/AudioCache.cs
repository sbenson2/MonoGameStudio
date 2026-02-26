using Microsoft.Xna.Framework.Audio;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Assets;

/// <summary>
/// Caches SoundEffect instances loaded from raw audio files (bypasses Content Pipeline).
/// </summary>
public class AudioCache : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _cache = new();

    public SoundEffect? Get(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (_cache.TryGetValue(fullPath, out var cached))
            return cached;
        return Load(fullPath);
    }

    public SoundEffect? Load(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;
        try
        {
            using var stream = File.OpenRead(fullPath);
            var sfx = SoundEffect.FromStream(stream);
            _cache[fullPath] = sfx;
            return sfx;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load audio: {fullPath}: {ex.Message}");
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
        if (_cache.TryGetValue(fullPath, out var sfx))
        {
            sfx.Dispose();
            _cache.Remove(fullPath);
        }
    }

    public void Clear()
    {
        foreach (var sfx in _cache.Values)
            sfx.Dispose();
        _cache.Clear();
    }

    public void Dispose() => Clear();
}
