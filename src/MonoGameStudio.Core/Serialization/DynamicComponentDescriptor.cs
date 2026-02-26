using System.Reflection;
using System.Text.Json;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Reflection-based IComponentDescriptor for externally loaded game components.
/// Uses MakeGenericMethod to call Arch's typed Has/Get/Set/Add/Remove.
/// </summary>
public class DynamicComponentDescriptor : IComponentDescriptor
{
    private readonly Type _componentType;
    private readonly MethodInfo _has;
    private readonly MethodInfo _get;
    private readonly MethodInfo _set;
    private readonly MethodInfo _add;
    private readonly MethodInfo _remove;
    private FieldDescriptor[] _fields = Array.Empty<FieldDescriptor>();

    public string Name { get; }
    public string Category { get; set; } = "Game";
    public bool IsCoreTransform => false;
    public bool IsInternal => false;
    public IReadOnlyList<FieldDescriptor> Fields => _fields;
    public Type ComponentType => _componentType;

    public DynamicComponentDescriptor(Type componentType)
    {
        _componentType = componentType;
        Name = componentType.Name;

        // Resolve generic methods from Arch.Core.World
        var worldType = typeof(Arch.Core.World);
        _has = worldType.GetMethod("Has", [typeof(Arch.Core.Entity)])!.MakeGenericMethod(componentType);
        _get = worldType.GetMethod("Get", [typeof(Arch.Core.Entity)])!.MakeGenericMethod(componentType);
        _set = worldType.GetMethod("Set", [typeof(Arch.Core.Entity)])!.MakeGenericMethod(componentType);
        _add = FindAddMethod(worldType, componentType);
        _remove = worldType.GetMethod("Remove", [typeof(Arch.Core.Entity)])!.MakeGenericMethod(componentType);
    }

    private static MethodInfo FindAddMethod(Type worldType, Type componentType)
    {
        foreach (var m in worldType.GetMethods())
        {
            if (m.Name == "Add" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
            {
                var p = m.GetParameters();
                if (p[0].ParameterType == typeof(Arch.Core.Entity))
                    return m.MakeGenericMethod(componentType);
            }
        }
        throw new InvalidOperationException($"Cannot find Add method for {componentType}");
    }

    public bool Has(Arch.Core.World world, Arch.Core.Entity entity) =>
        (bool)_has.Invoke(world, [entity])!;

    public object Get(Arch.Core.World world, Arch.Core.Entity entity) =>
        _get.Invoke(world, [entity])!;

    public void Set(Arch.Core.World world, Arch.Core.Entity entity, object value) =>
        _set.Invoke(world, [entity, value]);

    public void Add(Arch.Core.World world, Arch.Core.Entity entity, object? value = null)
    {
        var val = value ?? Activator.CreateInstance(_componentType)!;
        _add.Invoke(world, [entity, val]);
    }

    public void Remove(Arch.Core.World world, Arch.Core.Entity entity) =>
        _remove.Invoke(world, [entity]);

    public JsonElement SerializeToJson(object value, JsonSerializerOptions options) =>
        JsonSerializer.SerializeToElement(value, _componentType, options);

    public object? DeserializeFromJson(JsonElement element, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize(element.GetRawText(), _componentType, options);

    public object CreateDefault() => Activator.CreateInstance(_componentType)!;

    /// <summary>
    /// Build FieldDescriptors from public fields using reflection.
    /// </summary>
    public void BuildFieldsFromReflection()
    {
        var fields = _componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        var descriptors = new List<FieldDescriptor>();

        foreach (var field in fields)
        {
            var kind = ResolveFieldKind(field.FieldType);
            if (kind == null) continue;

            var capturedField = field;
            descriptors.Add(new FieldDescriptor
            {
                Name = field.Name,
                Kind = kind.Value,
                EnumType = field.FieldType.IsEnum ? field.FieldType : null,
                GetValue = component => capturedField.GetValue(component),
                SetValue = (component, value) =>
                {
                    var boxed = component;
                    capturedField.SetValue(boxed, value);
                    return boxed;
                }
            });
        }

        _fields = descriptors.ToArray();
    }

    private static FieldKind? ResolveFieldKind(Type type)
    {
        if (type == typeof(float)) return FieldKind.Float;
        if (type == typeof(int)) return FieldKind.Int;
        if (type == typeof(bool)) return FieldKind.Bool;
        if (type == typeof(string)) return FieldKind.String;
        if (type == typeof(Microsoft.Xna.Framework.Vector2)) return FieldKind.Vector2;
        if (type == typeof(Microsoft.Xna.Framework.Color)) return FieldKind.Color;
        if (type == typeof(Microsoft.Xna.Framework.Rectangle)) return FieldKind.Rectangle;
        if (type == typeof(Guid)) return FieldKind.Guid;
        if (type.IsEnum) return FieldKind.Enum;
        return null;
    }
}
