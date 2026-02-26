using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

/// <summary>
/// How particles are emitted over time.
/// </summary>
public enum EmissionMode
{
    Stream,
    Burst
}

/// <summary>
/// Shape from which particles are spawned.
/// </summary>
public enum EmissionShape
{
    Point,
    Circle,
    Rectangle,
    Edge
}

/// <summary>
/// Blend mode for particle rendering.
/// </summary>
public enum ParticleBlendMode
{
    Alpha,
    Additive
}

/// <summary>
/// Serializable particle preset data. Defines all properties for a particle emitter.
/// Saved as .particle.json files.
/// </summary>
public class ParticlePreset
{
    // --- Emission ---

    [JsonPropertyName("emissionRate")]
    [JsonInclude]
    public float EmissionRate = 10f;

    [JsonPropertyName("emissionMode")]
    public EmissionMode EmissionMode { get; set; } = EmissionMode.Stream;

    [JsonPropertyName("emissionShape")]
    public EmissionShape EmissionShape { get; set; } = EmissionShape.Point;

    [JsonPropertyName("maxParticles")]
    [JsonInclude]
    public int MaxParticles = 256;

    // --- Shape dimensions (for Rectangle / Circle / Edge) ---

    [JsonPropertyName("shapeWidth")]
    [JsonInclude]
    public float ShapeWidth = 32f;

    [JsonPropertyName("shapeHeight")]
    [JsonInclude]
    public float ShapeHeight = 32f;

    // --- Lifetime ---

    [JsonPropertyName("lifetimeMin")]
    [JsonInclude]
    public float LifetimeMin = 0.5f;

    [JsonPropertyName("lifetimeMax")]
    [JsonInclude]
    public float LifetimeMax = 2f;

    // --- Velocity ---

    [JsonPropertyName("speedMin")]
    [JsonInclude]
    public float SpeedMin = 20f;

    [JsonPropertyName("speedMax")]
    [JsonInclude]
    public float SpeedMax = 80f;

    [JsonPropertyName("angleMin")]
    [JsonInclude]
    public float AngleMin = 0f;

    [JsonPropertyName("angleMax")]
    [JsonInclude]
    public float AngleMax = 360f;

    // --- Forces ---

    [JsonPropertyName("gravityX")]
    [JsonInclude]
    public float GravityX = 0f;

    [JsonPropertyName("gravityY")]
    [JsonInclude]
    public float GravityY = 0f;

    // --- Scale ---

    [JsonPropertyName("scaleStart")]
    [JsonInclude]
    public float ScaleStart = 1f;

    [JsonPropertyName("scaleEnd")]
    [JsonInclude]
    public float ScaleEnd = 0f;

    // --- Color (RGBA as float[4]) ---

    [JsonPropertyName("startColor")]
    public float[] StartColor { get; set; } = [1f, 1f, 1f, 1f];

    [JsonPropertyName("endColor")]
    public float[] EndColor { get; set; } = [1f, 1f, 1f, 0f];

    // --- Rendering ---

    [JsonPropertyName("blendMode")]
    public ParticleBlendMode BlendMode { get; set; } = ParticleBlendMode.Alpha;

    /// <summary>
    /// Creates a deep copy of this preset.
    /// </summary>
    public ParticlePreset Clone()
    {
        return new ParticlePreset
        {
            EmissionRate = EmissionRate,
            EmissionMode = EmissionMode,
            EmissionShape = EmissionShape,
            MaxParticles = MaxParticles,
            ShapeWidth = ShapeWidth,
            ShapeHeight = ShapeHeight,
            LifetimeMin = LifetimeMin,
            LifetimeMax = LifetimeMax,
            SpeedMin = SpeedMin,
            SpeedMax = SpeedMax,
            AngleMin = AngleMin,
            AngleMax = AngleMax,
            GravityX = GravityX,
            GravityY = GravityY,
            ScaleStart = ScaleStart,
            ScaleEnd = ScaleEnd,
            StartColor = [..StartColor],
            EndColor = [..EndColor],
            BlendMode = BlendMode
        };
    }
}

/// <summary>
/// Serializer for ParticlePreset (.particle.json files).
/// </summary>
public static class ParticleSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static ParticlePreset? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ParticlePreset>(json, _options);
    }

    public static void Save(string path, ParticlePreset preset)
    {
        var json = JsonSerializer.Serialize(preset, _options);
        File.WriteAllText(path, json);
    }
}
