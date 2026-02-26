namespace MonoGameStudio.Core.Components;

/// <summary>
/// Marks a component struct with a category for the Add Component picker.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class ComponentCategoryAttribute : Attribute
{
    public string Category { get; }
    public ComponentCategoryAttribute(string category) { Category = category; }
}
