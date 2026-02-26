using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with SpriteRenderer + Position, sorts by (SortLayer priority, SortOrder),
/// groups draws by SamplerState from import settings, draws via SpriteBatch.
/// </summary>
public class SpriteRenderingSystem
{
    private readonly WorldManager _worldManager;
    private readonly TextureCache _textureCache;
    private readonly List<SpriteDrawCall> _drawCalls = new();

    // Render layer config (set externally)
    private RenderLayerConfig? _renderLayerConfig;

    public void SetRenderLayerConfig(RenderLayerConfig? config) => _renderLayerConfig = config;

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

            // Get import settings for SamplerState
            var importSettings = _textureCache.GetImportSettings(sprite.TexturePath);
            var samplerState = ResolveSamplerState(importSettings);

            // Get layer priority
            int layerPriority = 0;
            if (_renderLayerConfig != null && !string.IsNullOrEmpty(sprite.SortLayer))
            {
                layerPriority = _renderLayerConfig.GetPriority(sprite.SortLayer);
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
                LayerPriority = layerPriority,
                Effects = GetSpriteEffects(sprite.FlipX, sprite.FlipY),
                SamplerState = samplerState
            });
        });

        // Sort by (LayerPriority, SortOrder)
        _drawCalls.Sort((a, b) =>
        {
            int cmp = a.LayerPriority.CompareTo(b.LayerPriority);
            return cmp != 0 ? cmp : a.SortOrder.CompareTo(b.SortOrder);
        });

        // Group draws by SamplerState — each group needs its own Begin/End
        // But since we're called within an existing Begin/End, we draw directly
        // and rely on the caller to manage batch state. For now, draw all calls.
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

    private static SamplerState ResolveSamplerState(TextureImportSettings settings)
    {
        return (settings.FilterMode, settings.WrapMode) switch
        {
            (SpriteFilterMode.Point, TextureWrapMode.Clamp) => SamplerState.PointClamp,
            (SpriteFilterMode.Point, TextureWrapMode.Wrap) => SamplerState.PointWrap,
            (SpriteFilterMode.Linear, TextureWrapMode.Clamp) => SamplerState.LinearClamp,
            (SpriteFilterMode.Linear, TextureWrapMode.Wrap) => SamplerState.LinearWrap,
            _ => SamplerState.PointClamp
        };
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
        public int LayerPriority;
        public SpriteEffects Effects;
        public SamplerState SamplerState;
    }
}
