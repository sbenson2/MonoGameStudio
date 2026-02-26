using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Physics;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Gizmos;

public class PhysicsDebugOverlay
{
    private readonly PhysicsWorld2D _physicsWorld;
    private readonly GizmoRenderer _renderer;
    private readonly EditorCamera _camera;
    private bool _enabled;

    public bool Enabled { get => _enabled; set => _enabled = value; }

    public PhysicsDebugOverlay(PhysicsWorld2D physicsWorld, GizmoRenderer renderer, EditorCamera camera)
    {
        _physicsWorld = physicsWorld;
        _renderer = renderer;
        _camera = camera;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 viewportSize)
    {
        if (!_enabled) return;
        // Will draw Aether world contacts, body AABBs, and velocity arrows when physics is integrated
        // For now, this is a placeholder that will be wired up with Aether.Physics2D
    }
}
