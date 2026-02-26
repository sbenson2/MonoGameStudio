using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with Camera2D + Position, applies smooth follow toward a target entity,
/// enforces single active camera, and clamps to Camera2D.Limits.
/// </summary>
public class CameraSystem
{
    private readonly WorldManager _worldManager;

    // Cached active camera entity (Entity.Null if none found)
    private Entity _activeCamera;
    private bool _hasActiveCamera;

    public CameraSystem(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    /// <summary>
    /// The currently active camera entity. Check HasActiveCamera before using.
    /// </summary>
    public Entity ActiveCamera => _activeCamera;
    public bool HasActiveCamera => _hasActiveCamera;

    /// <summary>
    /// Gets the view matrix for the active camera (centered on viewport).
    /// Returns Matrix.Identity if no active camera.
    /// </summary>
    public Matrix GetViewMatrix(Vector2 viewportSize)
    {
        if (!_hasActiveCamera) return Matrix.Identity;

        var world = _worldManager.World;
        if (!world.IsAlive(_activeCamera)) { _hasActiveCamera = false; return Matrix.Identity; }

        var pos = world.Get<Position>(_activeCamera);
        var cam = world.Get<Camera2D>(_activeCamera);

        return Matrix.CreateTranslation(-pos.X, -pos.Y, 0f) *
               Matrix.CreateScale(cam.ZoomLevel, cam.ZoomLevel, 1f) *
               Matrix.CreateTranslation(viewportSize.X / 2f, viewportSize.Y / 2f, 0f);
    }

    /// <summary>
    /// Updates the camera system: finds the active camera, applies smooth follow
    /// toward a target entity (if provided), and clamps to limits.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    /// <param name="followTargetGuid">
    /// Optional GUID of the entity the camera should follow.
    /// Pass Guid.Empty to skip follow behavior.
    /// </param>
    public void Update(GameTime gameTime, Guid followTargetGuid = default)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var world = _worldManager.World;

        // Find the active camera (enforce single active)
        _hasActiveCamera = false;
        _activeCamera = Entity.Null;

        var query = new QueryDescription().WithAll<Camera2D, Position>();
        world.Query(query, (Entity entity, ref Camera2D cam, ref Position pos) =>
        {
            if (!cam.IsActive) return;

            // First active camera wins; deactivate any extras
            if (_hasActiveCamera)
            {
                cam.IsActive = false;
                return;
            }

            _hasActiveCamera = true;
            _activeCamera = entity;
        });

        if (!_hasActiveCamera) return;
        if (!world.IsAlive(_activeCamera)) { _hasActiveCamera = false; return; }

        ref var cameraPos = ref world.Get<Position>(_activeCamera);
        ref var camera = ref world.Get<Camera2D>(_activeCamera);

        // Follow target entity
        if (followTargetGuid != Guid.Empty)
        {
            Entity? targetEntity = FindEntityByGuid(followTargetGuid);
            if (targetEntity.HasValue && world.IsAlive(targetEntity.Value) &&
                world.Has<Position>(targetEntity.Value))
            {
                var targetPos = world.Get<Position>(targetEntity.Value);
                var targetVec = targetPos.ToVector2();
                var currentVec = cameraPos.ToVector2();

                // Exponential lerp for smooth follow
                // Smoothing == 0 means instant snap, higher values = smoother/slower
                if (camera.Smoothing > 0f)
                {
                    float lerpFactor = 1f - MathF.Exp(-camera.Smoothing * dt);
                    var newPos = Vector2.Lerp(currentVec, targetVec, lerpFactor);
                    cameraPos.X = newPos.X;
                    cameraPos.Y = newPos.Y;
                }
                else
                {
                    cameraPos.X = targetVec.X;
                    cameraPos.Y = targetVec.Y;
                }
            }
        }

        // Clamp to limits (if defined — non-empty rectangle)
        if (camera.Limits != Rectangle.Empty)
        {
            cameraPos.X = MathHelper.Clamp(cameraPos.X, camera.Limits.Left, camera.Limits.Right);
            cameraPos.Y = MathHelper.Clamp(cameraPos.Y, camera.Limits.Top, camera.Limits.Bottom);
        }

        // Clamp zoom to sane range
        camera.ZoomLevel = MathHelper.Clamp(camera.ZoomLevel, 0.1f, 10f);
    }

    /// <summary>
    /// Converts a screen position to world position using the active camera.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Vector2 viewportSize)
    {
        var invView = Matrix.Invert(GetViewMatrix(viewportSize));
        return Vector2.Transform(screenPos, invView);
    }

    /// <summary>
    /// Converts a world position to screen position using the active camera.
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPos, Vector2 viewportSize)
    {
        return Vector2.Transform(worldPos, GetViewMatrix(viewportSize));
    }

    private Entity? FindEntityByGuid(Guid guid)
    {
        var world = _worldManager.World;
        Entity? found = null;

        var query = new QueryDescription().WithAll<EntityGuid>();
        world.Query(query, (Entity entity, ref EntityGuid eg) =>
        {
            if (found.HasValue) return;
            if (eg.Id == guid)
                found = entity;
        });

        return found;
    }
}
