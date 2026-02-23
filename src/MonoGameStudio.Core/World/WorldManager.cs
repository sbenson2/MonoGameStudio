using Arch.Core;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Core.World;

public class WorldManager
{
    private Arch.Core.World _world;
    private int _entityCounter;

    public Arch.Core.World World => _world;

    public WorldManager()
    {
        _world = Arch.Core.World.Create();
    }

    public Entity CreateEntity(string name = "Entity", Entity? parent = null)
    {
        _entityCounter++;
        var actualName = name == "Entity" ? $"Entity ({_entityCounter})" : name;

        var entity = _world.Create(
            new EntityName(actualName),
            new EntityGuid(Guid.NewGuid()),
            new Position(0, 0),
            new Rotation(0),
            new Scale(1, 1),
            new LocalTransform { WorldMatrix = Microsoft.Xna.Framework.Matrix.Identity },
            new WorldTransform { WorldMatrix = Microsoft.Xna.Framework.Matrix.Identity },
            new Children()
        );

        if (parent.HasValue)
        {
            SetParent(entity, parent.Value);
        }

        Log.Info($"Created entity '{actualName}'");
        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (!_world.IsAlive(entity)) return;

        // Remove from parent's children list
        if (_world.Has<Parent>(entity))
        {
            var parentRef = _world.Get<Parent>(entity).Ref;
            if (parentRef.IsValid && _world.IsAlive(parentRef.Entity))
                RemoveChildRef(parentRef.Entity, entity);
        }

        // Destroy all children recursively
        if (_world.Has<Children>(entity))
        {
            var children = _world.Get<Children>(entity);
            foreach (var childRef in children.Refs.ToArray())
            {
                if (childRef.IsValid && _world.IsAlive(childRef.Entity))
                    DestroyEntity(childRef.Entity);
            }
        }

        var name = _world.Has<EntityName>(entity) ? _world.Get<EntityName>(entity).Name : "Unknown";
        _world.Destroy(entity);
        Log.Info($"Destroyed entity '{name}'");
    }

    public Entity DuplicateEntity(Entity source)
    {
        if (!_world.IsAlive(source)) throw new InvalidOperationException("Source entity is not alive");

        var name = _world.Get<EntityName>(source).Name + " (Copy)";
        var pos = _world.Get<Position>(source);
        var rot = _world.Get<Rotation>(source);
        var scale = _world.Get<Scale>(source);

        Entity? parent = null;
        if (_world.Has<Parent>(source))
        {
            var parentRef = _world.Get<Parent>(source).Ref;
            if (parentRef.IsValid && _world.IsAlive(parentRef.Entity))
                parent = parentRef.Entity;
        }

        var newEntity = CreateEntity(name, parent);
        _world.Set(newEntity, new Position(pos.X + 20, pos.Y + 20));
        _world.Set(newEntity, rot);
        _world.Set(newEntity, scale);

        return newEntity;
    }

    public void SetParent(Entity child, Entity parent)
    {
        if (!_world.IsAlive(child) || !_world.IsAlive(parent)) return;
        if (child.Equals(parent)) return;

        // Prevent circular parenting
        if (IsDescendantOf(parent, child)) return;

        // Remove from old parent
        if (_world.Has<Parent>(child))
        {
            var oldParentRef = _world.Get<Parent>(child).Ref;
            if (oldParentRef.IsValid && _world.IsAlive(oldParentRef.Entity))
                RemoveChildRef(oldParentRef.Entity, child);
            _world.Remove<Parent>(child);
        }

        // Set new parent
        if (_world.Has<Parent>(child))
            _world.Set(child, new Parent(parent));
        else
            _world.Add(child, new Parent(parent));

        // Add to parent's children
        if (!_world.Has<Children>(parent))
            _world.Add(parent, new Children());

        var children = _world.Get<Children>(parent);
        children.Refs.Add(new EntityRef(child));
        _world.Set(parent, children);
    }

    public void RemoveParent(Entity child)
    {
        if (!_world.IsAlive(child) || !_world.Has<Parent>(child)) return;

        var parentRef = _world.Get<Parent>(child).Ref;
        if (parentRef.IsValid && _world.IsAlive(parentRef.Entity))
            RemoveChildRef(parentRef.Entity, child);

        _world.Remove<Parent>(child);
    }

    public bool IsDescendantOf(Entity entity, Entity potentialAncestor)
    {
        if (!_world.IsAlive(entity) || !_world.Has<Parent>(entity)) return false;

        var parentRef = _world.Get<Parent>(entity).Ref;
        if (!parentRef.IsValid || !_world.IsAlive(parentRef.Entity)) return false;
        if (parentRef.Entity.Equals(potentialAncestor)) return true;

        return IsDescendantOf(parentRef.Entity, potentialAncestor);
    }

    public List<Entity> GetRootEntities()
    {
        var roots = new List<Entity>();
        var query = new QueryDescription().WithAll<EntityName>().WithNone<Parent>();
        _world.Query(query, (Entity entity) =>
        {
            roots.Add(entity);
        });
        return roots;
    }

    public List<Entity> GetChildren(Entity entity)
    {
        var result = new List<Entity>();
        if (!_world.IsAlive(entity) || !_world.Has<Children>(entity)) return result;

        var children = _world.Get<Children>(entity);
        foreach (var childRef in children.Refs)
        {
            if (childRef.IsValid && _world.IsAlive(childRef.Entity))
                result.Add(childRef.Entity);
        }
        return result;
    }

    public void RenameEntity(Entity entity, string newName)
    {
        if (!_world.IsAlive(entity)) return;
        _world.Set(entity, new EntityName(newName));
    }

    public void ResetWorld()
    {
        _entityCounter = 0;
        Arch.Core.World.Destroy(_world);
        _world = Arch.Core.World.Create();
    }

    public int EntityCount
    {
        get
        {
            int count = 0;
            var query = new QueryDescription().WithAll<EntityName>();
            _world.Query(query, (Entity _) => count++);
            return count;
        }
    }

    private void RemoveChildRef(Entity parent, Entity child)
    {
        if (!_world.IsAlive(parent) || !_world.Has<Children>(parent)) return;

        var children = _world.Get<Children>(parent);
        children.Refs.RemoveAll(r => !r.IsValid || !_world.IsAlive(r.Entity) || r.Entity.Equals(child));
        _world.Set(parent, children);
    }
}
