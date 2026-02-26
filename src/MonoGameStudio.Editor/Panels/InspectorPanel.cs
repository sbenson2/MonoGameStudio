using Arch.Core;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Core.World;
using ComponentRegistry = MonoGameStudio.Core.Serialization.ComponentRegistry;
using MonoGameStudio.Editor.Commands;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Inspector;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class InspectorPanel
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private readonly ComponentDrawer _componentDrawer;
    private readonly ComponentPicker _componentPicker;
    private CommandHistory? _commandHistory;

    public InspectorPanel(WorldManager worldManager, EditorState editorState)
    {
        _worldManager = worldManager;
        _editorState = editorState;
        _componentDrawer = new ComponentDrawer();
        _componentPicker = new ComponentPicker();
        _componentPicker.OnComponentSelected += OnAddComponent;
    }

    public void SetCommandHistory(CommandHistory commandHistory)
    {
        _commandHistory = commandHistory;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.Inspector, ref isOpen))
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
        IComponentDescriptor? descriptorToRemove = null;

        foreach (var type in componentTypes)
        {
            _componentDrawer.DrawComponent(world, entity, type);

            // Right-click context menu for removable components
            var descriptor = ComponentRegistry.GetDescriptor(type);
            if (descriptor != null && !descriptor.IsInternal && !descriptor.IsCoreTransform)
            {
                if (ImGui.BeginPopupContextItem($"##ctx_{descriptor.Name}"))
                {
                    if (ImGui.MenuItem($"Remove {descriptor.Name}"))
                    {
                        descriptorToRemove = descriptor;
                    }
                    ImGui.EndPopup();
                }
            }
        }

        // Execute removal outside the iteration loop
        if (descriptorToRemove != null)
        {
            var cmd = new RemoveComponentCommand(_worldManager, entity, descriptorToRemove);
            if (_commandHistory != null)
                _commandHistory.Execute(cmd);
            else
                cmd.Execute();
            _editorState.IsDirty = true;
        }

        ImGui.Separator();
        if (ImGui.Button("Add Component"))
        {
            ImGui.OpenPopup("AddComponent");
        }

        _componentPicker.Draw(world, entity);
    }

    private void OnAddComponent(Entity entity, IComponentDescriptor descriptor)
    {
        var cmd = new AddComponentCommand(_worldManager, entity, descriptor);
        if (_commandHistory != null)
            _commandHistory.Execute(cmd);
        else
            cmd.Execute();
        _editorState.IsDirty = true;
    }
}
