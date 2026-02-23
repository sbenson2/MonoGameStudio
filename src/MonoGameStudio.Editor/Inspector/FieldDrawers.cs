using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace MonoGameStudio.Editor.Inspector;

/// <summary>
/// Custom attributes for inspector display.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }
    public RangeAttribute(float min, float max) { Min = min; Max = max; }
}

[AttributeUsage(AttributeTargets.Field)]
public class TooltipAttribute : Attribute
{
    public string Text { get; }
    public TooltipAttribute(string text) { Text = text; }
}

[AttributeUsage(AttributeTargets.Field)]
public class HeaderAttribute : Attribute
{
    public string Text { get; }
    public HeaderAttribute(string text) { Text = text; }
}

[AttributeUsage(AttributeTargets.Field)]
public class HideInInspectorAttribute : Attribute { }

/// <summary>
/// Draws individual fields in the inspector based on their type.
/// </summary>
public static class FieldDrawers
{
    public static bool DrawField(string label, FieldInfo field, ref object component)
    {
        if (field.GetCustomAttribute<HideInInspectorAttribute>() != null)
            return false;

        var header = field.GetCustomAttribute<HeaderAttribute>();
        if (header != null)
        {
            ImGui.Separator();
            ImGui.Text(header.Text);
        }

        var value = field.GetValue(component);
        bool modified = false;

        var type = field.FieldType;

        if (type == typeof(float))
        {
            float v = (float)(value ?? 0f);
            var range = field.GetCustomAttribute<RangeAttribute>();
            if (range != null)
                modified = ImGui.SliderFloat(label, ref v, range.Min, range.Max);
            else
                modified = ImGui.DragFloat(label, ref v, 0.1f);
            if (modified) field.SetValue(component, v);
        }
        else if (type == typeof(int))
        {
            int v = (int)(value ?? 0);
            modified = ImGui.DragInt(label, ref v);
            if (modified) field.SetValue(component, v);
        }
        else if (type == typeof(bool))
        {
            bool v = (bool)(value ?? false);
            modified = ImGui.Checkbox(label, ref v);
            if (modified) field.SetValue(component, v);
        }
        else if (type == typeof(string))
        {
            string v = (string)(value ?? "");
            var buf = new byte[512];
            var bytes = System.Text.Encoding.UTF8.GetBytes(v);
            Array.Copy(bytes, buf, Math.Min(bytes.Length, buf.Length - 1));
            if (ImGui.InputText(label, buf, (uint)buf.Length))
            {
                modified = true;
                field.SetValue(component, System.Text.Encoding.UTF8.GetString(buf).TrimEnd('\0'));
            }
        }
        else if (type == typeof(Vector2))
        {
            var v = (Vector2)(value ?? Vector2.Zero);
            modified = ImGui.DragFloat2(label, ref v, 0.1f);
            if (modified) field.SetValue(component, v);
        }
        else if (type == typeof(Microsoft.Xna.Framework.Vector2))
        {
            var xna = (Microsoft.Xna.Framework.Vector2)(value ?? Microsoft.Xna.Framework.Vector2.Zero);
            var v = new Vector2(xna.X, xna.Y);
            modified = ImGui.DragFloat2(label, ref v, 0.1f);
            if (modified)
                field.SetValue(component, new Microsoft.Xna.Framework.Vector2(v.X, v.Y));
        }
        else if (type == typeof(Microsoft.Xna.Framework.Color))
        {
            var c = (Microsoft.Xna.Framework.Color)(value ?? Microsoft.Xna.Framework.Color.White);
            var v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            modified = ImGui.ColorEdit4(label, ref v);
            if (modified)
                field.SetValue(component, new Microsoft.Xna.Framework.Color(v.X, v.Y, v.Z, v.W));
        }
        else if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            int current = Array.IndexOf(names, value?.ToString() ?? names[0]);
            if (current < 0) current = 0;
            if (ImGui.Combo(label, ref current, names, names.Length))
            {
                modified = true;
                field.SetValue(component, Enum.Parse(type, names[current]));
            }
        }
        else if (type == typeof(Guid))
        {
            var guid = (Guid)(value ?? Guid.Empty);
            ImGui.Text($"{label}: {guid:N}");
        }
        else
        {
            ImGui.TextDisabled($"{label}: {type.Name} (unsupported)");
        }

        // Tooltip
        var tooltip = field.GetCustomAttribute<TooltipAttribute>();
        if (tooltip != null && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip.Text);
        }

        return modified;
    }
}
