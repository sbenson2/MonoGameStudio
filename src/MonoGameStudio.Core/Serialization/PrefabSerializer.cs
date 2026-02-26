using System.Text.Json;
using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Serialization;

/// <summary>
/// Serializes/deserializes entity subtrees to/from .prefab.json files.
/// Reuses the same SceneDocument/EntityData format as SceneSerializer.
/// </summary>
public static class PrefabSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        Converters =
        {
            new Vector2Converter(),
            new ColorConverter(),
            new MatrixConverter(),
            new RectangleConverter()
        }
    };

    /// <summary>
    /// Serialize an entity and all its descendants to a prefab JSON string.
    /// </summary>
    public static string Serialize(WorldManager worldManager, Entity root)
    {
        ComponentRegistry.Initialize();
        var doc = new SceneDocument { Name = "Prefab" };
        var world = worldManager.World;

        SerializeEntity(world, root, worldManager, doc);

        return JsonSerializer.Serialize(doc, _options);
    }

    private static void SerializeEntity(Arch.Core.World world, Entity entity,
        WorldManager worldManager, SceneDocument doc)
    {
        if (!world.IsAlive(entity)) return;

        var entityData = new EntityData
        {
            Guid = world.Get<EntityGuid>(entity).Id.ToString(),
            Name = world.Get<EntityName>(entity).Name
        };

        // Parent (only for child entities within the prefab)
        if (world.Has<Parent>(entity))
        {
            var parentRef = world.Get<Parent>(entity).Ref;
            if (parentRef.IsValid && world.IsAlive(parentRef.Entity) && world.Has<EntityGuid>(parentRef.Entity))
            {
                entityData.ParentGuid = world.Get<EntityGuid>(parentRef.Entity).Id.ToString();
            }
        }

        // Serialize all non-internal components
        foreach (var descriptor in ComponentRegistry.GetSerializableDescriptors())
        {
            if (!descriptor.Has(world, entity)) continue;
            var value = descriptor.Get(world, entity);
            entityData.Components[descriptor.Name] = descriptor.SerializeToJson(value, _options);
        }

        doc.Entities.Add(entityData);

        // Recurse into children
        var children = worldManager.GetChildren(entity);
        foreach (var child in children)
        {
            SerializeEntity(world, child, worldManager, doc);
        }
    }

    /// <summary>
    /// Save a prefab to a .prefab.json file.
    /// </summary>
    public static void SaveToFile(string path, WorldManager worldManager, Entity root)
    {
        var json = Serialize(worldManager, root);
        File.WriteAllText(path, json);
        Log.Info($"Prefab saved to {path}");
    }

    /// <summary>
    /// Instantiate a prefab from a .prefab.json file into the world.
    /// Returns the root entity of the instantiated prefab.
    /// </summary>
    public static Entity? InstantiateFromFile(string path, WorldManager worldManager,
        Vector2? positionOverride = null)
    {
        if (!File.Exists(path))
        {
            Log.Error($"Prefab file not found: {path}");
            return null;
        }

        var json = File.ReadAllText(path);
        return Instantiate(json, worldManager, positionOverride);
    }

    /// <summary>
    /// Instantiate a prefab from JSON into the world.
    /// Returns the root entity of the instantiated prefab.
    /// </summary>
    public static Entity? Instantiate(string json, WorldManager worldManager,
        Vector2? positionOverride = null)
    {
        ComponentRegistry.Initialize();
        var doc = JsonSerializer.Deserialize<SceneDocument>(json, _options);
        if (doc == null || doc.Entities.Count == 0) return null;

        var world = worldManager.World;
        var guidToEntity = new Dictionary<string, Entity>();
        Entity? rootEntity = null;

        // Pass 1: Create entities with new GUIDs
        foreach (var entityData in doc.Entities)
        {
            var entity = world.Create(
                new EntityName(entityData.Name),
                new EntityGuid(Guid.NewGuid()),
                new Position(0, 0),
                new Rotation(0),
                new Scale(1, 1),
                new LocalTransform { WorldMatrix = Matrix.Identity },
                new WorldTransform { WorldMatrix = Matrix.Identity },
                new Children()
            );

            // Deserialize components
            foreach (var (componentName, jsonElement) in entityData.Components)
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

            guidToEntity[entityData.Guid] = entity;
            rootEntity ??= entity;
        }

        // Pass 2: Link parents (only internal to prefab)
        foreach (var entityData in doc.Entities)
        {
            if (entityData.ParentGuid != null &&
                guidToEntity.TryGetValue(entityData.Guid, out var child) &&
                guidToEntity.TryGetValue(entityData.ParentGuid, out var parent))
            {
                worldManager.SetParent(child, parent);
            }
        }

        // Override position of root entity if requested
        if (positionOverride.HasValue && rootEntity.HasValue)
        {
            world.Set(rootEntity.Value, new Position(positionOverride.Value.X, positionOverride.Value.Y));
        }

        Log.Info($"Instantiated prefab with {doc.Entities.Count} entities");
        return rootEntity;
    }
}
