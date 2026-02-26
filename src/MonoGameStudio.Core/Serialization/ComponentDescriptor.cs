using System.Text.Json;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Generic implementation of IComponentDescriptor that calls
/// world.Has&lt;T&gt;(), world.Get&lt;T&gt;(), etc. directly — no reflection.
/// One instance is created per component type at startup via ComponentRegistrations.
/// </summary>
public class ComponentDescriptor<T> : IComponentDescriptor where T : struct
{
    public Type ComponentType => typeof(T);
    public required string Name { get; init; }
    public string Category { get; init; } = "General";
    public bool IsInternal { get; init; }
    public bool IsCoreTransform { get; init; }
    public required IReadOnlyList<FieldDescriptor> Fields { get; init; }

    public bool Has(Arch.Core.World world, Arch.Core.Entity entity) => world.Has<T>(entity);

    public object Get(Arch.Core.World world, Arch.Core.Entity entity) => world.Get<T>(entity);

    public void Set(Arch.Core.World world, Arch.Core.Entity entity, object value) => world.Set(entity, (T)value);

    public void Add(Arch.Core.World world, Arch.Core.Entity entity, object? value = null)
    {
        if (world.Has<T>(entity)) return;
        var instance = value != null ? (T)value : new T();
        world.Add(entity, instance);
    }

    public void Remove(Arch.Core.World world, Arch.Core.Entity entity)
    {
        if (!world.Has<T>(entity)) return;
        world.Remove<T>(entity);
    }

    public JsonElement SerializeToJson(object value, JsonSerializerOptions options)
    {
        return JsonSerializer.SerializeToElement((T)value, options);
    }

    public object? DeserializeFromJson(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
    }

    public object CreateDefault() => new T();
}
