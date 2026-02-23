using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using ImGuiNET;
using MonoGameStudio.Core.Components;

namespace MonoGameStudio.Editor.Inspector;

/// <summary>
/// Reflection-based component drawer for the inspector.
/// Caches FieldInfo[] per type and dispatches to FieldDrawers.
/// </summary>
public class ComponentDrawer
{
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
    private static readonly Dictionary<Type, MethodInfo> _getMethodCache = new();
    private static readonly Dictionary<Type, MethodInfo> _setMethodCache = new();
    private static readonly Dictionary<Type, MethodInfo> _hasMethodCache = new();

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

        var fields = GetFields(componentType);
        if (fields.Length == 0) return false;

        bool modified = false;

        var getMethod = GetGetMethod(componentType);
        var setMethod = GetSetMethod(componentType);

        // Read current value (boxed)
        object component;
        try
        {
            component = getMethod.Invoke(null, new object[] { world, entity })!;
        }
        catch
        {
            return false;
        }

        // Collapsing header for component
        var typeName = componentType.Name;
        bool open = ImGui.CollapsingHeader(typeName, ImGuiTreeNodeFlags.DefaultOpen);

        if (open)
        {
            ImGui.PushID(typeName);
            ImGui.Indent();

            foreach (var field in fields)
            {
                var label = field.Name;
                if (FieldDrawers.DrawField(label, field, ref component))
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
            setMethod.Invoke(null, new object[] { world, entity, component });
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

    private static FieldInfo[] GetFields(Type type)
    {
        if (!_fieldCache.TryGetValue(type, out var fields))
        {
            fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            _fieldCache[type] = fields;
        }
        return fields;
    }

    private static MethodInfo GetGetMethod(Type componentType)
    {
        if (!_getMethodCache.TryGetValue(componentType, out var method))
        {
            method = typeof(ComponentDrawerHelpers).GetMethod(nameof(ComponentDrawerHelpers.GetComponent))!
                .MakeGenericMethod(componentType);
            _getMethodCache[componentType] = method;
        }
        return method;
    }

    private static MethodInfo GetSetMethod(Type componentType)
    {
        if (!_setMethodCache.TryGetValue(componentType, out var method))
        {
            method = typeof(ComponentDrawerHelpers).GetMethod(nameof(ComponentDrawerHelpers.SetComponent))!
                .MakeGenericMethod(componentType);
            _setMethodCache[componentType] = method;
        }
        return method;
    }
}

public static class ComponentDrawerHelpers
{
    public static object GetComponent<T>(Arch.Core.World world, Entity entity) where T : struct
    {
        return world.Get<T>(entity);
    }

    public static void SetComponent<T>(Arch.Core.World world, Entity entity, object value) where T : struct
    {
        world.Set(entity, (T)value);
    }
}
