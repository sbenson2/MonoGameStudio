using Arch.Core;
using ImGuiNET;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Inspector;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class InspectorPanel
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private readonly ComponentDrawer _componentDrawer;

    public InspectorPanel(WorldManager worldManager, EditorState editorState)
    {
        _worldManager = worldManager;
        _editorState = editorState;
        _componentDrawer = new ComponentDrawer();
    }

    public void Draw()
    {
        if (ImGui.Begin(LayoutDefinitions.Inspector))
        {
            var primary = _editorState.PrimarySelection;
            if (primary.HasValue && _worldManager.World.IsAlive(primary.Value))
            {
                DrawEntity(primary.Value);
            }
            else
            {
                ImGui.TextDisabled("No entity selected");
            }
        }
        ImGui.End();
    }

    private void DrawEntity(Entity entity)
    {
        var world = _worldManager.World;

        var componentTypes = ComponentDrawer.GetComponentTypes(world, entity);
        foreach (var type in componentTypes)
        {
            _componentDrawer.DrawComponent(world, entity, type);
        }

        ImGui.Separator();
        if (ImGui.Button("Add Component"))
        {
            ImGui.OpenPopup("AddComponent");
        }

        if (ImGui.BeginPopup("AddComponent"))
        {
            ImGui.TextDisabled("Component picker coming soon");
            ImGui.EndPopup();
        }
    }
}
