using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;

namespace MonoGameStudio.Core.Systems;

public class TransformPropagationSystem
{
    private readonly World.WorldManager _worldManager;

    public TransformPropagationSystem(World.WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    public void Update()
    {
        var world = _worldManager.World;

        // Process root entities first (no Parent component)
        var roots = _worldManager.GetRootEntities();
        foreach (var root in roots)
        {
            UpdateTransform(world, root, Matrix.Identity);
        }
    }

    private void UpdateTransform(Arch.Core.World world, Entity entity, Matrix parentWorld)
    {
        if (!world.IsAlive(entity)) return;

        var pos = world.Get<Position>(entity);
        var rot = world.Get<Rotation>(entity);
        var scale = world.Get<Scale>(entity);

        var local = Matrix.CreateScale(scale.X, scale.Y, 1f) *
                    Matrix.CreateRotationZ(rot.Angle) *
                    Matrix.CreateTranslation(pos.X, pos.Y, 0f);

        var worldMatrix = local * parentWorld;

        world.Set(entity, new LocalTransform { WorldMatrix = local });
        world.Set(entity, new WorldTransform { WorldMatrix = worldMatrix });

        // Process children
        var children = _worldManager.GetChildren(entity);
        foreach (var child in children)
        {
            UpdateTransform(world, child, worldMatrix);
        }
    }
}
