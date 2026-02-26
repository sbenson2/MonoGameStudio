using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Particles;

/// <summary>
/// A single particle instance. Designed to be compact for cache-friendly
/// iteration in particle pools. ~56 bytes per particle.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    /// <summary>World position.</summary>
    public Vector2 Position;

    /// <summary>Velocity in pixels per second.</summary>
    public Vector2 Velocity;

    /// <summary>Current rotation in radians.</summary>
    public float Rotation;

    /// <summary>Current visual scale (interpolated between start/end).</summary>
    public float Scale;

    /// <summary>Current tint color (interpolated between start/end).</summary>
    public Color Color;

    /// <summary>Total lifetime in seconds.</summary>
    public float Lifetime;

    /// <summary>Elapsed time since spawn in seconds.</summary>
    public float Elapsed;

    /// <summary>Scale at spawn time.</summary>
    public float ScaleStart;

    /// <summary>Scale at death time.</summary>
    public float ScaleEnd;

    /// <summary>Color at spawn time (packed).</summary>
    public Color ColorStart;

    /// <summary>Color at death time (packed).</summary>
    public Color ColorEnd;

    /// <summary>True if the particle has not exceeded its lifetime.</summary>
    public readonly bool IsAlive => Elapsed < Lifetime;

    /// <summary>Normalized age (0 = just spawned, 1 = about to die).</summary>
    public readonly float NormalizedAge => Lifetime > 0f ? MathHelper.Clamp(Elapsed / Lifetime, 0f, 1f) : 1f;
}
