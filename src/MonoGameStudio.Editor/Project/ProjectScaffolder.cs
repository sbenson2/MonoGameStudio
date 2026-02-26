using System.Diagnostics;
using System.Text.Json;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Project;
using MonoGameStudio.Core.Serialization;

namespace MonoGameStudio.Editor.Project;

public static class ProjectScaffolder
{
    /// <summary>
    /// Creates a new project on disk: runs dotnet new, creates .mgstudio file, scaffolds directories.
    /// Returns the ProjectInfo if successful, null on failure.
    /// </summary>
    public static ProjectInfo? Scaffold(string name, string parentDirectory, ProjectTemplateType template)
    {
        var projectDir = Path.Combine(parentDirectory, name);

        if (Directory.Exists(projectDir))
        {
            Log.Error($"Directory already exists: {projectDir}");
            return null;
        }

        // 1. Run dotnet new mgdesktopgl
        if (!RunDotnetNew(name, projectDir))
        {
            Log.Warn("dotnet new failed — creating project structure without MonoGame template.");
            Directory.CreateDirectory(projectDir);
        }

        // 2. Create MonoGameStudio directories
        Directory.CreateDirectory(Path.Combine(projectDir, "Scenes"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Assets", "Textures"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Assets", "Audio"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Assets", "Fonts"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Prefabs"));

        // 3. Create default scene
        var defaultScenePath = Path.Combine(projectDir, "Scenes", "MainLevel.scene.json");
        var scene = CreateDefaultScene(template);
        File.WriteAllText(defaultScenePath, scene);

        // 4. Create .mgstudio file
        var csprojName = FindCsproj(projectDir);
        var project = ProjectSerializer.Create(name, parentDirectory);
        project.GameProject = csprojName;
        project.DefaultScene = "Scenes/MainLevel.scene.json";
        ProjectSerializer.Save(project);

        Log.Info($"Project created: {projectDir}");
        return project;
    }

    private static bool RunDotnetNew(string name, string outputDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new mgdesktopgl -n {name} -o \"{outputDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            process.WaitForExit(30_000);
            if (process.ExitCode != 0)
            {
                var err = process.StandardError.ReadToEnd();
                Log.Warn($"dotnet new exited with code {process.ExitCode}: {err}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to run dotnet new: {ex.Message}");
            return false;
        }
    }

    private static string? FindCsproj(string directory)
    {
        if (!Directory.Exists(directory)) return null;
        var files = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        return files.Length > 0 ? Path.GetFileName(files[0]) : null;
    }

    private static string CreateDefaultScene(ProjectTemplateType template)
    {
        var doc = new SceneDocument { Name = "MainLevel" };

        switch (template)
        {
            case ProjectTemplateType.Platformer2D:
                doc.Entities.Add(CreateEntityData("Player", 0, -50));
                doc.Entities.Add(CreateEntityData("Ground", 0, 100));
                doc.Entities.Add(CreateEntityData("Platform", 150, 0));
                break;

            case ProjectTemplateType.TopDownRPG:
                doc.Entities.Add(CreateEntityData("Player", 0, 0));
                doc.Entities.Add(CreateEntityData("NPC", 100, 100));
                break;

            case ProjectTemplateType.Empty:
            default:
                // Empty scene — no entities
                break;
        }

        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    private static EntityData CreateEntityData(string name, float x, float y)
    {
        var entity = new EntityData
        {
            Guid = Guid.NewGuid().ToString(),
            Name = name
        };

        entity.Components["Position"] = JsonSerializer.SerializeToElement(new { x, y });
        entity.Components["Rotation"] = JsonSerializer.SerializeToElement(new { angle = 0f });
        entity.Components["Scale"] = JsonSerializer.SerializeToElement(new { x = 1f, y = 1f });

        return entity;
    }
}
