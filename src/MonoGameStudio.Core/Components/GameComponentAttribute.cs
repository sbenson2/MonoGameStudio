namespace MonoGameStudio.Core.Components;

/// <summary>
/// Marks a struct in a game project as a component that can be loaded by the editor.
/// The struct must be a value type with public fields.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class GameComponentAttribute : Attribute
{
    public string? Category { get; set; }
}
