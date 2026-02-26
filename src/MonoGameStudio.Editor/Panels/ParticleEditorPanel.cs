using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Particles;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;
using Vector2 = System.Numerics.Vector2;

namespace MonoGameStudio.Editor.Panels;

/// <summary>
/// Particle editor panel with live preview and a property inspector.
/// Allows editing ParticlePreset properties and saving/loading .particle.json files.
/// </summary>
public class ParticleEditorPanel
{
    private ParticlePreset? _preset;
    private string? _currentPath;

    // Live preview runtime
    private ParticleEmitterRuntime? _previewRuntime;
    private RenderTarget2D? _previewTarget;
    private SpriteBatch? _previewSpriteBatch;
    private Texture2D? _pixelTexture;
    private ImTextureRef? _previewTextureRef;
    private ImGuiManager? _imGui;
    private GraphicsDevice? _graphicsDevice;

    // Preview state
    private bool _isPreviewPlaying = true;

    /// <summary>
    /// Panel title constant for layout registration.
    /// </summary>
    public const string PanelTitle = "Particle Editor";

    public void Initialize(GraphicsDevice graphicsDevice, ImGuiManager imGui)
    {
        _graphicsDevice = graphicsDevice;
        _imGui = imGui;

        _previewSpriteBatch = new SpriteBatch(graphicsDevice);
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Microsoft.Xna.Framework.Color.White]);

        // Create preview render target
        _previewTarget = new RenderTarget2D(graphicsDevice, 400, 300);
        _previewTextureRef = imGui.BindTexture(_previewTarget);
    }

    public void OpenPreset(string path)
    {
        _preset = ParticleSerializer.Load(path);
        _currentPath = path;
        RebuildPreviewRuntime();
        Log.Info($"Opened particle preset: {path}");
    }

    public void CreateNew(string savePath)
    {
        _preset = new ParticlePreset();
        _currentPath = savePath;
        RebuildPreviewRuntime();
    }

    public void UpdatePreview(float deltaTime)
    {
        if (!_isPreviewPlaying || _previewRuntime == null) return;
        if (_graphicsDevice == null || _previewSpriteBatch == null) return;
        if (_previewTarget == null || _pixelTexture == null) return;

        // Update runtime at center of preview
        var center = new Microsoft.Xna.Framework.Vector2(
            _previewTarget.Width / 2f,
            _previewTarget.Height / 2f);
        _previewRuntime.Update(deltaTime, center);

        // Render to target
        _graphicsDevice.SetRenderTarget(_previewTarget);
        _graphicsDevice.Clear(new Microsoft.Xna.Framework.Color(30, 30, 30));

        var blendMode = _preset?.BlendMode ?? ParticleBlendMode.Alpha;
        var blendState = blendMode == ParticleBlendMode.Additive
            ? BlendState.Additive
            : BlendState.AlphaBlend;

        _previewSpriteBatch.Begin(
            SpriteSortMode.Deferred,
            blendState,
            SamplerState.PointClamp);

        var particles = _previewRuntime.Pool.GetAliveParticles();
        var origin = new Microsoft.Xna.Framework.Vector2(0.5f, 0.5f);
        for (int i = 0; i < particles.Length; i++)
        {
            ref readonly var p = ref particles[i];
            _previewSpriteBatch.Draw(
                _pixelTexture,
                p.Position,
                null,
                p.Color,
                p.Rotation,
                origin,
                new Microsoft.Xna.Framework.Vector2(p.Scale * 4f, p.Scale * 4f),
                SpriteEffects.None,
                0f);
        }

        _previewSpriteBatch.End();
        _graphicsDevice.SetRenderTarget(null);
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(PanelTitle, ref isOpen))
        {
            if (_preset == null)
            {
                ImGui.TextDisabled("No particle preset open.");
                ImGui.TextDisabled("Create one from the Asset Browser.");
            }
            else
            {
                DrawToolbar();
                DrawContent();
            }
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        // Playback controls
        if (_isPreviewPlaying)
        {
            if (ImGui.Button($"{FontAwesomeIcons.Pause}##particle_pause"))
                _isPreviewPlaying = false;
        }
        else
        {
            if (ImGui.Button($"{FontAwesomeIcons.Play}##particle_play"))
                _isPreviewPlaying = true;
        }
        ImGui.SameLine();

        if (ImGui.Button($"{FontAwesomeIcons.Redo}##particle_restart"))
        {
            _previewRuntime?.Reset();
            _isPreviewPlaying = true;
        }
        ImGui.SameLine();

        // Alive count
        int alive = _previewRuntime?.Pool.AliveCount ?? 0;
        ImGui.Text($"Particles: {alive}");

        ImGui.SameLine();
        float rightEdge = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rightEdge - 60);

        if (ImGui.Button("Save"))
            Save();

        ImGui.Separator();
    }

    private void DrawContent()
    {
        float availWidth = ImGui.GetContentRegionAvail().X;

        // Left: live preview
        float previewWidth = availWidth * 0.45f;
        if (ImGui.BeginChild("ParticlePreview", new Vector2(previewWidth, 0), ImGuiChildFlags.Borders))
        {
            DrawPreview();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Right: property inspector
        if (ImGui.BeginChild("ParticleProperties", Vector2.Zero, ImGuiChildFlags.Borders))
        {
            DrawPropertyInspector();
        }
        ImGui.EndChild();
    }

    private void DrawPreview()
    {
        if (_previewTextureRef == null || _previewTarget == null) return;

        var availSize = ImGui.GetContentRegionAvail();
        // OpenGL Y-flip: uv0=(0,1), uv1=(1,0)
        ImGui.Image(
            _previewTextureRef.Value,
            new Vector2(availSize.X, Math.Max(availSize.Y - 4, 100)),
            new Vector2(0, 1), new Vector2(1, 0));
    }

    private void DrawPropertyInspector()
    {
        if (_preset == null) return;

        // --- Emission ---
        if (ImGui.CollapsingHeader("Emission", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.DragFloat("Emission Rate", ref _preset.EmissionRate, 1f, 0f, 10000f);
            ImGui.DragInt("Max Particles", ref _preset.MaxParticles, 1f, 1, 100000);

            int emissionMode = (int)_preset.EmissionMode;
            if (ImGui.Combo("Emission Mode", ref emissionMode, "Stream\0Burst\0"))
            {
                _preset.EmissionMode = (EmissionMode)emissionMode;
                RebuildPreviewRuntime();
            }
        }

        // --- Shape ---
        if (ImGui.CollapsingHeader("Shape", ImGuiTreeNodeFlags.DefaultOpen))
        {
            int shape = (int)_preset.EmissionShape;
            if (ImGui.Combo("Emission Shape", ref shape, "Point\0Circle\0Rectangle\0Edge\0"))
            {
                _preset.EmissionShape = (EmissionShape)shape;
            }

            if (_preset.EmissionShape != EmissionShape.Point)
            {
                ImGui.DragFloat("Shape Width", ref _preset.ShapeWidth, 1f, 0f, 2000f);
                if (_preset.EmissionShape is EmissionShape.Rectangle or EmissionShape.Edge)
                    ImGui.DragFloat("Shape Height", ref _preset.ShapeHeight, 1f, 0f, 2000f);
            }
        }

        // --- Lifetime ---
        if (ImGui.CollapsingHeader("Lifetime", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.DragFloat("Lifetime Min", ref _preset.LifetimeMin, 0.01f, 0.01f, 60f);
            ImGui.DragFloat("Lifetime Max", ref _preset.LifetimeMax, 0.01f, 0.01f, 60f);
            _preset.LifetimeMax = Math.Max(_preset.LifetimeMax, _preset.LifetimeMin);
        }

        // --- Velocity ---
        if (ImGui.CollapsingHeader("Velocity", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.DragFloat("Speed Min", ref _preset.SpeedMin, 1f, 0f, 5000f);
            ImGui.DragFloat("Speed Max", ref _preset.SpeedMax, 1f, 0f, 5000f);
            _preset.SpeedMax = Math.Max(_preset.SpeedMax, _preset.SpeedMin);

            ImGui.DragFloat("Angle Min", ref _preset.AngleMin, 1f, 0f, 360f);
            ImGui.DragFloat("Angle Max", ref _preset.AngleMax, 1f, 0f, 360f);
        }

        // --- Forces ---
        if (ImGui.CollapsingHeader("Forces"))
        {
            ImGui.DragFloat("Gravity X", ref _preset.GravityX, 1f, -2000f, 2000f);
            ImGui.DragFloat("Gravity Y", ref _preset.GravityY, 1f, -2000f, 2000f);
        }

        // --- Scale ---
        if (ImGui.CollapsingHeader("Scale", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.DragFloat("Scale Start", ref _preset.ScaleStart, 0.1f, 0.01f, 50f);
            ImGui.DragFloat("Scale End", ref _preset.ScaleEnd, 0.1f, 0f, 50f);
        }

        // --- Color ---
        if (ImGui.CollapsingHeader("Color", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var startColor = new System.Numerics.Vector4(
                _preset.StartColor[0], _preset.StartColor[1],
                _preset.StartColor[2], _preset.StartColor[3]);
            if (ImGui.ColorEdit4("Start Color", ref startColor))
            {
                _preset.StartColor[0] = startColor.X;
                _preset.StartColor[1] = startColor.Y;
                _preset.StartColor[2] = startColor.Z;
                _preset.StartColor[3] = startColor.W;
            }

            var endColor = new System.Numerics.Vector4(
                _preset.EndColor[0], _preset.EndColor[1],
                _preset.EndColor[2], _preset.EndColor[3]);
            if (ImGui.ColorEdit4("End Color", ref endColor))
            {
                _preset.EndColor[0] = endColor.X;
                _preset.EndColor[1] = endColor.Y;
                _preset.EndColor[2] = endColor.Z;
                _preset.EndColor[3] = endColor.W;
            }
        }

        // --- Rendering ---
        if (ImGui.CollapsingHeader("Rendering"))
        {
            int blendMode = (int)_preset.BlendMode;
            if (ImGui.Combo("Blend Mode", ref blendMode, "Alpha\0Additive\0"))
            {
                _preset.BlendMode = (ParticleBlendMode)blendMode;
            }
        }

        // Apply changes button (rebuilds preview runtime with new MaxParticles, etc.)
        ImGui.Separator();
        if (ImGui.Button("Apply & Restart Preview"))
        {
            RebuildPreviewRuntime();
        }
    }

    private void RebuildPreviewRuntime()
    {
        if (_preset == null) return;
        _previewRuntime = new ParticleEmitterRuntime(_preset);
        _isPreviewPlaying = true;
    }

    private void Save()
    {
        if (_preset == null || _currentPath == null) return;
        ParticleSerializer.Save(_currentPath, _preset);
        Log.Info($"Particle preset saved: {_currentPath}");
    }

    public void Dispose()
    {
        if (_previewTextureRef.HasValue && _imGui != null)
        {
            _imGui.UnbindTexture(_previewTextureRef.Value);
            _previewTextureRef = null;
        }
        _previewTarget?.Dispose();
        _previewSpriteBatch?.Dispose();
        _pixelTexture?.Dispose();
    }
}
