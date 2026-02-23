using Arch.Core;

namespace MonoGameStudio.Core.Components;

/// <summary>
/// Lightweight wrapper around Entity for safe storage as a component.
/// Always check IsAlive via World before accessing.
/// </summary>
public struct EntityRef
{
    public Entity Entity;
    public bool IsValid => Entity != Entity.Null;

    public EntityRef(Entity entity) { Entity = entity; }
    public static EntityRef Null => new(Entity.Null);
}

public struct Parent
{
    public EntityRef Ref;

    public Parent(EntityRef entityRef) { Ref = entityRef; }
    public Parent(Entity entity) { Ref = new EntityRef(entity); }
}

public struct Children
{
    public List<EntityRef> Refs;

    public Children() { Refs = new List<EntityRef>(); }
}
