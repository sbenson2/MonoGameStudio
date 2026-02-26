using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Rendering;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

/// <summary>
/// Post-processing editor panel. Displays an ordered list of post-process effects
/// with enable/disable toggles, reorder controls, parameter editing, and presets.
/// </summary>
public class PostProcessPanel
{
    private PostProcessorPipeline? _pipeline;

    // Add effect state
    private bool _showAddPopup;

    private static readonly string[] Presets =
    [
        "Bloom",
        "Vignette",
        "LUT Color Grading",
        "Grayscale",
        "Chromatic Aberration",
        "Blur",
        "Custom..."
    ];

    private string _customEffectPath = "";

    // Track which effect is expanded in the parameter editor
    private int _expandedEffect = -1;

    public void Initialize(PostProcessorPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.PostProcess, ref isOpen))
        {
            if (_pipeline == null)
            {
                ImGui.TextDisabled("Post-processing pipeline not initialized.");
                ImGui.End();
                return;
            }

            DrawToolbar();
            ImGui.Separator();
            DrawEffectList();
        }
        ImGui.End();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button("+ Add Effect"))
            ImGui.OpenPopup("AddEffectPopup");

        ImGui.SameLine();
        if (ImGui.Button("Reload All"))
            _pipeline!.ReloadEffects();

        // Add effect popup
        if (ImGui.BeginPopup("AddEffectPopup"))
        {
            ImGui.Text("Select Effect Preset:");
            ImGui.Separator();

            for (int i = 0; i < Presets.Length; i++)
            {
                if (ImGui.Selectable(Presets[i]))
                {
                    if (i == Presets.Length - 1)
                    {
                        // Custom — open path input
                        _showAddPopup = true;
                    }
                    else
                    {
                        AddPresetEffect(Presets[i]);
                    }
                }
            }

            ImGui.EndPopup();
        }

        // Custom effect path popup
        if (_showAddPopup)
        {
            ImGui.OpenPopup("CustomEffectPopup");
            _showAddPopup = false;
        }

        if (ImGui.BeginPopupModal("CustomEffectPopup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Effect file path (.mgfx / .xnb):");
            ImGui.InputText("##customFxPath", ref _customEffectPath, 512);

            if (ImGui.Button("Add"))
            {
                if (!string.IsNullOrWhiteSpace(_customEffectPath))
                {
                    var effect = new PostProcessEffect
                    {
                        Name = Path.GetFileNameWithoutExtension(_customEffectPath),
                        EffectPath = _customEffectPath,
                        Enabled = true
                    };
                    _pipeline!.AddEffect(effect);
                    _customEffectPath = "";
                    Log.Info($"Added custom post-process effect: {effect.Name}");
                }
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _customEffectPath = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawEffectList()
    {
        var effects = _pipeline!.Effects;

        if (effects.Count == 0)
        {
            ImGui.TextDisabled("No post-process effects. Click '+ Add Effect' to get started.");
            return;
        }

        int removeIndex = -1;
        int moveUpIndex = -1;
        int moveDownIndex = -1;

        for (int i = 0; i < effects.Count; i++)
        {
            var fx = effects[i];
            ImGui.PushID(i);

            // Enable/disable checkbox
            var enabled = fx.Enabled;
            if (ImGui.Checkbox("##enabled", ref enabled))
                fx.Enabled = enabled;

            ImGui.SameLine();

            // Effect name as collapsing header
            var flags = ImGuiTreeNodeFlags.AllowOverlap;
            if (_expandedEffect == i) flags |= ImGuiTreeNodeFlags.DefaultOpen;

            bool isOpen = ImGui.TreeNodeEx(fx.Name, flags);

            // Reorder and remove buttons on the same line
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - 80);
            if (i > 0 && ImGui.SmallButton("^")) moveUpIndex = i;
            ImGui.SameLine();
            if (i < effects.Count - 1 && ImGui.SmallButton("v")) moveDownIndex = i;
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f));
            if (ImGui.SmallButton("X")) removeIndex = i;
            ImGui.PopStyleColor();

            if (isOpen)
            {
                _expandedEffect = i;

                // Effect path (read-only display)
                ImGui.TextDisabled($"Path: {fx.EffectPath}");

                // Draw parameters if effect is loaded
                if (fx.Effect != null)
                {
                    DrawEffectParameters(fx);
                }
                else
                {
                    ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f),
                        "Effect not loaded. Check the file path.");
                }

                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        // Apply deferred operations (avoid modifying during iteration)
        if (removeIndex >= 0)
        {
            var name = effects[removeIndex].Name;
            _pipeline.RemoveEffectAt(removeIndex);
            if (_expandedEffect == removeIndex) _expandedEffect = -1;
            else if (_expandedEffect > removeIndex) _expandedEffect--;
            Log.Info($"Removed post-process effect: {name}");
        }
        if (moveUpIndex >= 0)
        {
            _pipeline.MoveUp(moveUpIndex);
            if (_expandedEffect == moveUpIndex) _expandedEffect--;
            else if (_expandedEffect == moveUpIndex - 1) _expandedEffect++;
        }
        if (moveDownIndex >= 0)
        {
            _pipeline.MoveDown(moveDownIndex);
            if (_expandedEffect == moveDownIndex) _expandedEffect++;
            else if (_expandedEffect == moveDownIndex + 1) _expandedEffect--;
        }
    }

    private void DrawEffectParameters(PostProcessEffect fx)
    {
        if (fx.Effect == null) return;

        ImGui.Indent();
        foreach (var param in fx.Effect.Parameters)
        {
            DrawSingleParameter(fx, param);
        }
        ImGui.Unindent();
    }

    private void DrawSingleParameter(PostProcessEffect fx, EffectParameter param)
    {
        var name = param.Name;

        switch (param.ParameterType)
        {
            case EffectParameterType.Single:
            {
                if (!fx.FloatParameters.TryGetValue(name, out var val))
                {
                    val = param.GetValueSingle();
                    fx.FloatParameters[name] = val;
                }
                if (ImGui.SliderFloat(name, ref val, 0f, 10f))
                {
                    fx.FloatParameters[name] = val;
                    param.SetValue(val);
                }
                break;
            }

            case EffectParameterType.Bool:
            {
                bool val = param.GetValueBoolean();
                if (ImGui.Checkbox(name, ref val))
                    param.SetValue(val);
                break;
            }

            default:
            {
                if (param.ParameterClass == EffectParameterClass.Vector)
                {
                    switch (param.ColumnCount)
                    {
                        case 2:
                        {
                            if (!fx.Vector2Parameters.TryGetValue(name, out var xnaVal))
                            {
                                xnaVal = param.GetValueVector2();
                                fx.Vector2Parameters[name] = xnaVal;
                            }
                            var v = new Vector2(xnaVal.X, xnaVal.Y);
                            if (ImGui.DragFloat2(name, ref v, 0.01f))
                            {
                                var newVal = new Microsoft.Xna.Framework.Vector2(v.X, v.Y);
                                fx.Vector2Parameters[name] = newVal;
                                param.SetValue(newVal);
                            }
                            break;
                        }
                        case 4:
                        {
                            if (!fx.Vector4Parameters.TryGetValue(name, out var xnaVal))
                            {
                                xnaVal = param.GetValueVector4();
                                fx.Vector4Parameters[name] = xnaVal;
                            }
                            var v = new Vector4(xnaVal.X, xnaVal.Y, xnaVal.Z, xnaVal.W);

                            bool isColor = name.Contains("color", StringComparison.OrdinalIgnoreCase) ||
                                           name.Contains("tint", StringComparison.OrdinalIgnoreCase);
                            bool changed = isColor
                                ? ImGui.ColorEdit4(name, ref v)
                                : ImGui.DragFloat4(name, ref v, 0.01f);

                            if (changed)
                            {
                                var newVal = new Microsoft.Xna.Framework.Vector4(v.X, v.Y, v.Z, v.W);
                                fx.Vector4Parameters[name] = newVal;
                                param.SetValue(newVal);
                            }
                            break;
                        }
                        default:
                            ImGui.TextDisabled($"{name}: Vector{param.ColumnCount}");
                            break;
                    }
                }
                else if (param.ParameterClass == EffectParameterClass.Object)
                {
                    ImGui.TextDisabled($"{name}: [Texture]");
                }
                else
                {
                    ImGui.TextDisabled($"{name}: {param.ParameterType}");
                }
                break;
            }
        }
    }

    private void AddPresetEffect(string presetName)
    {
        var effect = new PostProcessEffect
        {
            Name = presetName,
            Enabled = true
        };

        // Set default parameters based on preset
        switch (presetName)
        {
            case "Bloom":
                effect.FloatParameters["BloomThreshold"] = 0.5f;
                effect.FloatParameters["BloomIntensity"] = 1.2f;
                effect.FloatParameters["BlurAmount"] = 4f;
                break;

            case "Vignette":
                effect.FloatParameters["Radius"] = 0.75f;
                effect.FloatParameters["Softness"] = 0.45f;
                effect.FloatParameters["Intensity"] = 1f;
                break;

            case "LUT Color Grading":
                effect.FloatParameters["Contribution"] = 1f;
                break;

            case "Grayscale":
                effect.FloatParameters["Strength"] = 1f;
                break;

            case "Chromatic Aberration":
                effect.FloatParameters["Offset"] = 0.005f;
                break;

            case "Blur":
                effect.FloatParameters["BlurAmount"] = 2f;
                break;
        }

        _pipeline!.AddEffect(effect);
        _expandedEffect = _pipeline.Effects.Count - 1;
        Log.Info($"Added post-process effect preset: {presetName}");
    }

    /// <summary>
    /// Exports the current pipeline state to a PostProcessStackDocument for serialization.
    /// </summary>
    public PostProcessStackDocument ExportStack()
    {
        var doc = new PostProcessStackDocument();
        if (_pipeline == null) return doc;

        foreach (var fx in _pipeline.Effects)
        {
            var data = new PostProcessEffectData
            {
                Name = fx.Name,
                Enabled = fx.Enabled,
                EffectPath = fx.EffectPath
            };

            foreach (var (name, val) in fx.FloatParameters)
                data.Parameters[name] = System.Text.Json.JsonSerializer.SerializeToElement(val);

            doc.Effects.Add(data);
        }

        return doc;
    }

    /// <summary>
    /// Imports a PostProcessStackDocument, rebuilding the pipeline.
    /// </summary>
    public void ImportStack(PostProcessStackDocument doc)
    {
        if (_pipeline == null) return;

        // Clear existing
        while (_pipeline.Effects.Count > 0)
            _pipeline.RemoveEffectAt(0);

        foreach (var data in doc.Effects)
        {
            var fx = new PostProcessEffect
            {
                Name = data.Name,
                Enabled = data.Enabled,
                EffectPath = data.EffectPath
            };

            foreach (var (name, element) in data.Parameters)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                    fx.FloatParameters[name] = element.GetSingle();
            }

            _pipeline.AddEffect(fx);
        }

        _expandedEffect = -1;
        Log.Info($"Imported post-process stack with {doc.Effects.Count} effect(s)");
    }
}
