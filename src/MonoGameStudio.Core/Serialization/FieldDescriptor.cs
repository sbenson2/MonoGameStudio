using System.Text.Json;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// The kind of value a component field holds.
/// Used for type-safe dispatch without reflection.
/// </summary>
public enum FieldKind
{
    Float,
    Int,
    Bool,
    String,
    Vector2,
    Color,
    Rectangle,
    Enum,
    Guid,
    Matrix,
}

/// <summary>
/// Metadata for a single field within a component, with typed get/set accessors.
/// Replaces FieldInfo reflection with pre-built delegates.
/// </summary>
public class FieldDescriptor
{
    public required string Name { get; init; }
    public required FieldKind Kind { get; init; }

    /// <summary>Enum type for FieldKind.Enum fields, null otherwise.</summary>
    public Type? EnumType { get; init; }

    // Inspector metadata (replaces [Range], [Tooltip], [Header], [HideInInspector] attributes)
    public float? RangeMin { get; init; }
    public float? RangeMax { get; init; }
    public string? Tooltip { get; init; }
    public string? Header { get; init; }
    public bool HideInInspector { get; init; }

    /// <summary>
    /// Gets the field value from a boxed component. Returns boxed value.
    /// </summary>
    public required Func<object, object?> GetValue { get; init; }

    /// <summary>
    /// Returns a new boxed component with the field value replaced.
    /// Components are structs, so this creates a copy with the new value.
    /// </summary>
    public required Func<object, object?, object> SetValue { get; init; }

    /// <summary>
    /// Serialize the field value to a JSON-friendly representation.
    /// Used by the C API to return inspector data as JSON.
    /// </summary>
    private static readonly JsonSerializerOptions _defaultOptions = new()
    {
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public string SerializeValue(object? value, JsonSerializerOptions? options = null)
    {
        if (value == null) return "null";
        return JsonSerializer.Serialize(value, value.GetType(), options ?? _defaultOptions);
    }
}
