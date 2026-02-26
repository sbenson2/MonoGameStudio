using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

public class AtlasDocument
{
    [JsonPropertyName("texturePath")]
    public string TexturePath { get; set; } = "";

    [JsonPropertyName("entries")]
    public List<AtlasEntryData> Entries { get; set; } = new();
}

public class AtlasEntryData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public static class AtlasSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static void Save(string path, AtlasDocument document)
    {
        var json = JsonSerializer.Serialize(document, _options);
        File.WriteAllText(path, json);
    }

    public static AtlasDocument? Load(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AtlasDocument>(json, _options);
    }
}
