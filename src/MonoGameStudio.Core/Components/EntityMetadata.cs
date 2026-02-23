namespace MonoGameStudio.Core.Components;

public struct EntityName
{
    public string Name;

    public EntityName(string name) { Name = name; }
}

public struct EntityGuid
{
    public Guid Id;

    public EntityGuid(Guid id) { Id = id; }
    public static EntityGuid New() => new(Guid.NewGuid());
}
