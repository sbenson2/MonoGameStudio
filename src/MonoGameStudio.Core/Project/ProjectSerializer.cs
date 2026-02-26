using System.Text.Json;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Project;

public static class ProjectSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static void Save(ProjectInfo project)
    {
        var json = JsonSerializer.Serialize(project, _options);
        File.WriteAllText(project.FilePath, json);
    }

    public static ProjectInfo? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Warn($"Project file not found: {filePath}");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var project = JsonSerializer.Deserialize<ProjectInfo>(json, _options);
            if (project == null) return null;

            project.FilePath = Path.GetFullPath(filePath);
            project.ProjectDirectory = Path.GetDirectoryName(project.FilePath) ?? "";
            return project;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load project: {ex.Message}");
            return null;
        }
    }

    public static ProjectInfo Create(string name, string directory)
    {
        var projectDir = Path.Combine(directory, name);
        var filePath = Path.Combine(projectDir, $"{name}.mgstudio");

        var project = new ProjectInfo
        {
            Name = name,
            Created = DateTime.UtcNow,
            LastOpened = DateTime.UtcNow,
            ProjectDirectory = projectDir,
            FilePath = filePath
        };

        return project;
    }
}
