using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Serialization;

namespace MonoGameStudio.Editor.Inspector;

/// <summary>
/// Draws individual fields in the inspector based on FieldKind enum.
/// Dispatches on descriptor metadata instead of FieldInfo.FieldType reflection.
/// </summary>
public static class FieldDrawers
{
    public static bool DrawField(FieldDescriptor field, ref object component)
    {
        if (field.HideInInspector)
            return false;

        if (field.Header != null)
        {
            ImGui.Separator();
            ImGui.Text(field.Header);
        }

        var value = field.GetValue(component);
        bool modified = false;
        var label = field.Name;

        switch (field.Kind)
        {
            case FieldKind.Float:
            {
                float v = (float)(value ?? 0f);
                if (field.RangeMin.HasValue && field.RangeMax.HasValue)
                    modified = ImGui.SliderFloat(label, ref v, field.RangeMin.Value, field.RangeMax.Value);
                else
                    modified = ImGui.DragFloat(label, ref v, 0.1f);
                if (modified) component = field.SetValue(component, v);
                break;
            }

            case FieldKind.Int:
            {
                int v = (int)(value ?? 0);
                modified = ImGui.DragInt(label, ref v);
                if (modified) component = field.SetValue(component, v);
                break;
            }

            case FieldKind.Bool:
            {
                bool v = (bool)(value ?? false);
                modified = ImGui.Checkbox(label, ref v);
                if (modified) component = field.SetValue(component, v);
                break;
            }

            case FieldKind.String:
            {
                string v = (string)(value ?? "");
                if (ImGui.InputText(label, ref v, 512))
                {
                    modified = true;
                    component = field.SetValue(component, v);
                }
                break;
            }

            case FieldKind.Vector2:
            {
                // Handle both XNA Vector2 and System.Numerics.Vector2
                if (value is Microsoft.Xna.Framework.Vector2 xna)
                {
                    var v = new Vector2(xna.X, xna.Y);
                    modified = ImGui.DragFloat2(label, ref v, 0.1f);
                    if (modified)
                        component = field.SetValue(component, new Microsoft.Xna.Framework.Vector2(v.X, v.Y));
                }
                else
                {
                    var v = (Vector2)(value ?? Vector2.Zero);
                    modified = ImGui.DragFloat2(label, ref v, 0.1f);
                    if (modified) component = field.SetValue(component, v);
                }
                break;
            }

            case FieldKind.Color:
            {
                var c = (Microsoft.Xna.Framework.Color)(value ?? Microsoft.Xna.Framework.Color.White);
                var v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                modified = ImGui.ColorEdit4(label, ref v);
                if (modified)
                    component = field.SetValue(component, new Microsoft.Xna.Framework.Color(v.X, v.Y, v.Z, v.W));
                break;
            }

            case FieldKind.Rectangle:
            {
                var r = (Microsoft.Xna.Framework.Rectangle)(value ?? Microsoft.Xna.Framework.Rectangle.Empty);
                var vals = new Vector4(r.X, r.Y, r.Width, r.Height);
                if (ImGui.DragFloat4(label, ref vals, 1f))
                {
                    modified = true;
                    component = field.SetValue(component, new Microsoft.Xna.Framework.Rectangle(
                        (int)vals.X, (int)vals.Y, (int)vals.Z, (int)vals.W));
                }
                break;
            }

            case FieldKind.Enum:
            {
                if (field.EnumType != null)
                {
                    var names = Enum.GetNames(field.EnumType);
                    int current = Array.IndexOf(names, value?.ToString() ?? names[0]);
                    if (current < 0) current = 0;
                    if (ImGui.Combo(label, ref current, names, names.Length))
                    {
                        modified = true;
                        component = field.SetValue(component, Enum.Parse(field.EnumType, names[current]));
                    }
                }
                else
                {
                    ImGui.TextDisabled($"{label}: Enum (missing EnumType)");
                }
                break;
            }

            case FieldKind.Guid:
            {
                var guid = (Guid)(value ?? Guid.Empty);
                ImGui.Text($"{label}: {guid:N}");
                break;
            }

            default:
                ImGui.TextDisabled($"{label}: {field.Kind} (unsupported)");
                break;
        }

        // Tooltip
        if (field.Tooltip != null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(field.Tooltip);
        }

        return modified;
    }
}
