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
        Converters =
        {
            new Vector2Converter(),
            new ColorConverter(),
            new MatrixConverter()
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

            // Serialize Position, Rotation, Scale
            if (world.Has<Position>(entity))
            {
                var pos = world.Get<Position>(entity);
                entityData.Components["Position"] = JsonSerializer.SerializeToElement(
                    new { x = pos.X, y = pos.Y }, _options);
            }
            if (world.Has<Rotation>(entity))
            {
                var rot = world.Get<Rotation>(entity);
                entityData.Components["Rotation"] = JsonSerializer.SerializeToElement(
                    new { angle = rot.Angle }, _options);
            }
            if (world.Has<Scale>(entity))
            {
                var scale = world.Get<Scale>(entity);
                entityData.Components["Scale"] = JsonSerializer.SerializeToElement(
                    new { x = scale.X, y = scale.Y }, _options);
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

        // Pass 1: create all entities
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

            // Deserialize components
            if (entityData.Components.TryGetValue("Position", out var posEl))
            {
                world.Set(entity, new Position(
                    posEl.GetProperty("x").GetSingle(),
                    posEl.GetProperty("y").GetSingle()));
            }
            if (entityData.Components.TryGetValue("Rotation", out var rotEl))
            {
                world.Set(entity, new Rotation(rotEl.GetProperty("angle").GetSingle()));
            }
            if (entityData.Components.TryGetValue("Scale", out var scaleEl))
            {
                world.Set(entity, new Scale(
                    scaleEl.GetProperty("x").GetSingle(),
                    scaleEl.GetProperty("y").GetSingle()));
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
