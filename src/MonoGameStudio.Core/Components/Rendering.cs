using Microsoft.Xna.Framework;

namespace MonoGameStudio.Core.Components;

[ComponentCategory("Rendering")]
public struct SpriteRenderer
{
    public string TexturePath;
    public Color Tint;
    public Rectangle SourceRect;
    public Vector2 Origin;
    public int SortOrder;
    public bool FlipX;
    public bool FlipY;
    public float Opacity;

    public SpriteRenderer()
    {
        TexturePath = "";
        Tint = Color.White;
        SourceRect = Rectangle.Empty;
        Origin = Vector2.Zero;
        SortOrder = 0;
        FlipX = false;
        FlipY = false;
        Opacity = 1f;
    }
}

[ComponentCategory("Rendering")]
public struct Animator
{
    public string AnimationDataPath;
    public string CurrentState;
    public float Speed;
    public bool IsPlaying;

    public Animator()
    {
        AnimationDataPath = "";
        CurrentState = "";
        Speed = 1f;
        IsPlaying = false;
    }
}

[ComponentCategory("Rendering")]
public struct Camera2D
{
    public float ZoomLevel;
    public bool IsActive;
    public Rectangle Limits;
    public float Smoothing;

    public Camera2D()
    {
        ZoomLevel = 1f;
        IsActive = false;
        Limits = Rectangle.Empty;
        Smoothing = 0f;
    }
}

[ComponentCategory("Rendering")]
public struct TilemapRenderer
{
    public string TilemapDataPath;
    public int SortOrder;

    public TilemapRenderer()
    {
        TilemapDataPath = "";
        SortOrder = 0;
    }
}
