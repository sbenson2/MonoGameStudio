using Arch.Core;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Editor.Commands;

public class AddComponentCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private readonly Entity _entity;
    private readonly IComponentDescriptor _descriptor;

    public string Description => $"Add {_descriptor.Name}";

    public AddComponentCommand(WorldManager worldManager, Entity entity, IComponentDescriptor descriptor)
    {
        _worldManager = worldManager;
        _entity = entity;
        _descriptor = descriptor;
    }

    public void Execute()
    {
        if (!_worldManager.World.IsAlive(_entity)) return;
        _descriptor.Add(_worldManager.World, _entity);
    }

    public void Undo()
    {
        if (!_worldManager.World.IsAlive(_entity)) return;
        _descriptor.Remove(_worldManager.World, _entity);
    }
}

public class RemoveComponentCommand : ICommand
{
    private readonly WorldManager _worldManager;
    private readonly Entity _entity;
    private readonly IComponentDescriptor _descriptor;
    private object? _snapshot;

    public string Description => $"Remove {_descriptor.Name}";

    public RemoveComponentCommand(WorldManager worldManager, Entity entity, IComponentDescriptor descriptor)
    {
        _worldManager = worldManager;
        _entity = entity;
        _descriptor = descriptor;

        // Snapshot current value for undo
        if (_worldManager.World.IsAlive(_entity) && _descriptor.Has(_worldManager.World, _entity))
            _snapshot = _descriptor.Get(_worldManager.World, _entity);
    }

    public void Execute()
    {
        if (!_worldManager.World.IsAlive(_entity)) return;
        _descriptor.Remove(_worldManager.World, _entity);
    }

    public void Undo()
    {
        if (!_worldManager.World.IsAlive(_entity)) return;
        if (_snapshot != null)
            _descriptor.Add(_worldManager.World, _entity, _snapshot);
        else
            _descriptor.Add(_worldManager.World, _entity);
    }
}
