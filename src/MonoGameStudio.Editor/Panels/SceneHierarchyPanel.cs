using Arch.Core;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Layout;

namespace MonoGameStudio.Editor.Panels;

public class SceneHierarchyPanel
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private Entity? _renamingEntity;
    private string _renameText = "";
    private string _searchFilter = "";

    public event Action<Entity>? OnEntityCreated;
    public event Action<Entity>? OnEntityDeleted;
    public event Action<Entity>? OnEntityDuplicated;
    public event Action<Entity, string>? OnEntityRenamed;
    public event Action<Entity, Entity>? OnEntityReparented; // child, newParent
    public event Action<Entity>? OnSaveAsPrefab;

    public SceneHierarchyPanel(WorldManager worldManager, EditorState editorState)
    {
        _worldManager = worldManager;
        _editorState = editorState;
    }

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;
        if (ImGui.Begin(LayoutDefinitions.Hierarchy, ref isOpen))
        {
            // Search bar
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##HierarchySearch", "Search entities...", ref _searchFilter, 256);
            ImGui.Separator();

            // Context menu on empty space
            if (ImGui.BeginPopupContextWindow("HierarchyContext"))
            {
                if (ImGui.MenuItem("Create Entity"))
                {
                    var entity = _worldManager.CreateEntity();
                    _editorState.Select(entity);
                    OnEntityCreated?.Invoke(entity);
                }
                ImGui.EndPopup();
            }

            bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
            var roots = _worldManager.GetRootEntities();
            foreach (var root in roots)
            {
                if (hasFilter)
                {
                    if (EntityMatchesFilter(root))
                        DrawEntityNode(root, true);
                }
                else
                {
                    DrawEntityNode(root, false);
                }
            }

            // Drop on empty space to unparent
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ENTITY");
                unsafe
                {
                    if (payload.Handle != null)
                    {
                        var draggedEntity = *(Entity*)payload.Data;
                        if (_worldManager.World.IsAlive(draggedEntity))
                        {
                            _worldManager.RemoveParent(draggedEntity);
                            OnEntityReparented?.Invoke(draggedEntity, Entity.Null);
                        }
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
        ImGui.End();
    }

    private void DrawEntityNode(Entity entity, bool forceOpen = false)
    {
        var world = _worldManager.World;
        if (!world.IsAlive(entity)) return;

        var name = world.Get<EntityName>(entity).Name;
        var children = _worldManager.GetChildren(entity);
        bool hasChildren = children.Count > 0;
        bool isSelected = _editorState.IsSelected(entity);
        bool isRenaming = _renamingEntity.HasValue && _renamingEntity.Value.Equals(entity);

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (isSelected) flags |= ImGuiTreeNodeFlags.Selected;
        if (!hasChildren) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        if (forceOpen && hasChildren) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        var id = entity.Id;
        bool opened = ImGui.TreeNodeEx($"{name}##{id}", flags);

        // Selection
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen())
        {
            var io = ImGui.GetIO();
            if (io.KeyCtrl)
                _editorState.ToggleSelection(entity);
            else
                _editorState.Select(entity);
        }

        // Drag source
        if (ImGui.BeginDragDropSource())
        {
            unsafe
            {
                var e = entity;
                ImGui.SetDragDropPayload("ENTITY", &e, (uint)sizeof(Entity));
            }
            ImGui.Text(name);
            ImGui.EndDragDropSource();
        }

        // Drop target (reparent)
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ENTITY");
            unsafe
            {
                if (payload.Handle != null)
                {
                    var draggedEntity = *(Entity*)payload.Data;
                    if (world.IsAlive(draggedEntity) && !draggedEntity.Equals(entity))
                    {
                        _worldManager.SetParent(draggedEntity, entity);
                        OnEntityReparented?.Invoke(draggedEntity, entity);
                    }
                }
            }
            ImGui.EndDragDropTarget();
        }

        // Context menu
        if (ImGui.BeginPopupContextItem($"EntityContext##{id}"))
        {
            if (ImGui.MenuItem("Create Child"))
            {
                var child = _worldManager.CreateEntity("Entity", entity);
                _editorState.Select(child);
                OnEntityCreated?.Invoke(child);
            }
            if (ImGui.MenuItem("Duplicate"))
            {
                var dup = _worldManager.DuplicateEntity(entity);
                _editorState.Select(dup);
                OnEntityDuplicated?.Invoke(dup);
            }
            if (ImGui.MenuItem("Rename", "F2"))
            {
                StartRename(entity, name);
            }
            if (ImGui.MenuItem("Save as Prefab"))
            {
                OnSaveAsPrefab?.Invoke(entity);
            }
            ImGui.Separator();
            if (ImGui.MenuItem("Delete", "Del"))
            {
                OnEntityDeleted?.Invoke(entity);
                _editorState.ClearSelection();
                _worldManager.DestroyEntity(entity);
            }
            ImGui.EndPopup();
        }

        // Double-click to rename
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            StartRename(entity, name);
        }

        // Inline rename
        if (isRenaming)
        {
            ImGui.SameLine();
            ImGui.SetKeyboardFocusHere();
            if (ImGui.InputText($"##rename{id}", ref _renameText, 256,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
            {
                FinishRename(entity);
            }
            if (!ImGui.IsItemActive() && _renamingEntity.HasValue)
            {
                FinishRename(entity);
            }
        }

        // Draw children
        if (opened && hasChildren)
        {
            foreach (var child in children)
            {
                if (forceOpen && !EntityMatchesFilter(child))
                    continue;
                DrawEntityNode(child, forceOpen);
            }
            ImGui.TreePop();
        }
    }

    private void StartRename(Entity entity, string currentName)
    {
        _renamingEntity = entity;
        _renameText = currentName;
    }

    private void FinishRename(Entity entity)
    {
        if (!string.IsNullOrWhiteSpace(_renameText))
        {
            _worldManager.RenameEntity(entity, _renameText);
            OnEntityRenamed?.Invoke(entity, _renameText);
        }
        _renamingEntity = null;
    }

    /// <summary>
    /// Returns true if this entity or any of its descendants match the search filter.
    /// </summary>
    private bool EntityMatchesFilter(Entity entity)
    {
        var world = _worldManager.World;
        if (!world.IsAlive(entity)) return false;

        var name = world.Get<EntityName>(entity).Name;
        if (name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check children recursively
        var children = _worldManager.GetChildren(entity);
        foreach (var child in children)
        {
            if (EntityMatchesFilter(child))
                return true;
        }
        return false;
    }
}
