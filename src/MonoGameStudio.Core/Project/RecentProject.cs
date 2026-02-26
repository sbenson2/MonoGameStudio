using System.Text.Json.Serialization;

namespace MonoGameStudio.Core.Project;

public class RecentProject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("lastOpened")]
    public DateTime LastOpened { get; set; }
}

public class RecentProjectsList
{
    [JsonPropertyName("recentProjects")]
    public List<RecentProject> Projects { get; set; } = new();
}
