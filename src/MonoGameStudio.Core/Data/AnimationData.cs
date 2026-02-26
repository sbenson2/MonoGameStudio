using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

public class AnimationDocument
{
    [JsonPropertyName("spriteSheetPath")]
    public string SpriteSheetPath { get; set; } = "";

    [JsonPropertyName("clips")]
    public List<AnimationClip> Clips { get; set; } = new();
}

public class AnimationClip
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("frames")]
    public List<AnimationFrameRef> Frames { get; set; } = new();

    // Public fields for ImGui ref access
    [JsonPropertyName("loop")]
    [JsonInclude]
    public bool Loop = true;

    [JsonPropertyName("speed")]
    [JsonInclude]
    public float Speed = 1f;
}

public class AnimationFrameRef
{
    [JsonPropertyName("frameName")]
    public string FrameName { get; set; } = "";

    // Public field for ImGui ref access
    [JsonPropertyName("duration")]
    [JsonInclude]
    public float Duration = 0.1f;
}

public static class AnimationSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static AnimationDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AnimationDocument>(json, _options);
    }

    public static void Save(string path, AnimationDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, _options);
        File.WriteAllText(path, json);
    }
}
