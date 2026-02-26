using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Particles;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

/// <summary>
/// Queries entities with ParticleEmitter + Position, manages ParticleEmitterRuntime
/// instances per entity, updates all active emitters, and draws particles via SpriteBatch.
/// </summary>
public class ParticleSystem
{
    private readonly WorldManager _worldManager;

    // Runtime state keyed by entity ID
    private readonly Dictionary<int, ParticleEmitterRuntime> _runtimes = new();

    // Cached preset data by path
    private readonly Dictionary<string, ParticlePreset> _presetCache = new();

    // 1x1 white pixel texture for rendering particles
    private Texture2D? _pixelTexture;

    // Draw calls separated by blend mode
    private readonly List<ParticleDrawCall> _alphaDrawCalls = new();
    private readonly List<ParticleDrawCall> _additiveDrawCalls = new();

    public ParticleSystem(WorldManager worldManager)
    {
        _worldManager = worldManager;
    }

    /// <summary>
    /// Initializes the system. Must be called once with the GraphicsDevice
    /// to create the 1x1 white pixel texture.
    /// </summary>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
    }

    /// <summary>
    /// Updates all particle emitters and their pools.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var world = _worldManager.World;

        // Track which entity IDs are still alive this frame
        var activeIds = new HashSet<int>();

        var query = new QueryDescription().WithAll<ParticleEmitter, Position>();
        world.Query(query, (Entity entity, ref ParticleEmitter emitter, ref Position pos) =>
        {
            int entityId = entity.Id;
            activeIds.Add(entityId);

            if (string.IsNullOrEmpty(emitter.PresetPath)) return;

            // Get or create runtime
            if (!_runtimes.TryGetValue(entityId, out var runtime))
            {
                var preset = GetPreset(emitter.PresetPath);
                if (preset == null) return;

                runtime = new ParticleEmitterRuntime(preset);
                _runtimes[entityId] = runtime;

                // Apply PlayOnStart
                if (emitter.PlayOnStart)
                    emitter.IsEmitting = true;
            }

            // Sync emitting state
            runtime.IsEmitting = emitter.IsEmitting;

            // Update the runtime
            runtime.Update(dt, pos.ToVector2());
        });

        // Clean up runtimes for destroyed entities
        var deadIds = new List<int>();
        foreach (var kvp in _runtimes)
        {
            if (!activeIds.Contains(kvp.Key))
                deadIds.Add(kvp.Key);
        }
        foreach (var id in deadIds)
            _runtimes.Remove(id);
    }

    /// <summary>
    /// Draws all alive particles. Call within an active SpriteBatch Begin/End
    /// or let this method manage its own batches for blend mode grouping.
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with.</param>
    /// <param name="viewMatrix">
    /// The camera view matrix. Pass Matrix.Identity if no camera transform is needed.
    /// </param>
    public void Draw(SpriteBatch spriteBatch, Matrix viewMatrix)
    {
        if (_pixelTexture == null) return;

        _alphaDrawCalls.Clear();
        _additiveDrawCalls.Clear();

        // Collect draw calls from all runtimes
        foreach (var kvp in _runtimes)
        {
            var runtime = kvp.Value;
            var blendMode = runtime.Preset.BlendMode;
            var particles = runtime.Pool.GetAliveParticles();

            for (int i = 0; i < particles.Length; i++)
            {
                ref readonly var p = ref particles[i];
                var call = new ParticleDrawCall
                {
                    Position = p.Position,
                    Scale = p.Scale,
                    Rotation = p.Rotation,
                    Color = p.Color
                };

                if (blendMode == ParticleBlendMode.Additive)
                    _additiveDrawCalls.Add(call);
                else
                    _alphaDrawCalls.Add(call);
            }
        }

        // Draw alpha-blended particles
        if (_alphaDrawCalls.Count > 0)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null, null, null,
                viewMatrix);

            DrawCalls(spriteBatch, _alphaDrawCalls);
            spriteBatch.End();
        }

        // Draw additive particles
        if (_additiveDrawCalls.Count > 0)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                null, null, null,
                viewMatrix);

            DrawCalls(spriteBatch, _additiveDrawCalls);
            spriteBatch.End();
        }
    }

    /// <summary>
    /// Clears all cached presets and runtimes.
    /// </summary>
    public void ClearCache()
    {
        _runtimes.Clear();
        _presetCache.Clear();
    }

    /// <summary>
    /// Invalidates and reloads the preset for a specific path.
    /// </summary>
    public void InvalidatePreset(string path)
    {
        _presetCache.Remove(path);
    }

    private void DrawCalls(SpriteBatch spriteBatch, List<ParticleDrawCall> calls)
    {
        var origin = new Vector2(0.5f, 0.5f); // Center of 1x1 pixel
        foreach (var call in calls)
        {
            spriteBatch.Draw(
                _pixelTexture!,
                call.Position,
                null,
                call.Color,
                call.Rotation,
                origin,
                new Vector2(call.Scale * 4f, call.Scale * 4f), // Scale up the 1x1 pixel
                SpriteEffects.None,
                0f);
        }
    }

    private ParticlePreset? GetPreset(string path)
    {
        if (_presetCache.TryGetValue(path, out var cached))
            return cached;

        var preset = ParticleSerializer.Load(path);
        if (preset != null)
            _presetCache[path] = preset;
        return preset;
    }

    private struct ParticleDrawCall
    {
        public Vector2 Position;
        public float Scale;
        public float Rotation;
        public Color Color;
    }
}
