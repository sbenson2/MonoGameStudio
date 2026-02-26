using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Rendering;

/// <summary>
/// A single post-process effect stage wrapping a MonoGame Effect.
/// </summary>
public class PostProcessEffect
{
    public string Name { get; set; } = "Effect";
    public bool Enabled { get; set; } = true;
    public string EffectPath { get; set; } = "";
    public Dictionary<string, float> FloatParameters { get; set; } = new();
    public Dictionary<string, Vector2> Vector2Parameters { get; set; } = new();
    public Dictionary<string, Vector4> Vector4Parameters { get; set; } = new();

    /// <summary>
    /// The loaded Effect instance. Set by the pipeline during initialization.
    /// </summary>
    public Effect? Effect { get; set; }

    /// <summary>
    /// Applies all stored parameters to the loaded Effect.
    /// </summary>
    public void ApplyParameters()
    {
        if (Effect == null) return;

        foreach (var (name, value) in FloatParameters)
        {
            var param = Effect.Parameters[name];
            if (param != null) param.SetValue(value);
        }

        foreach (var (name, value) in Vector2Parameters)
        {
            var param = Effect.Parameters[name];
            if (param != null) param.SetValue(value);
        }

        foreach (var (name, value) in Vector4Parameters)
        {
            var param = Effect.Parameters[name];
            if (param != null) param.SetValue(value);
        }
    }
}

/// <summary>
/// Ordered pipeline of post-processing effects using ping-pong RenderTarget pattern.
/// Renders: scene RT -> effect1 -> RT_B -> effect2 -> RT_A -> ... -> final RT.
/// </summary>
public class PostProcessorPipeline : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly EffectCache _effectCache;
    private readonly List<PostProcessEffect> _effects = new();

    private RenderTarget2D? _rtA;
    private RenderTarget2D? _rtB;
    private int _lastWidth;
    private int _lastHeight;

    public IReadOnlyList<PostProcessEffect> Effects => _effects;

    public PostProcessorPipeline(GraphicsDevice graphicsDevice, EffectCache effectCache)
    {
        _graphicsDevice = graphicsDevice;
        _effectCache = effectCache;
    }

    public void AddEffect(PostProcessEffect effect)
    {
        LoadEffect(effect);
        _effects.Add(effect);
    }

    public void InsertEffect(int index, PostProcessEffect effect)
    {
        LoadEffect(effect);
        _effects.Insert(Math.Clamp(index, 0, _effects.Count), effect);
    }

    public void RemoveEffect(PostProcessEffect effect)
    {
        _effects.Remove(effect);
    }

    public void RemoveEffectAt(int index)
    {
        if (index >= 0 && index < _effects.Count)
            _effects.RemoveAt(index);
    }

    public void MoveUp(int index)
    {
        if (index > 0 && index < _effects.Count)
            (_effects[index - 1], _effects[index]) = (_effects[index], _effects[index - 1]);
    }

    public void MoveDown(int index)
    {
        if (index >= 0 && index < _effects.Count - 1)
            (_effects[index], _effects[index + 1]) = (_effects[index + 1], _effects[index]);
    }

    public void ReloadEffects()
    {
        foreach (var effect in _effects)
            LoadEffect(effect);
    }

    /// <summary>
    /// Processes the source render target through all enabled effects using ping-pong pattern.
    /// Returns the final render target containing the processed image.
    /// If no effects are enabled, returns the source unchanged.
    /// </summary>
    public RenderTarget2D Process(SpriteBatch spriteBatch, RenderTarget2D source)
    {
        var enabledEffects = _effects.Where(e => e.Enabled && e.Effect != null).ToList();
        if (enabledEffects.Count == 0)
            return source;

        EnsureRenderTargets(source.Width, source.Height);

        var current = source;
        var target = _rtA!;

        for (int i = 0; i < enabledEffects.Count; i++)
        {
            var fx = enabledEffects[i];
            fx.ApplyParameters();

            _graphicsDevice.SetRenderTarget(target);
            _graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, null, fx.Effect);
            spriteBatch.Draw(current, Vector2.Zero, Color.White);
            spriteBatch.End();

            // Ping-pong: swap current/target for next pass
            current = target;
            target = (target == _rtA) ? _rtB! : _rtA!;
        }

        _graphicsDevice.SetRenderTarget(null);
        return (RenderTarget2D)current;
    }

    private void LoadEffect(PostProcessEffect effect)
    {
        if (!string.IsNullOrEmpty(effect.EffectPath))
        {
            effect.Effect = _effectCache.Get(effect.EffectPath);
            if (effect.Effect == null)
                Log.Warn($"PostProcess: failed to load effect '{effect.EffectPath}'");
        }
    }

    private void EnsureRenderTargets(int width, int height)
    {
        if (_rtA != null && _lastWidth == width && _lastHeight == height)
            return;

        _rtA?.Dispose();
        _rtB?.Dispose();

        _rtA = new RenderTarget2D(_graphicsDevice, width, height, false,
            SurfaceFormat.Color, DepthFormat.None);
        _rtB = new RenderTarget2D(_graphicsDevice, width, height, false,
            SurfaceFormat.Color, DepthFormat.None);

        _lastWidth = width;
        _lastHeight = height;
    }

    public void Dispose()
    {
        _rtA?.Dispose();
        _rtB?.Dispose();
        _rtA = null;
        _rtB = null;
    }
}
