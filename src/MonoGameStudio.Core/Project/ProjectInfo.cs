using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Project;

public class ProjectInfo
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastOpened")]
    public DateTime LastOpened { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("defaultScene")]
    public string DefaultScene { get; set; } = "Scenes/MainLevel.scene.json";

    [JsonPropertyName("gameProject")]
    public string? GameProject { get; set; }

    [JsonPropertyName("editorSettings")]
    public EditorSettings EditorSettings { get; set; } = new();

    /// <summary>
    /// The directory containing the .mgstudio file. Not serialized.
    /// </summary>
    [JsonIgnore]
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Full path to the .mgstudio file. Not serialized.
    /// </summary>
    [JsonIgnore]
    public string FilePath { get; set; } = "";
}

public class EditorSettings
{
    [JsonPropertyName("cameraX")]
    public float CameraX { get; set; }

    [JsonPropertyName("cameraY")]
    public float CameraY { get; set; }

    [JsonPropertyName("cameraZoom")]
    public float CameraZoom { get; set; } = 1.0f;

    [JsonPropertyName("openPanels")]
    public List<string> OpenPanels { get; set; } = new()
    {
        "Hierarchy", "Inspector", "Console", "Viewport", "AssetBrowser"
    };
}
