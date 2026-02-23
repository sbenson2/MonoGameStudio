using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Editor.Commands;

public class MoveEntityCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private readonly Entity _entity;
    private readonly Vector2 _oldPosition;
    private readonly Vector2 _newPosition;

    public string Description => $"Move entity";

    public MoveEntityCommand(WorldManager worldManager, Entity entity, Vector2 oldPos, Vector2 newPos)
    {
        _worldManager = worldManager;
        _entity = entity;
        _oldPosition = oldPos;
        _newPosition = newPos;
    }

    public void Execute()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.World.Set(_entity, new Position(_newPosition.X, _newPosition.Y));
    }

    public void Undo()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.World.Set(_entity, new Position(_oldPosition.X, _oldPosition.Y));
    }
}

public class CreateEntityCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private Entity _entity;
    private readonly string _name;
    private readonly Entity? _parent;

    public string Description => $"Create entity '{_name}'";
    public Entity CreatedEntity => _entity;

    public CreateEntityCommand(WorldManager worldManager, string name = "Entity", Entity? parent = null)
    {
        _worldManager = worldManager;
        _name = name;
        _parent = parent;
    }

    public void Execute()
    {
        _entity = _worldManager.CreateEntity(_name, _parent);
    }

    public void Undo()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.DestroyEntity(_entity);
    }
}

public class DeleteEntityCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private readonly Entity _entity;
    private readonly string _name;
    private readonly Vector2 _position;
    private readonly float _rotation;
    private readonly Vector2 _scale;
    private readonly Guid _guid;

    public string Description => $"Delete entity '{_name}'";

    public DeleteEntityCommand(WorldManager worldManager, Entity entity)
    {
        _worldManager = worldManager;
        _entity = entity;

        var world = worldManager.World;
        _name = world.Get<EntityName>(entity).Name;
        var pos = world.Get<Position>(entity);
        _position = new Vector2(pos.X, pos.Y);
        _rotation = world.Get<Rotation>(entity).Angle;
        var s = world.Get<Scale>(entity);
        _scale = new Vector2(s.X, s.Y);
        _guid = world.Get<EntityGuid>(entity).Id;
    }

    public void Execute()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.DestroyEntity(_entity);
    }

    public void Undo()
    {
        // Re-create (won't have same Entity struct but restores data)
        var entity = _worldManager.CreateEntity(_name);
        _worldManager.World.Set(entity, new Position(_position.X, _position.Y));
        _worldManager.World.Set(entity, new Rotation(_rotation));
        _worldManager.World.Set(entity, new Scale(_scale.X, _scale.Y));
        _worldManager.World.Set(entity, new EntityGuid(_guid));
    }
}

public class RenameEntityCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private readonly Entity _entity;
    private readonly string _oldName;
    private readonly string _newName;

    public string Description => $"Rename entity to '{_newName}'";

    public RenameEntityCommand(WorldManager worldManager, Entity entity, string oldName, string newName)
    {
        _worldManager = worldManager;
        _entity = entity;
        _oldName = oldName;
        _newName = newName;
    }

    public void Execute()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.RenameEntity(_entity, _newName);
    }

    public void Undo()
    {
        if (_worldManager.World.IsAlive(_entity))
            _worldManager.RenameEntity(_entity, _oldName);
    }
}

public class ModifyComponentCommand<T> : ICommand where T : struct
{
    private readonly Arch.Core.World _world;
    private readonly Entity _entity;
    private readonly T _oldValue;
    private readonly T _newValue;
    private readonly string _componentName;

    public string Description => $"Modify {_componentName}";

    public ModifyComponentCommand(Arch.Core.World world, Entity entity, T oldValue, T newValue)
    {
        _world = world;
        _entity = entity;
        _oldValue = oldValue;
        _newValue = newValue;
        _componentName = typeof(T).Name;
    }

    public void Execute()
    {
        if (_world.IsAlive(_entity))
            _world.Set(_entity, _newValue);
    }

    public void Undo()
    {
        if (_world.IsAlive(_entity))
            _world.Set(_entity, _oldValue);
    }
}
