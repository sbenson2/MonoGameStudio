using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

public class BuildProfileConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Debug";

    [JsonPropertyName("configuration")]
    public string Configuration { get; set; } = "Debug";

    [JsonPropertyName("runtimeIdentifier")]
    public string? RuntimeIdentifier { get; set; }

    [JsonPropertyName("selfContained")]
    public bool SelfContained { get; set; }
}
