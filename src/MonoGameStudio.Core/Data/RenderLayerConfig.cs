using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Data;

public class RenderLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}

public class RenderLayerConfig
{
    [JsonPropertyName("layers")]
    public List<RenderLayer> Layers { get; set; } = new()
    {
        new() { Name = "Background", Priority = -100 },
        new() { Name = "Default", Priority = 0 },
        new() { Name = "Foreground", Priority = 100 },
        new() { Name = "UI", Priority = 1000 },
    };

    public int GetPriority(string layerName)
    {
        var layer = Layers.Find(l => l.Name == layerName);
        return layer?.Priority ?? 0;
    }

    public string[] GetLayerNames() => Layers.Select(l => l.Name).ToArray();
}
