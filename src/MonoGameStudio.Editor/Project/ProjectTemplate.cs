namespace MonoGameStudio.Editor.Project;

public enum ProjectTemplateType
{
    Empty,
    Platformer2D,
    TopDownRPG
}

public class ProjectTemplate
{
    public ProjectTemplateType Type { get; }
    public string Name { get; }
    public string Description { get; }
    public string Icon { get; }

    private ProjectTemplate(ProjectTemplateType type, string name, string description, string icon)
    {
        Type = type;
        Name = name;
        Description = description;
        Icon = icon;
    }

    public static readonly ProjectTemplate[] All =
    {
        new(ProjectTemplateType.Empty, "Empty Project", "A blank project with an empty scene.", ImGuiIntegration.FontAwesomeIcons.File),
        new(ProjectTemplateType.Platformer2D, "2D Platformer", "A starter scene with player, ground, and platform entities.", ImGuiIntegration.FontAwesomeIcons.Cubes),
        new(ProjectTemplateType.TopDownRPG, "Top-Down RPG", "A starter scene with player and NPC entities.", ImGuiIntegration.FontAwesomeIcons.Cubes),
    };
}
