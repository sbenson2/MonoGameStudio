using System.Reflection;
using System.Runtime.Loader;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Loads a game DLL, scans for [GameComponent] structs, and registers them as dynamic component descriptors.
/// Uses AssemblyLoadContext for isolation.
/// </summary>
public class ExternalComponentLoader
{
    private AssemblyLoadContext? _loadContext;
    private Assembly? _loadedAssembly;
    private readonly List<DynamicComponentDescriptor> _registeredDescriptors = new();

    public IReadOnlyList<DynamicComponentDescriptor> RegisteredDescriptors => _registeredDescriptors;

    public void LoadGameAssembly(string dllPath)
    {
        if (!File.Exists(dllPath))
        {
            Log.Error($"Game assembly not found: {dllPath}");
            return;
        }

        Unload();

        _loadContext = new AssemblyLoadContext("GameComponents", isCollectible: true);
        try
        {
            _loadedAssembly = _loadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
            ScanAndRegister();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load game assembly: {ex.Message}");
            Unload();
        }
    }

    private void ScanAndRegister()
    {
        if (_loadedAssembly == null) return;

        foreach (var type in _loadedAssembly.GetExportedTypes())
        {
            if (!type.IsValueType) continue;
            var attr = type.GetCustomAttribute<GameComponentAttribute>();
            if (attr == null) continue;

            var descriptor = new DynamicComponentDescriptor(type);
            descriptor.Category = attr.Category ?? "Game";
            descriptor.BuildFieldsFromReflection();

            ComponentRegistry.Register(descriptor);
            _registeredDescriptors.Add(descriptor);
            Log.Info($"Registered game component: {type.Name} (category: {descriptor.Category})");
        }
    }

    public void Unload()
    {
        // Unregister descriptors
        foreach (var desc in _registeredDescriptors)
        {
            ComponentRegistry.Unregister(desc.Name);
        }
        _registeredDescriptors.Clear();

        _loadedAssembly = null;
        _loadContext?.Unload();
        _loadContext = null;
    }
}
