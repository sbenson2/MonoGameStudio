using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Assets;

/// <summary>
/// Caches MonoGame Effect instances loaded from compiled .xnb files via ContentManager,
/// with a fallback for raw MGFX bytecode (.fx / .mgfx) files.
/// </summary>
public class EffectCache : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Effect> _cache = new();
    private ContentManager? _contentManager;

    public EffectCache(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Sets the ContentManager used for loading .xnb effect files.
    /// Call this after the ContentManager is initialized.
    /// </summary>
    public void SetContentManager(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    public Effect? Get(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (_cache.TryGetValue(fullPath, out var cached))
            return cached;
        return Load(fullPath);
    }

    public Effect? Load(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;

        try
        {
            Effect effect;
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            if (extension == ".xnb" && _contentManager != null)
            {
                // Load via content pipeline — strip extension for ContentManager
                var contentPath = Path.ChangeExtension(fullPath, null);
                effect = _contentManager.Load<Effect>(contentPath);
            }
            else
            {
                // Load raw MGFX bytecode (.fx, .mgfx, or .xnb without ContentManager)
                var bytecode = File.ReadAllBytes(fullPath);
                effect = new Effect(_graphicsDevice, bytecode);
            }

            _cache[fullPath] = effect;
            return effect;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load effect: {fullPath}: {ex.Message}");
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
        if (_cache.TryGetValue(fullPath, out var effect))
        {
            effect.Dispose();
            _cache.Remove(fullPath);
        }
    }

    public void Clear()
    {
        foreach (var effect in _cache.Values)
            effect.Dispose();
        _cache.Clear();
    }

    public void Dispose() => Clear();
}
