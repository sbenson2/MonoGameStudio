using System.Text.Json;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Describes a component type with strongly-typed ECS operations.
/// Replaces MakeGenericMethod/Activator.CreateInstance reflection
/// with pre-compiled delegates, enabling NativeAOT compatibility.
/// </summary>
public interface IComponentDescriptor
{
    /// <summary>The CLR type of the component struct.</summary>
    Type ComponentType { get; }

    /// <summary>Short name used as the serialization key (e.g. "Position").</summary>
    string Name { get; }

    /// <summary>Category for the Add Component picker (e.g. "Rendering", "Physics").</summary>
    string Category { get; }

    /// <summary>True for tags, hierarchy, metadata — never shown in Add Component or serialized.</summary>
    bool IsInternal { get; }

    /// <summary>True for Position, Rotation, Scale — always present, serialized but not addable.</summary>
    bool IsCoreTransform { get; }

    /// <summary>Ordered list of field descriptors for inspector rendering and JSON.</summary>
    IReadOnlyList<FieldDescriptor> Fields { get; }

    // === ECS operations (replace MakeGenericMethod) ===

    bool Has(Arch.Core.World world, Arch.Core.Entity entity);
    object Get(Arch.Core.World world, Arch.Core.Entity entity);
    void Set(Arch.Core.World world, Arch.Core.Entity entity, object value);
    void Add(Arch.Core.World world, Arch.Core.Entity entity, object? value = null);
    void Remove(Arch.Core.World world, Arch.Core.Entity entity);

    // === Serialization ===

    /// <summary>Serialize a boxed component value to a JsonElement.</summary>
    JsonElement SerializeToJson(object value, JsonSerializerOptions options);

    /// <summary>Deserialize a JsonElement to a boxed component value.</summary>
    object? DeserializeFromJson(JsonElement element, JsonSerializerOptions options);

    /// <summary>Create a default instance of this component.</summary>
    object CreateDefault();
}
