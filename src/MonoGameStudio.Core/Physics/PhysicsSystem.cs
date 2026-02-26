using Arch.Core;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Physics;

public class PhysicsSystem
{
    private readonly WorldManager _worldManager;
    private readonly PhysicsWorld2D _physicsWorld;
    private bool _initialized;

    public PhysicsSystem(WorldManager worldManager, PhysicsWorld2D physicsWorld)
    {
        _worldManager = worldManager;
        _physicsWorld = physicsWorld;
    }

    public void Initialize()
    {
        _physicsWorld.Initialize();

        // Query all entities with RigidBody2D + collider and create physics bodies
        var world = _worldManager.World;
        var query = new QueryDescription().WithAll<RigidBody2D, Position>();
        world.Query(query, (Entity entity, ref RigidBody2D rb, ref Position pos) =>
        {
            // Body creation will happen when Aether is integrated
        });

        _initialized = true;
    }

    public void Update(float deltaTime)
    {
        if (!_initialized) return;
        _physicsWorld.Step(deltaTime);
        _physicsWorld.SyncBodiesToECS();
    }

    public void Cleanup()
    {
        _physicsWorld.Clear();
        _initialized = false;
    }
}
