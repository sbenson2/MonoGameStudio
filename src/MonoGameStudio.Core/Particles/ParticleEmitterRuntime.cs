using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Core.Particles;

/// <summary>
/// Per-emitter runtime state. Holds a ParticlePool, manages emission accumulation,
/// and dispatches spawn position/velocity based on the configured EmissionShape.
/// </summary>
public class ParticleEmitterRuntime
{
    private readonly ParticlePool _pool;
    private readonly ParticlePreset _preset;
    private readonly Random _rng = new();

    private float _emissionAccumulator;
    private bool _hasBurst;

    /// <summary>The underlying particle pool.</summary>
    public ParticlePool Pool => _pool;

    /// <summary>The preset driving this emitter.</summary>
    public ParticlePreset Preset => _preset;

    /// <summary>Whether the emitter is currently active.</summary>
    public bool IsEmitting { get; set; } = true;

    public ParticleEmitterRuntime(ParticlePreset preset)
    {
        _preset = preset;
        _pool = new ParticlePool(preset.MaxParticles);
    }

    /// <summary>
    /// Updates the emitter: emits new particles according to the preset, then updates the pool.
    /// </summary>
    /// <param name="deltaTime">Frame delta time in seconds.</param>
    /// <param name="emitterPosition">World position of the emitter entity.</param>
    public void Update(float deltaTime, Vector2 emitterPosition)
    {
        // Emit new particles
        if (IsEmitting)
        {
            switch (_preset.EmissionMode)
            {
                case EmissionMode.Stream:
                    EmitStream(deltaTime, emitterPosition);
                    break;

                case EmissionMode.Burst:
                    EmitBurst(emitterPosition);
                    break;
            }
        }

        // Update the pool (advance particles, kill dead ones)
        _pool.Update(deltaTime, _preset.GravityX, _preset.GravityY);
    }

    /// <summary>
    /// Resets the emitter state. Clears all particles and resets the emission accumulator.
    /// </summary>
    public void Reset()
    {
        _pool.Clear();
        _emissionAccumulator = 0f;
        _hasBurst = false;
    }

    /// <summary>
    /// Triggers a single burst emission (useful for one-shot effects).
    /// </summary>
    public void TriggerBurst(Vector2 emitterPosition)
    {
        int count = (int)_preset.EmissionRate;
        for (int i = 0; i < count; i++)
            EmitSingleParticle(emitterPosition);
    }

    private void EmitStream(float deltaTime, Vector2 emitterPosition)
    {
        if (_preset.EmissionRate <= 0f) return;

        _emissionAccumulator += _preset.EmissionRate * deltaTime;

        while (_emissionAccumulator >= 1f)
        {
            _emissionAccumulator -= 1f;
            EmitSingleParticle(emitterPosition);
        }
    }

    private void EmitBurst(Vector2 emitterPosition)
    {
        if (_hasBurst) return;
        _hasBurst = true;

        int count = (int)_preset.EmissionRate;
        for (int i = 0; i < count; i++)
            EmitSingleParticle(emitterPosition);
    }

    private void EmitSingleParticle(Vector2 emitterPosition)
    {
        if (!_pool.HasRoom) return;

        // Spawn position based on emission shape
        var spawnOffset = GetShapeOffset();
        var spawnPos = emitterPosition + spawnOffset;

        // Random speed and angle
        float speed = RandomRange(_preset.SpeedMin, _preset.SpeedMax);
        float angleRad = MathHelper.ToRadians(RandomRange(_preset.AngleMin, _preset.AngleMax));
        var velocity = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad)) * speed;

        // Random lifetime
        float lifetime = RandomRange(_preset.LifetimeMin, _preset.LifetimeMax);

        // Colors
        var startColor = ToColor(_preset.StartColor);
        var endColor = ToColor(_preset.EndColor);

        _pool.Emit(
            spawnPos,
            velocity,
            lifetime,
            startColor,
            endColor,
            _preset.ScaleStart,
            _preset.ScaleEnd);
    }

    private Vector2 GetShapeOffset()
    {
        return _preset.EmissionShape switch
        {
            EmissionShape.Point => Vector2.Zero,

            EmissionShape.Circle => GetCircleOffset(),

            EmissionShape.Rectangle => new Vector2(
                RandomRange(-_preset.ShapeWidth / 2f, _preset.ShapeWidth / 2f),
                RandomRange(-_preset.ShapeHeight / 2f, _preset.ShapeHeight / 2f)),

            EmissionShape.Edge => GetEdgeOffset(),

            _ => Vector2.Zero
        };
    }

    private Vector2 GetCircleOffset()
    {
        float angle = RandomRange(0f, MathF.Tau);
        float radius = _preset.ShapeWidth / 2f * MathF.Sqrt((float)_rng.NextDouble());
        return new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }

    private Vector2 GetEdgeOffset()
    {
        // Emit from the perimeter of a rectangle
        float halfW = _preset.ShapeWidth / 2f;
        float halfH = _preset.ShapeHeight / 2f;
        float perimeter = 2f * (_preset.ShapeWidth + _preset.ShapeHeight);
        float t = (float)_rng.NextDouble() * perimeter;

        if (t < _preset.ShapeWidth)
            return new Vector2(-halfW + t, -halfH); // Top edge
        t -= _preset.ShapeWidth;
        if (t < _preset.ShapeHeight)
            return new Vector2(halfW, -halfH + t); // Right edge
        t -= _preset.ShapeHeight;
        if (t < _preset.ShapeWidth)
            return new Vector2(halfW - t, halfH); // Bottom edge
        t -= _preset.ShapeWidth;
        return new Vector2(-halfW, halfH - t); // Left edge
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_rng.NextDouble() * (max - min);
    }

    private static Color ToColor(float[] rgba)
    {
        if (rgba.Length < 4) return Color.White;
        return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
    }
}
