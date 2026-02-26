using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Particles;

/// <summary>
/// Fixed-capacity particle pool. Uses a swap-dead-to-end update pattern
/// so alive particles are always contiguous at the front of the array.
/// </summary>
public class ParticlePool
{
    private readonly Particle[] _particles;
    private int _aliveCount;

    /// <summary>Number of alive particles in the pool.</summary>
    public int AliveCount => _aliveCount;

    /// <summary>Maximum capacity of the pool.</summary>
    public int Capacity => _particles.Length;

    /// <summary>True if the pool has room for more particles.</summary>
    public bool HasRoom => _aliveCount < _particles.Length;

    public ParticlePool(int capacity)
    {
        _particles = new Particle[capacity];
        _aliveCount = 0;
    }

    /// <summary>
    /// Emits a single particle. Returns false if the pool is full.
    /// </summary>
    public bool Emit(
        Vector2 position,
        Vector2 velocity,
        float lifetime,
        Color startColor,
        Color endColor,
        float startScale,
        float endScale,
        float rotation = 0f)
    {
        if (_aliveCount >= _particles.Length)
            return false;

        ref var p = ref _particles[_aliveCount];
        p.Position = position;
        p.Velocity = velocity;
        p.Lifetime = lifetime;
        p.Elapsed = 0f;
        p.Rotation = rotation;
        p.Scale = startScale;
        p.ScaleStart = startScale;
        p.ScaleEnd = endScale;
        p.Color = startColor;
        p.ColorStart = startColor;
        p.ColorEnd = endColor;

        _aliveCount++;
        return true;
    }

    /// <summary>
    /// Advances all particles by deltaTime. Applies gravity, interpolates scale/color,
    /// and swaps dead particles to the end of the alive region.
    /// </summary>
    public void Update(float deltaTime, float gravityX = 0f, float gravityY = 0f)
    {
        int i = 0;
        while (i < _aliveCount)
        {
            ref var p = ref _particles[i];
            p.Elapsed += deltaTime;

            if (!p.IsAlive)
            {
                // Swap with the last alive particle
                _aliveCount--;
                if (i < _aliveCount)
                    _particles[i] = _particles[_aliveCount];

                // Don't increment i — re-check the swapped particle
                continue;
            }

            // Apply gravity
            p.Velocity.X += gravityX * deltaTime;
            p.Velocity.Y += gravityY * deltaTime;

            // Integrate position
            p.Position.X += p.Velocity.X * deltaTime;
            p.Position.Y += p.Velocity.Y * deltaTime;

            // Interpolate scale
            float t = p.NormalizedAge;
            p.Scale = MathHelper.Lerp(p.ScaleStart, p.ScaleEnd, t);

            // Interpolate color
            p.Color = Color.Lerp(p.ColorStart, p.ColorEnd, t);

            i++;
        }
    }

    /// <summary>
    /// Returns a ReadOnlySpan over the alive particles (contiguous at front of array).
    /// </summary>
    public ReadOnlySpan<Particle> GetAliveParticles()
    {
        return new ReadOnlySpan<Particle>(_particles, 0, _aliveCount);
    }

    /// <summary>
    /// Kills all particles immediately.
    /// </summary>
    public void Clear()
    {
        _aliveCount = 0;
    }
}
