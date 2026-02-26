namespace MonoGameStudio.Core.Components;

/// <summary>
/// ECS component that attaches a particle emitter to an entity.
/// References a .particle.json preset file by path.
/// </summary>
[ComponentCategory("Particles")]
public struct ParticleEmitter
{
    /// <summary>Path to the .particle.json preset file.</summary>
    public string PresetPath;

    /// <summary>Whether this emitter is currently emitting particles.</summary>
    public bool IsEmitting;

    /// <summary>Whether to start emitting automatically when the scene starts.</summary>
    public bool PlayOnStart;

    public ParticleEmitter()
    {
        PresetPath = "";
        IsEmitting = false;
        PlayOnStart = false;
    }
}
