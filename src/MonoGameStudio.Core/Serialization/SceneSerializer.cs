using System.Text.Json;
using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Serialization;

public class SceneSerializer
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

    public static string Serialize(WorldManager worldManager)
    {
        ComponentRegistry.Initialize();
        var doc = new SceneDocument();
        var world = worldManager.World;

        var query = new QueryDescription().WithAll<EntityName, EntityGuid>();
        world.Query(query, (Entity entity, ref EntityName name, ref EntityGuid guid) =>
        {
            var entityData = new EntityData
            {
                Guid = guid.Id.ToString(),
                Name = name.Name
            };

            // Parent
            if (world.Has<Parent>(entity))
            {
                var parentRef = world.Get<Parent>(entity).Ref;
                if (parentRef.IsValid && world.IsAlive(parentRef.Entity) && world.Has<EntityGuid>(parentRef.Entity))
                {
                    entityData.ParentGuid = world.Get<EntityGuid>(parentRef.Entity).Id.ToString();
                }
            }

            // Generic component serialization via descriptors (no reflection)
            foreach (var descriptor in ComponentRegistry.GetSerializableDescriptors())
            {
                if (!descriptor.Has(world, entity)) continue;

                var value = descriptor.Get(world, entity);
                entityData.Components[descriptor.Name] = descriptor.SerializeToJson(value, _options);
            }

            doc.Entities.Add(entityData);
        });

        return JsonSerializer.Serialize(doc, _options);
    }

    public static void Deserialize(string json, WorldManager worldManager)
    {
        ComponentRegistry.Initialize();
        var doc = JsonSerializer.Deserialize<SceneDocument>(json, _options);
        if (doc == null) return;

        worldManager.ResetWorld();
        var world = worldManager.World;

        // Pass 1: create all entities with base archetype
        var guidToEntity = new Dictionary<string, Entity>();

        foreach (var entityData in doc.Entities)
        {
            var entity = world.Create(
                new EntityName(entityData.Name),
                new EntityGuid(Guid.Parse(entityData.Guid)),
                new Position(0, 0),
                new Rotation(0),
                new Scale(1, 1),
                new LocalTransform { WorldMatrix = Matrix.Identity },
                new WorldTransform { WorldMatrix = Matrix.Identity },
                new Children()
            );

            // Deserialize all components from JSON via descriptors (no reflection)
            foreach (var (componentName, jsonElement) in entityData.Components)
            {
                var descriptor = ComponentRegistry.GetDescriptor(componentName);
                if (descriptor == null)
                {
                    Log.Warn($"Unknown component type '{componentName}' in entity '{entityData.Name}'");
                    continue;
                }

                var value = descriptor.DeserializeFromJson(jsonElement, _options);
                if (value == null) continue;

                // Core transform types are already on the entity — use Set
                if (descriptor.IsCoreTransform)
                {
                    descriptor.Set(world, entity, value);
                }
                else
                {
                    // Other components need to be Added
                    descriptor.Add(world, entity, value);
                }
            }

            guidToEntity[entityData.Guid] = entity;
        }

        // Pass 2: link parents
        foreach (var entityData in doc.Entities)
        {
            if (entityData.ParentGuid != null &&
                guidToEntity.TryGetValue(entityData.Guid, out var child) &&
                guidToEntity.TryGetValue(entityData.ParentGuid, out var parent))
            {
                worldManager.SetParent(child, parent);
            }
        }

        Log.Info($"Loaded scene with {doc.Entities.Count} entities");
    }

    public static void SaveToFile(string path, WorldManager worldManager)
    {
        var json = Serialize(worldManager);
        File.WriteAllText(path, json);
        Log.Info($"Scene saved to {path}");
    }

    public static void LoadFromFile(string path, WorldManager worldManager)
    {
        if (!File.Exists(path))
        {
            Log.Error($"Scene file not found: {path}");
            return;
        }
        var json = File.ReadAllText(path);
        Deserialize(json, worldManager);
    }
}
