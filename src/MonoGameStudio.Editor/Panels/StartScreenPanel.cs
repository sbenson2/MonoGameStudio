using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Project;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Platform;
using MonoGameStudio.Editor.Project;

namespace MonoGameStudio.Editor.Panels;

public class StartScreenPanel
{
    private readonly ProjectManager _projectManager;
    private readonly IFileDialogService _fileDialogs;

    // New Project wizard state
    private bool _showNewProjectWizard;
    private string _newProjectName = "MyGame";
    private string _newProjectLocation = GetDefaultProjectLocation();
    private int _selectedTemplate;
    private string? _wizardError;

    // Convert Project dialog state
    private bool _showConvertDialog;
    private string? _convertFolder;
    private string? _convertCsproj;

    public StartScreenPanel(ProjectManager projectManager, IFileDialogService fileDialogs)
    {
        _projectManager = projectManager;
        _fileDialogs = fileDialogs;
    }

    public void Draw()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);

        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking |
                    ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("StartScreen", flags))
        {
            DrawContent(viewport.Size);
        }

        // Draw popups inside the window context (before End)
        if (_showNewProjectWizard)
            DrawNewProjectWizard();

        if (_showConvertDialog)
            DrawConvertDialog();

        ImGui.End();
    }

    private void DrawContent(Vector2 windowSize)
    {
        // Title
        var titleY = windowSize.Y * 0.08f;
        ImGui.SetCursorPosY(titleY);

        var title = "MonoGameStudio";
        var titleSize = ImGui.CalcTextSize(title);
        ImGui.SetCursorPosX((windowSize.X - titleSize.X) / 2f);
        ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1.0f), title);

        var version = "v0.1.0";
        var versionSize = ImGui.CalcTextSize(version);
        ImGui.SetCursorPosX((windowSize.X - versionSize.X) / 2f);
        ImGui.TextDisabled(version);

        ImGui.Spacing();
        ImGui.Spacing();

        // Two-column layout
        float leftWidth = windowSize.X * 0.35f;
        float rightWidth = windowSize.X * 0.55f;
        float columnPadding = windowSize.X * 0.05f;
        float startX = columnPadding;
        float startY = ImGui.GetCursorPosY() + 20f;

        // Left column: Actions & Templates
        ImGui.SetCursorPos(new Vector2(startX, startY));
        if (ImGui.BeginChild("LeftColumn", new Vector2(leftWidth, windowSize.Y - startY - 40f)))
        {
            DrawLeftColumn(leftWidth);
        }
        ImGui.EndChild();

        // Right column: Recent Projects
        ImGui.SetCursorPos(new Vector2(startX + leftWidth + columnPadding, startY));
        if (ImGui.BeginChild("RightColumn", new Vector2(rightWidth, windowSize.Y - startY - 40f)))
        {
            DrawRecentProjects();
        }
        ImGui.EndChild();
    }

    private void DrawLeftColumn(float width)
    {
        ImGui.Text("Get Started");
        ImGui.Separator();
        ImGui.Spacing();

        float buttonWidth = width - 20f;
        float buttonHeight = 40f;

        if (ImGui.Button($"{FontAwesomeIcons.Plus}  New Project", new Vector2(buttonWidth, buttonHeight)))
        {
            _showNewProjectWizard = true;
            _newProjectName = "MyGame";
            _wizardError = null;
        }

        ImGui.Spacing();

        if (ImGui.Button($"{FontAwesomeIcons.FolderOpen}  Open Project", new Vector2(buttonWidth, buttonHeight)))
        {
            OpenProjectBrowser();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Templates");
        ImGui.Separator();
        ImGui.Spacing();

        foreach (var template in ProjectTemplate.All)
        {
            bool selected = _selectedTemplate == (int)template.Type;
            if (ImGui.Selectable($"{template.Icon}  {template.Name}", selected, ImGuiSelectableFlags.None,
                    new Vector2(0, 30f)))
            {
                _selectedTemplate = (int)template.Type;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(template.Description);
            }
        }
    }

    private void DrawRecentProjects()
    {
        ImGui.Text($"{FontAwesomeIcons.Clock}  Recent Projects");
        ImGui.Separator();
        ImGui.Spacing();

        var recent = _projectManager.RecentProjects;
        if (recent.Count == 0)
        {
            ImGui.TextDisabled("No recent projects.");
            ImGui.TextDisabled("Create a new project or open an existing one.");
            return;
        }

        for (int i = 0; i < recent.Count; i++)
        {
            var project = recent[i];
            ImGui.PushID(i);

            bool exists = File.Exists(project.Path);
            if (!exists) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

            if (ImGui.Selectable($"##recent_{i}", false, ImGuiSelectableFlags.AllowDoubleClick,
                    new Vector2(0, 50f)))
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (exists)
                        _projectManager.OpenProject(project.Path);
                    else
                        Log.Warn($"Project file not found: {project.Path}");
                }
            }

            // Draw project info overlaid on the selectable
            var itemMin = ImGui.GetItemRectMin();
            ImGui.SetCursorScreenPos(new Vector2(itemMin.X + 10f, itemMin.Y + 5f));
            ImGui.Text(project.Name);
            ImGui.SetCursorScreenPos(new Vector2(itemMin.X + 10f, itemMin.Y + 25f));
            ImGui.TextDisabled(project.Path);

            // Time ago on the right
            var timeAgo = FormatTimeAgo(project.LastOpened);
            var timeSize = ImGui.CalcTextSize(timeAgo);
            var itemMax = ImGui.GetItemRectMax();
            ImGui.SetCursorScreenPos(new Vector2(itemMax.X - timeSize.X - 10f, itemMin.Y + 5f));
            ImGui.TextDisabled(timeAgo);

            if (!exists) ImGui.PopStyleColor();

            ImGui.PopID();
        }
    }

    private void DrawNewProjectWizard()
    {
        ImGui.OpenPopup("New Project");

        var viewport = ImGui.GetMainViewport();
        var popupSize = new Vector2(500, 340);
        ImGui.SetNextWindowPos(viewport.Pos + (viewport.Size - popupSize) * 0.5f);
        ImGui.SetNextWindowSize(popupSize);

        if (ImGui.BeginPopupModal("New Project", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Spacing();

            ImGui.Text("Project Name");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##name", ref _newProjectName, 256);

            ImGui.Spacing();
            ImGui.Text("Location");
            ImGui.SetNextItemWidth(-80);
            ImGui.InputText("##location", ref _newProjectLocation, 1024);
            ImGui.SameLine();
            if (ImGui.Button("Browse", new Vector2(70, 0)))
            {
                var folder = _fileDialogs.OpenFolderDialog("Choose Location", _newProjectLocation);
                if (folder != null)
                    _newProjectLocation = folder;
            }

            ImGui.Spacing();
            ImGui.Text("Template");
            for (int i = 0; i < ProjectTemplate.All.Length; i++)
            {
                var t = ProjectTemplate.All[i];
                if (ImGui.RadioButton($"{t.Icon}  {t.Name}", _selectedTemplate == i))
                    _selectedTemplate = i;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(t.Description);
            }

            ImGui.Spacing();

            // Project path preview
            var previewPath = Path.Combine(_newProjectLocation, _newProjectName);
            ImGui.TextDisabled($"Project will be created at: {previewPath}");

            if (_wizardError != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), _wizardError);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float buttonWidth = 120;
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonWidth * 2 - 20);

            if (ImGui.Button("Create", new Vector2(buttonWidth, 0)))
            {
                TryCreateProject();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
            {
                _showNewProjectWizard = false;
                _wizardError = null;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void TryCreateProject()
    {
        if (string.IsNullOrWhiteSpace(_newProjectName))
        {
            _wizardError = "Project name cannot be empty.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_newProjectLocation))
        {
            _wizardError = "Location cannot be empty.";
            return;
        }

        if (!Directory.Exists(_newProjectLocation))
        {
            _wizardError = $"Location does not exist: {_newProjectLocation}";
            return;
        }

        var projectDir = Path.Combine(_newProjectLocation, _newProjectName);
        if (Directory.Exists(projectDir))
        {
            _wizardError = $"A folder named '{_newProjectName}' already exists at this location.";
            return;
        }

        var template = (ProjectTemplateType)_selectedTemplate;
        if (_projectManager.CreateProject(_newProjectName, _newProjectLocation, template))
        {
            _showNewProjectWizard = false;
            _wizardError = null;
            ImGui.CloseCurrentPopup();
        }
        else
        {
            _wizardError = "Failed to create project. Check the console for details.";
        }
    }

    private void OpenProjectBrowser()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultLocation = Path.Combine(home, "MonoGameProjects");
        if (!Directory.Exists(defaultLocation))
            defaultLocation = home;

        var folder = _fileDialogs.OpenFolderDialog("Open Project", defaultLocation);
        if (folder == null) return;

        // Find the .mgstudio file inside the selected folder
        try
        {
            var files = Directory.GetFiles(folder, "*.mgstudio", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                _projectManager.OpenProject(files[0]);
            }
            else
            {
                // No .mgstudio file — check if it's a MonoGame project with a .csproj
                // First check root, then all subdirectories (multi-project solutions)
                var csprojFiles = Directory.GetFiles(folder, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length == 0)
                    csprojFiles = Directory.GetFiles(folder, "*.csproj", SearchOption.AllDirectories);

                if (csprojFiles.Length > 0)
                {
                    _convertFolder = folder;
                    // Prefer Desktop project if multiple found (it's the runnable one)
                    var desktop = csprojFiles.FirstOrDefault(f =>
                        f.Contains("Desktop", StringComparison.OrdinalIgnoreCase));
                    _convertCsproj = Path.GetFileName(desktop ?? csprojFiles[0]);
                    _showConvertDialog = true;
                }
                else
                {
                    Log.Warn($"No .mgstudio or .csproj file found in: {folder}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open project folder: {ex.Message}");
        }
    }

    private void DrawConvertDialog()
    {
        ImGui.OpenPopup("Convert to MonoGameStudio Project?");

        var viewport = ImGui.GetMainViewport();
        var popupSize = new Vector2(520, 340);
        ImGui.SetNextWindowPos(viewport.Pos + (viewport.Size - popupSize) * 0.5f);
        ImGui.SetNextWindowSize(popupSize);

        if (ImGui.BeginPopupModal("Convert to MonoGameStudio Project?", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Spacing();

            ImGui.Text($"Detected project: {_convertCsproj}");
            ImGui.TextDisabled(_convertFolder);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("The following will be created:");
            ImGui.Spacing();

            var folderName = Path.GetFileName(_convertFolder);
            ImGui.BulletText($"{folderName}.mgstudio");
            ImGui.BulletText("Scenes/");
            ImGui.BulletText("Scenes/MainLevel.scene.json");
            ImGui.BulletText("Assets/Textures/");
            ImGui.BulletText("Assets/Audio/");
            ImGui.BulletText("Assets/Fonts/");
            ImGui.BulletText("Prefabs/");

            ImGui.Spacing();
            ImGui.TextDisabled("Existing folders will not be modified.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            float buttonWidth = 120;
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonWidth * 2 - 20);

            if (ImGui.Button("Convert", new Vector2(buttonWidth, 0)))
            {
                TryConvertProject();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
            {
                _showConvertDialog = false;
                _convertFolder = null;
                _convertCsproj = null;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void TryConvertProject()
    {
        if (_convertFolder == null || _convertCsproj == null) return;

        try
        {
            var folderName = Path.GetFileName(_convertFolder);
            var mgstudioPath = Path.Combine(_convertFolder, $"{folderName}.mgstudio");

            // Scaffold directories (no-op if they already exist)
            Directory.CreateDirectory(Path.Combine(_convertFolder, "Scenes"));
            Directory.CreateDirectory(Path.Combine(_convertFolder, "Assets", "Textures"));
            Directory.CreateDirectory(Path.Combine(_convertFolder, "Assets", "Audio"));
            Directory.CreateDirectory(Path.Combine(_convertFolder, "Assets", "Fonts"));
            Directory.CreateDirectory(Path.Combine(_convertFolder, "Prefabs"));

            // Create default scene if it doesn't exist
            var defaultScenePath = Path.Combine(_convertFolder, "Scenes", "MainLevel.scene.json");
            if (!File.Exists(defaultScenePath))
            {
                var emptyScene = new Core.Serialization.SceneDocument { Name = "MainLevel" };
                var json = System.Text.Json.JsonSerializer.Serialize(emptyScene,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(defaultScenePath, json);
            }

            // Create .mgstudio file
            var project = new ProjectInfo
            {
                Name = folderName,
                GameProject = _convertCsproj,
                DefaultScene = "Scenes/MainLevel.scene.json",
                ProjectDirectory = _convertFolder,
                FilePath = mgstudioPath
            };
            ProjectSerializer.Save(project);

            Log.Info($"Converted {folderName} to MonoGameStudio project");

            _showConvertDialog = false;
            _convertFolder = null;
            _convertCsproj = null;
            ImGui.CloseCurrentPopup();

            _projectManager.OpenProject(mgstudioPath);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to convert project: {ex.Message}");
        }
    }

    private static string GetDefaultProjectLocation()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(docs, "MonoGameProjects");
    }

    private static string FormatTimeAgo(DateTime utcTime)
    {
        var span = DateTime.UtcNow - utcTime;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return utcTime.ToLocalTime().ToString("MMM d, yyyy");
    }
}
