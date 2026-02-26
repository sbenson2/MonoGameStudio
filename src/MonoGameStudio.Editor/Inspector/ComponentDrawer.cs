using Arch.Core;
using Arch.Core.Extensions;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Serialization;
using ComponentRegistry = MonoGameStudio.Core.Serialization.ComponentRegistry;

namespace MonoGameStudio.Editor.Inspector;

/// <summary>
/// Descriptor-based component drawer for the inspector.
/// Uses IComponentDescriptor and FieldDescriptor instead of reflection.
/// </summary>
public class ComponentDrawer
{
    // Types that are tags or internal and should not be drawn
    private static readonly HashSet<Type> _hiddenTypes = new()
    {
        typeof(SelectedTag),
        typeof(EditorOnlyTag),
        typeof(LocalTransform),
        typeof(WorldTransform),
        typeof(Children),
        typeof(Parent)
    };

    public bool DrawComponent(Arch.Core.World world, Entity entity, Type componentType)
    {
        if (_hiddenTypes.Contains(componentType)) return false;

        var descriptor = ComponentRegistry.GetDescriptor(componentType);
        if (descriptor == null || descriptor.Fields.Count == 0) return false;

        bool modified = false;

        // Read current value (boxed)
        object component;
        try
        {
            component = descriptor.Get(world, entity);
        }
        catch
        {
            return false;
        }

        // Collapsing header for component
        bool open = ImGui.CollapsingHeader(descriptor.Name, ImGuiTreeNodeFlags.DefaultOpen);

        if (open)
        {
            ImGui.PushID(descriptor.Name);
            ImGui.Indent();

            foreach (var field in descriptor.Fields)
            {
                if (FieldDrawers.DrawField(field, ref component))
                {
                    modified = true;
                }
            }

            ImGui.Unindent();
            ImGui.PopID();
        }

        // Write back if modified
        if (modified)
        {
            descriptor.Set(world, entity, component);
        }

        return modified;
    }

    public static List<Type> GetComponentTypes(Arch.Core.World world, Entity entity)
    {
        var types = new List<Type>();
        var componentTypes = entity.GetComponentTypes();
        foreach (var ct in componentTypes)
        {
            types.Add(ct);
        }
        return types;
    }
}
