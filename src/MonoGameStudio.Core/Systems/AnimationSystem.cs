using Arch.Core;
using Microsoft.Xna.Framework;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with Animator + SpriteRenderer, advances frame timer, updates SourceRect.
/// </summary>
public class AnimationSystem
{
    private readonly WorldManager _worldManager;

    // Cached animation data by path
    private readonly Dictionary<string, AnimationDocument> _animationCache = new();
    private readonly Dictionary<string, SpriteSheetDocument> _spriteSheetCache = new();

    // Per-entity animation state (keyed by entity id)
    private readonly Dictionary<int, AnimationState> _states = new();

    public AnimationSystem(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var world = _worldManager.World;

        var query = new QueryDescription().WithAll<Animator, SpriteRenderer>();
        world.Query(query, (Entity entity, ref Animator animator, ref SpriteRenderer sprite) =>
        {
            if (!animator.IsPlaying) return;
            if (string.IsNullOrEmpty(animator.AnimationDataPath)) return;

            var animDoc = GetAnimation(animator.AnimationDataPath);
            if (animDoc == null) return;

            // Copy ref values to locals (can't capture ref params in lambdas)
            var currentState = animator.CurrentState;

            // Find current clip
            AnimationClip? clip = null;
            for (int i = 0; i < animDoc.Clips.Count; i++)
            {
                if (animDoc.Clips[i].Name == currentState) { clip = animDoc.Clips[i]; break; }
            }
            if (clip == null && animDoc.Clips.Count > 0)
            {
                clip = animDoc.Clips[0];
                animator.CurrentState = clip.Name;
            }
            if (clip == null || clip.Frames.Count == 0) return;

            // Get sprite sheet for frame rects
            var sheet = GetSpriteSheet(animDoc.SpriteSheetPath);
            if (sheet == null) return;

            // Get or create animation state
            int entityId = entity.Id;
            if (!_states.TryGetValue(entityId, out var state))
            {
                state = new AnimationState();
                _states[entityId] = state;
            }

            // Advance timer
            state.Timer += dt * animator.Speed * clip.Speed;
            float frameDuration = clip.Frames[state.FrameIndex].Duration;

            if (state.Timer >= frameDuration)
            {
                state.Timer -= frameDuration;
                state.FrameIndex++;

                if (state.FrameIndex >= clip.Frames.Count)
                {
                    if (clip.Loop)
                        state.FrameIndex = 0;
                    else
                    {
                        state.FrameIndex = clip.Frames.Count - 1;
                        animator.IsPlaying = false;
                    }
                }
            }

            // Apply frame to SpriteRenderer
            var frameRef = clip.Frames[state.FrameIndex];
            SpriteFrame? spriteFrame = null;
            for (int i = 0; i < sheet.Frames.Count; i++)
            {
                if (sheet.Frames[i].Name == frameRef.FrameName) { spriteFrame = sheet.Frames[i]; break; }
            }
            if (spriteFrame != null)
            {
                sprite.SourceRect = spriteFrame.ToRectangle();
                sprite.Origin = new Vector2(
                    spriteFrame.PivotX * spriteFrame.Width,
                    spriteFrame.PivotY * spriteFrame.Height);
            }
        });
    }

    public void ClearCache()
    {
        _animationCache.Clear();
        _spriteSheetCache.Clear();
        _states.Clear();
    }

    private AnimationDocument? GetAnimation(string path)
    {
        if (_animationCache.TryGetValue(path, out var cached))
            return cached;

        var doc = AnimationSerializer.Load(path);
        if (doc != null)
            _animationCache[path] = doc;
        return doc;
    }

    private SpriteSheetDocument? GetSpriteSheet(string path)
    {
        if (_spriteSheetCache.TryGetValue(path, out var cached))
            return cached;

        var doc = SpriteSheetSerializer.Load(path);
        if (doc != null)
            _spriteSheetCache[path] = doc;
        return doc;
    }

    private class AnimationState
    {
        public int FrameIndex;
        public float Timer;
    }
}
