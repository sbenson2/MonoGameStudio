using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Project;

namespace MonoGameStudio.Editor.Project;

public class ProjectManager
{
    private readonly UserDataManager _userData;

    public ProjectInfo? CurrentProject { get; private set; }
    public bool IsProjectOpen => CurrentProject != null;

    public event Action<ProjectInfo>? OnProjectOpened;
    public event Action? OnProjectClosed;

    public IReadOnlyList<RecentProject> RecentProjects => _userData.RecentProjects;

    public ProjectManager()
    {
        _userData = new UserDataManager();
    }

    public bool CreateProject(string name, string parentDirectory, ProjectTemplateType template)
    {
        var project = ProjectScaffolder.Scaffold(name, parentDirectory, template);
        if (project == null) return false;

        OpenProjectInternal(project);
        return true;
    }

    public bool OpenProject(string mgstudioFilePath)
    {
        var project = ProjectSerializer.Load(mgstudioFilePath);
        if (project == null)
        {
            Log.Error($"Failed to open project: {mgstudioFilePath}");
            return false;
        }

        OpenProjectInternal(project);
        return true;
    }

    public void CloseProject()
    {
        if (CurrentProject == null) return;

        SaveProjectSettings();
        Log.Info($"Project closed: {CurrentProject.Name}");
        CurrentProject = null;
        OnProjectClosed?.Invoke();
    }

    public void SaveProjectSettings()
    {
        if (CurrentProject == null) return;

        CurrentProject.LastOpened = DateTime.UtcNow;
        try
        {
            ProjectSerializer.Save(CurrentProject);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save project settings: {ex.Message}");
        }
    }

    public void UpdateCameraSettings(float x, float y, float zoom)
    {
        if (CurrentProject == null) return;
        CurrentProject.EditorSettings.CameraX = x;
        CurrentProject.EditorSettings.CameraY = y;
        CurrentProject.EditorSettings.CameraZoom = zoom;
    }

    public string? GetDefaultScenePath()
    {
        if (CurrentProject == null) return null;
        var scenePath = Path.Combine(CurrentProject.ProjectDirectory, CurrentProject.DefaultScene);
        return File.Exists(scenePath) ? scenePath : null;
    }

    private void OpenProjectInternal(ProjectInfo project)
    {
        // Close any existing project first
        if (CurrentProject != null)
            CloseProject();

        project.LastOpened = DateTime.UtcNow;
        ProjectSerializer.Save(project);

        CurrentProject = project;
        _userData.AddRecentProject(project.Name, project.FilePath);

        Log.Info($"Project opened: {project.Name} ({project.ProjectDirectory})");
        OnProjectOpened?.Invoke(project);
    }
}
