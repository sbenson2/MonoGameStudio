using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Assets;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Editor.ImGuiIntegration;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

/// <summary>
/// Shader/effect preview panel. Allows selecting an effect file, inspecting its parameters
/// with typed ImGui widgets, and previewing the effect applied to a test sprite.
/// </summary>
public class ShaderPreviewPanel
{
    private EffectCache? _effectCache;
    private TextureCache? _textureCache;
    private ImGuiManager? _imGui;
    private GraphicsDevice? _graphicsDevice;

    private string _effectPath = "";
    private Effect? _currentEffect;
    private string _previewTexturePath = "";
    private Microsoft.Xna.Framework.Graphics.Texture2D? _previewTexture;
    private ImTextureRef? _previewTextureRef;

    // Cached parameter values for editing
    private readonly Dictionary<string, float> _floatParams = new();
    private readonly Dictionary<string, Vector2> _vec2Params = new();
    private readonly Dictionary<string, Vector3> _vec3Params = new();
    private readonly Dictionary<string, Vector4> _vec4Params = new();
    private readonly Dictionary<string, bool> _boolParams = new();

    public void Initialize(EffectCache effectCache, TextureCache textureCache,
        ImGuiManager imGui, GraphicsDevice graphicsDevice)
    {
        _effectCache = effectCache;
        _textureCache = textureCache;
        _imGui = imGui;
        _graphicsDevice = graphicsDevice;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.ShaderPreview, ref isOpen))
        {
            DrawEffectSelector();
            ImGui.Separator();

            if (_currentEffect != null)
            {
                DrawParameterInspector();
                ImGui.Separator();
                DrawPreviewArea();
            }
            else
            {
                ImGui.TextDisabled("No effect loaded. Enter a path to a compiled .mgfx or .xnb effect file.");
            }
        }
        ImGui.End();
    }

    private void DrawEffectSelector()
    {
        ImGui.Text("Effect File:");
        ImGui.SameLine();

        bool pathChanged = ImGui.InputText("##effectPath", ref _effectPath, 512,
            ImGuiInputTextFlags.EnterReturnsTrue);

        // Accept drag-drop from asset browser
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ASSET_PATH");
            unsafe
            {
                if (payload.Handle != null && payload.Data != null)
                {
                    var droppedPath = System.Text.Encoding.UTF8.GetString(
                        (byte*)payload.Data, (int)payload.DataSize).TrimEnd('\0');
                    _effectPath = droppedPath;
                    pathChanged = true;
                }
            }
            ImGui.EndDragDropTarget();
        }

        ImGui.SameLine();
        if (ImGui.Button("Load") || pathChanged)
            LoadEffect();

        // Preview texture selector
        ImGui.Text("Preview Texture:");
        ImGui.SameLine();
        if (ImGui.InputText("##previewTex", ref _previewTexturePath, 512,
            ImGuiInputTextFlags.EnterReturnsTrue))
        {
            LoadPreviewTexture();
        }
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ASSET_PATH");
            unsafe
            {
                if (payload.Handle != null && payload.Data != null)
                {
                    var droppedPath = System.Text.Encoding.UTF8.GetString(
                        (byte*)payload.Data, (int)payload.DataSize).TrimEnd('\0');
                    _previewTexturePath = droppedPath;
                    LoadPreviewTexture();
                }
            }
            ImGui.EndDragDropTarget();
        }
    }

    private void DrawParameterInspector()
    {
        if (_currentEffect == null) return;

        ImGui.Text("Parameters:");
        if (ImGui.BeginChild("##shaderParams", new Vector2(0, 200), ImGuiChildFlags.Borders))
        {
            foreach (var param in _currentEffect.Parameters)
            {
                var name = param.Name;
                DrawEffectParameter(param, name);
            }
        }
        ImGui.EndChild();
    }

    private void DrawEffectParameter(EffectParameter param, string name)
    {
        switch (param.ParameterType)
        {
            case EffectParameterType.Single:
            {
                if (!_floatParams.ContainsKey(name))
                    _floatParams[name] = param.GetValueSingle();

                var val = _floatParams[name];
                if (ImGui.SliderFloat(name, ref val, 0f, 10f))
                {
                    _floatParams[name] = val;
                    param.SetValue(val);
                }
                break;
            }

            case EffectParameterType.Bool:
            {
                if (!_boolParams.ContainsKey(name))
                    _boolParams[name] = param.GetValueBoolean();

                var val = _boolParams[name];
                if (ImGui.Checkbox(name, ref val))
                {
                    _boolParams[name] = val;
                    param.SetValue(val);
                }
                break;
            }

            case EffectParameterType.Int32:
            {
                // Use float params dictionary for simplicity, convert
                if (!_floatParams.ContainsKey(name))
                    _floatParams[name] = param.GetValueInt32();

                var val = _floatParams[name];
                if (ImGui.DragFloat(name, ref val, 1f))
                {
                    _floatParams[name] = val;
                    param.SetValue((int)val);
                }
                break;
            }

            default:
            {
                // Handle vector types by column count
                if (param.ParameterClass == EffectParameterClass.Vector)
                {
                    DrawVectorParameter(param, name);
                }
                else if (param.ParameterClass == EffectParameterClass.Object)
                {
                    // Texture parameter — show path label
                    ImGui.TextDisabled($"{name}: [Texture]");
                }
                else
                {
                    ImGui.TextDisabled($"{name}: {param.ParameterType} ({param.ParameterClass})");
                }
                break;
            }
        }
    }

    private void DrawVectorParameter(EffectParameter param, string name)
    {
        switch (param.ColumnCount)
        {
            case 2:
            {
                if (!_vec2Params.ContainsKey(name))
                {
                    var xna = param.GetValueVector2();
                    _vec2Params[name] = new Vector2(xna.X, xna.Y);
                }
                var v2 = _vec2Params[name];
                if (ImGui.DragFloat2(name, ref v2, 0.01f))
                {
                    _vec2Params[name] = v2;
                    param.SetValue(new Microsoft.Xna.Framework.Vector2(v2.X, v2.Y));
                }
                break;
            }
            case 3:
            {
                if (!_vec3Params.ContainsKey(name))
                {
                    var xna = param.GetValueVector3();
                    _vec3Params[name] = new Vector3(xna.X, xna.Y, xna.Z);
                }
                var v3 = _vec3Params[name];
                if (ImGui.DragFloat3(name, ref v3, 0.01f))
                {
                    _vec3Params[name] = v3;
                    param.SetValue(new Microsoft.Xna.Framework.Vector3(v3.X, v3.Y, v3.Z));
                }
                break;
            }
            case 4:
            {
                if (!_vec4Params.ContainsKey(name))
                {
                    var xna = param.GetValueVector4();
                    _vec4Params[name] = new Vector4(xna.X, xna.Y, xna.Z, xna.W);
                }
                var v4 = _vec4Params[name];

                // Detect if likely a color (name contains "color" or "tint")
                if (name.Contains("color", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("tint", StringComparison.OrdinalIgnoreCase))
                {
                    if (ImGui.ColorEdit4(name, ref v4))
                    {
                        _vec4Params[name] = v4;
                        param.SetValue(new Microsoft.Xna.Framework.Vector4(v4.X, v4.Y, v4.Z, v4.W));
                    }
                }
                else
                {
                    if (ImGui.DragFloat4(name, ref v4, 0.01f))
                    {
                        _vec4Params[name] = v4;
                        param.SetValue(new Microsoft.Xna.Framework.Vector4(v4.X, v4.Y, v4.Z, v4.W));
                    }
                }
                break;
            }
            default:
                ImGui.TextDisabled($"{name}: Vector{param.ColumnCount} (unsupported)");
                break;
        }
    }

    private void DrawPreviewArea()
    {
        ImGui.Text("Preview:");
        if (_previewTexture == null || _previewTextureRef == null)
        {
            ImGui.TextDisabled("Drop a texture above to see the effect preview.");
            return;
        }

        var avail = ImGui.GetContentRegionAvail();
        float maxSize = MathF.Min(avail.X, avail.Y - 10);
        if (maxSize < 32) return;

        float aspect = (float)_previewTexture.Width / _previewTexture.Height;
        float drawW, drawH;
        if (aspect >= 1f)
        {
            drawW = maxSize;
            drawH = maxSize / aspect;
        }
        else
        {
            drawH = maxSize;
            drawW = maxSize * aspect;
        }

        ImGui.Image(_previewTextureRef.Value, new Vector2(drawW, drawH));
    }

    private void LoadEffect()
    {
        if (_effectCache == null || string.IsNullOrWhiteSpace(_effectPath)) return;

        _currentEffect = _effectCache.Get(_effectPath);
        if (_currentEffect != null)
        {
            // Reset parameter caches
            _floatParams.Clear();
            _vec2Params.Clear();
            _vec3Params.Clear();
            _vec4Params.Clear();
            _boolParams.Clear();
            Log.Info($"Loaded effect for preview: {_effectPath}");
        }
        else
        {
            Log.Warn($"Could not load effect: {_effectPath}");
        }
    }

    private void LoadPreviewTexture()
    {
        if (_textureCache == null || _imGui == null) return;
        if (string.IsNullOrWhiteSpace(_previewTexturePath)) return;

        _previewTexture = _textureCache.Get(_previewTexturePath);
        if (_previewTexture != null)
        {
            _previewTextureRef = _imGui.BindTexture(_previewTexture);
        }
    }
}
