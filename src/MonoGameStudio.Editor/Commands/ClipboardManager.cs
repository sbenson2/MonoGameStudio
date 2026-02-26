using System.Text.Json;
using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Serialization;
using ComponentRegistry = MonoGameStudio.Core.Serialization.ComponentRegistry;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Editor;

namespace MonoGameStudio.Editor.Commands;

/// <summary>
/// Manages in-memory copy/paste of entities using ComponentRegistry serialization.
/// </summary>
public class ClipboardManager
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        Converters =
        {
            new Vector2Converter(),
            new ColorConverter(),
            new MatrixConverter(),
            new RectangleConverter()
        }
    };

    // Clipboard stores serialized entity data
    private List<ClipboardEntity>? _clipboard;

    public ClipboardManager(WorldManager worldManager, EditorState editorState)
    {
        _worldManager = worldManager;
        _editorState = editorState;
    }

    public bool HasClipboard => _clipboard != null && _clipboard.Count > 0;

    public void Copy()
    {
        var selected = _editorState.SelectedEntities;
        if (selected.Count == 0) return;

        ComponentRegistry.Initialize();
        var world = _worldManager.World;
        _clipboard = new List<ClipboardEntity>();

        foreach (var entity in selected)
        {
            if (!world.IsAlive(entity)) continue;

            var entry = new ClipboardEntity
            {
                Name = world.Get<EntityName>(entity).Name
            };

            // Serialize all serializable components
            foreach (var descriptor in ComponentRegistry.GetSerializableDescriptors())
            {
                if (!descriptor.Has(world, entity)) continue;
                var value = descriptor.Get(world, entity);
                entry.Components[descriptor.Name] = descriptor.SerializeToJson(value, _options);
            }

            _clipboard.Add(entry);
        }

        Log.Info($"Copied {_clipboard.Count} entit{(_clipboard.Count == 1 ? "y" : "ies")} to clipboard");
    }

    public List<Entity> Paste()
    {
        if (_clipboard == null || _clipboard.Count == 0) return new();

        ComponentRegistry.Initialize();
        var world = _worldManager.World;
        var created = new List<Entity>();

        foreach (var entry in _clipboard)
        {
            var entity = _worldManager.CreateEntity(entry.Name + " (Paste)");

            // Deserialize all components
            foreach (var (componentName, jsonElement) in entry.Components)
            {
                var descriptor = ComponentRegistry.GetDescriptor(componentName);
                if (descriptor == null) continue;

                var value = descriptor.DeserializeFromJson(jsonElement, _options);
                if (value == null) continue;

                if (descriptor.IsCoreTransform)
                    descriptor.Set(world, entity, value);
                else
                    descriptor.Add(world, entity, value);
            }

            // Offset position and assign new GUID
            var pos = world.Get<Position>(entity);
            world.Set(entity, new Position(pos.X + 20, pos.Y + 20));
            world.Set(entity, new EntityGuid(Guid.NewGuid()));

            created.Add(entity);
        }

        Log.Info($"Pasted {created.Count} entit{(created.Count == 1 ? "y" : "ies")} from clipboard");
        return created;
    }

    private class ClipboardEntity
    {
        public string Name { get; set; } = "";
        public Dictionary<string, JsonElement> Components { get; set; } = new();
    }
}
