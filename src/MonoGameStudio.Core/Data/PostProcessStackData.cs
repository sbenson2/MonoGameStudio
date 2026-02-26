using System.Text.Json;
using System.Text.Json.Serialization;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Data;

public class PostProcessStackDocument
{
    [JsonPropertyName("effects")]
    public List<PostProcessEffectData> Effects { get; set; } = new();
}

public class PostProcessEffectData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("enabled")]
    [JsonInclude]
    public bool Enabled = true;

    [JsonPropertyName("effectPath")]
    public string EffectPath { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, JsonElement> Parameters { get; set; } = new();
}

public static class PostProcessStackSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static PostProcessStackDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PostProcessStackDocument>(json, _options);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load post-process stack: {path}: {ex.Message}");
            return null;
        }
    }

    public static void Save(string path, PostProcessStackDocument doc)
    {
        var json = JsonSerializer.Serialize(doc, _options);
        File.WriteAllText(path, json);
    }
}
