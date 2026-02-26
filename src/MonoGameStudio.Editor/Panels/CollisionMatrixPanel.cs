using System.Numerics;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Physics;

namespace MonoGameStudio.Editor.Panels;

public class CollisionMatrixPanel
{
    private CollisionLayerSettings? _settings;

    public void SetSettings(CollisionLayerSettings settings)
    {
        _settings = settings;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin("Collision Matrix", ref isOpen))
        {
            if (_settings == null)
            {
                ImGui.TextDisabled("No collision settings loaded");
            }
            else
            {
                DrawMatrix();
            }
        }
        ImGui.End();
    }

    private void DrawMatrix()
    {
        if (_settings == null) return;

        // Count active layers (non-default names)
        int activeCount = 0;
        for (int i = 0; i < CollisionLayerSettings.MaxLayers; i++)
        {
            if (!string.IsNullOrEmpty(_settings.LayerNames[i]) && _settings.LayerNames[i] != $"Layer {i}")
                activeCount = i + 1;
        }
        activeCount = Math.Max(activeCount, 4); // show at least 4
        activeCount = Math.Min(activeCount, 16); // cap display

        // Layer name editing
        if (ImGui.CollapsingHeader("Layer Names", ImGuiTreeNodeFlags.DefaultOpen))
        {
            for (int i = 0; i < activeCount; i++)
            {
                ImGui.PushID(i);
                var name = _settings.LayerNames[i];
                ImGui.SetNextItemWidth(150);
                if (ImGui.InputText($"##Layer{i}", ref name, 64))
                    _settings.LayerNames[i] = name;
                ImGui.PopID();
            }
        }

        ImGui.Spacing();

        // NxN checkbox grid
        if (ImGui.CollapsingHeader("Collision Matrix", ImGuiTreeNodeFlags.DefaultOpen))
        {
            // Header row
            ImGui.Text("     ");
            for (int j = 0; j < activeCount; j++)
            {
                ImGui.SameLine();
                ImGui.Text($" {j}");
            }

            for (int i = 0; i < activeCount; i++)
            {
                ImGui.Text($"{i}: {_settings.LayerNames[i],-6}");
                for (int j = i; j < activeCount; j++)
                {
                    ImGui.SameLine();
                    ImGui.PushID(i * CollisionLayerSettings.MaxLayers + j);
                    bool collides = _settings.GetCollision(i, j);
                    if (ImGui.Checkbox($"##col", ref collides))
                        _settings.SetCollision(i, j, collides);
                    ImGui.PopID();
                }
            }
        }
    }
}
