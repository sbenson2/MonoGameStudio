using System.Reflection;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Discovers and maps component types by short name for serialization.
/// </summary>
public static class ComponentRegistry
{
    private static readonly Dictionary<string, Type> _nameToType = new();
    private static readonly Dictionary<Type, string> _typeToName = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Scan the Core assembly for component structs
        var coreAssembly = typeof(ComponentRegistry).Assembly;
        RegisterComponentsFrom(coreAssembly);
    }

    public static void RegisterComponentsFrom(Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsValueType && !type.IsEnum && type.Namespace?.Contains("Components") == true)
            {
                Register(type);
            }
        }
    }

    public static void Register(Type type)
    {
        var name = type.Name;
        _nameToType[name] = type;
        _typeToName[type] = name;
    }

    public static Type? GetType(string name) =>
        _nameToType.TryGetValue(name, out var type) ? type : null;

    public static string? GetName(Type type) =>
        _typeToName.TryGetValue(type, out var name) ? name : null;

    public static IEnumerable<Type> AllTypes => _nameToType.Values;
}
