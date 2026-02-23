using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Components;

public struct Position
{
    public float X;
    public float Y;

    public Position(float x, float y) { X = x; Y = y; }

    public Vector2 ToVector2() => new(X, Y);
    public static Position FromVector2(Vector2 v) => new(v.X, v.Y);
}

public struct Rotation
{
    public float Angle; // radians

    public Rotation(float angle) { Angle = angle; }
}

public struct Scale
{
    public float X;
    public float Y;

    public Scale(float x, float y) { X = x; Y = y; }

    public static Scale One => new(1f, 1f);
    public Vector2 ToVector2() => new(X, Y);
    public static Scale FromVector2(Vector2 v) => new(v.X, v.Y);
}

public struct LocalTransform
{
    public Matrix WorldMatrix;

    public static LocalTransform Identity => new() { WorldMatrix = Matrix.Identity };
}

public struct WorldTransform
{
    public Matrix WorldMatrix;

    public static WorldTransform Identity => new() { WorldMatrix = Matrix.Identity };
}
