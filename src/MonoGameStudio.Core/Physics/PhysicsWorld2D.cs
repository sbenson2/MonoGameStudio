using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Physics;

public class PhysicsWorld2D
{
    private readonly WorldManager _worldManager;
    private readonly Dictionary<int, object> _entityBodies = new(); // Entity ID -> Body (typed later when Aether added)

    public float PixelsPerMeter { get; set; } = 64f;
    public Vector2 Gravity { get; set; } = new(0, 9.81f);

    // Aether world will be initialized when package is added
    private object? _physicsWorld;

    public PhysicsWorld2D(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    public void Initialize()
    {
        _entityBodies.Clear();
        Log.Info("Physics world initialized");
    }

    public void Step(float deltaTime)
    {
        // Will call Aether World.Step() when package is added
    }

    public void SyncBodiesToECS()
    {
        // Will sync Aether body positions back to ECS Position components
    }

    public void Clear()
    {
        _entityBodies.Clear();
        _physicsWorld = null;
    }
}
