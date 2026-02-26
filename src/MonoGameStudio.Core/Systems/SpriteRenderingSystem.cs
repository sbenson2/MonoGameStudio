using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with SpriteRenderer + Position, sorts by SortOrder, draws via SpriteBatch.
/// </summary>
public class SpriteRenderingSystem
{
    private readonly WorldManager _worldManager;
    private readonly TextureCache _textureCache;
    private readonly List<SpriteDrawCall> _drawCalls = new();

    public SpriteRenderingSystem(WorldManager worldManager, TextureCache textureCache)
    {
        _worldManager = worldManager;
        _textureCache = textureCache;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _drawCalls.Clear();
        var world = _worldManager.World;

        var query = new QueryDescription().WithAll<SpriteRenderer, Position>();
        world.Query(query, (Entity entity, ref SpriteRenderer sprite, ref Position pos) =>
        {
            if (string.IsNullOrEmpty(sprite.TexturePath)) return;

            var texture = _textureCache.Get(sprite.TexturePath);
            if (texture == null) return;

            float rotation = 0f;
            if (world.Has<Rotation>(entity))
                rotation = world.Get<Rotation>(entity).Angle;

            var scale = Vector2.One;
            if (world.Has<Scale>(entity))
            {
                var s = world.Get<Scale>(entity);
                scale = new Vector2(s.X, s.Y);
            }

            _drawCalls.Add(new SpriteDrawCall
            {
                Texture = texture,
                Position = new Vector2(pos.X, pos.Y),
                SourceRect = sprite.SourceRect == Rectangle.Empty ? null : sprite.SourceRect,
                Tint = sprite.Tint * sprite.Opacity,
                Rotation = rotation,
                Origin = sprite.Origin,
                Scale = scale,
                SortOrder = sprite.SortOrder,
                Effects = GetSpriteEffects(sprite.FlipX, sprite.FlipY)
            });
        });

        // Sort by SortOrder (lower draws first / behind)
        _drawCalls.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

        foreach (var call in _drawCalls)
        {
            spriteBatch.Draw(
                call.Texture,
                call.Position,
                call.SourceRect,
                call.Tint,
                call.Rotation,
                call.Origin,
                call.Scale,
                call.Effects,
                0f);
        }
    }

    private static SpriteEffects GetSpriteEffects(bool flipX, bool flipY)
    {
        var effects = SpriteEffects.None;
        if (flipX) effects |= SpriteEffects.FlipHorizontally;
        if (flipY) effects |= SpriteEffects.FlipVertically;
        return effects;
    }

    private struct SpriteDrawCall
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Rectangle? SourceRect;
        public Color Tint;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 Scale;
        public int SortOrder;
        public SpriteEffects Effects;
    }
}
