using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Components;

public enum BodyType
{
    Static,
    Kinematic,
    Dynamic
}

[ComponentCategory("Physics")]
public struct BoxCollider
{
    public float Width;
    public float Height;
    public Vector2 Offset;
    public bool IsTrigger;
    public int CollisionLayer;
    public int CollisionMask;

    public BoxCollider()
    {
        Width = 32f;
        Height = 32f;
        Offset = Vector2.Zero;
        IsTrigger = false;
        CollisionLayer = 1;
        CollisionMask = 1;
    }
}

[ComponentCategory("Physics")]
public struct CircleCollider
{
    public float Radius;
    public Vector2 Offset;
    public bool IsTrigger;
    public int CollisionLayer;
    public int CollisionMask;

    public CircleCollider()
    {
        Radius = 16f;
        Offset = Vector2.Zero;
        IsTrigger = false;
        CollisionLayer = 1;
        CollisionMask = 1;
    }
}

[ComponentCategory("Physics")]
public struct RigidBody2D
{
    public float Mass;
    public float GravityScale;
    public float Damping;
    public bool IsKinematic;
    public bool FixedRotation;
    public BodyType BodyType;

    public RigidBody2D()
    {
        Mass = 1f;
        GravityScale = 1f;
        Damping = 0f;
        IsKinematic = false;
        FixedRotation = false;
        BodyType = BodyType.Dynamic;
    }
}
