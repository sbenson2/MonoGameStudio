namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Component type registry using static descriptors (NativeAOT-safe).
/// No reflection — all types registered explicitly via ComponentRegistrations.RegisterAll().
/// </summary>
public static class ComponentRegistry
{
    private static readonly Dictionary<string, IComponentDescriptor> _nameToDescriptor = new();
    private static readonly Dictionary<Type, IComponentDescriptor> _typeToDescriptor = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        ComponentRegistrations.RegisterAll();
    }

    public static void Register(IComponentDescriptor descriptor)
    {
        _nameToDescriptor[descriptor.Name] = descriptor;
        _typeToDescriptor[descriptor.ComponentType] = descriptor;
    }

    public static void Unregister(string name)
    {
        if (_nameToDescriptor.TryGetValue(name, out var descriptor))
        {
            _nameToDescriptor.Remove(name);
            _typeToDescriptor.Remove(descriptor.ComponentType);
        }
    }

    public static IComponentDescriptor? GetDescriptor(string name) =>
        _nameToDescriptor.TryGetValue(name, out var d) ? d : null;

    public static IComponentDescriptor? GetDescriptor(Type type) =>
        _typeToDescriptor.TryGetValue(type, out var d) ? d : null;

    public static Type? GetType(string name) =>
        _nameToDescriptor.TryGetValue(name, out var d) ? d.ComponentType : null;

    public static string? GetName(Type type) =>
        _typeToDescriptor.TryGetValue(type, out var d) ? d.Name : null;

    public static string GetCategory(Type type) =>
        _typeToDescriptor.TryGetValue(type, out var d) ? d.Category : "General";

    public static IEnumerable<IComponentDescriptor> AllDescriptors => _nameToDescriptor.Values;

    public static IEnumerable<Type> AllTypes => _nameToDescriptor.Values.Select(d => d.ComponentType);

    /// <summary>
    /// Returns descriptors suitable for the Add Component picker.
    /// Excludes internal types and core transform types.
    /// </summary>
    public static IEnumerable<IComponentDescriptor> GetAddableDescriptors()
    {
        Initialize();
        return _nameToDescriptor.Values
            .Where(d => !d.IsInternal && !d.IsCoreTransform);
    }

    /// <summary>
    /// Returns component types suitable for the Add Component picker.
    /// </summary>
    public static IEnumerable<Type> GetAddableTypes()
    {
        return GetAddableDescriptors().Select(d => d.ComponentType);
    }

    /// <summary>
    /// Returns descriptors for components that should be serialized to scene JSON.
    /// Excludes internal types but includes core transforms.
    /// </summary>
    public static IEnumerable<IComponentDescriptor> GetSerializableDescriptors()
    {
        Initialize();
        return _nameToDescriptor.Values
            .Where(d => !d.IsInternal);
    }

    /// <summary>
    /// Returns component types that should be serialized to scene JSON.
    /// </summary>
    public static IEnumerable<Type> GetSerializableTypes()
    {
        return GetSerializableDescriptors().Select(d => d.ComponentType);
    }

    public static bool IsInternalType(Type type) =>
        _typeToDescriptor.TryGetValue(type, out var d) && d.IsInternal;

    public static bool IsCoreTransformType(Type type) =>
        _typeToDescriptor.TryGetValue(type, out var d) && d.IsCoreTransform;
}
