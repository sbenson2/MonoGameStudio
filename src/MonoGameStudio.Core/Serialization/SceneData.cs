using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Serialization;

public class SceneDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Untitled Scene";

    [JsonPropertyName("entities")]
    public List<EntityData> Entities { get; set; } = new();
}

public class EntityData
{
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("parentGuid")]
    public string? ParentGuid { get; set; }

    [JsonPropertyName("components")]
    public Dictionary<string, System.Text.Json.JsonElement> Components { get; set; } = new();
}
