using Arch.Core;
using Arch.Core.Extensions;
using Hexa.NET.ImGui;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Editor.ImGuiIntegration;
using ComponentRegistry = MonoGameStudio.Core.Serialization.ComponentRegistry;

namespace MonoGameStudio.Editor.Inspector;

/// <summary>
/// Searchable popup for adding components to an entity, grouped by category.
/// Uses IComponentDescriptor instead of Type for the selection callback.
/// </summary>
public class ComponentPicker
{
    private string _searchFilter = "";
    private List<(string Category, List<IComponentDescriptor> Descriptors)>? _cachedGroups;

    public event Action<Entity, IComponentDescriptor>? OnComponentSelected;

    public void Draw(Arch.Core.World world, Entity entity)
    {
        if (ImGui.BeginPopup("AddComponent"))
        {
            ImGui.SetNextItemWidth(250);
            ImGui.InputTextWithHint("##search", $"{FontAwesomeIcons.Search} Search components...",
                ref _searchFilter, 256);

            ImGui.Separator();

            var signature = entity.GetComponentTypes();
            var existingTypes = new HashSet<Type>();
            foreach (var ct in signature)
                existingTypes.Add(ct);

            var groups = GetGroupedDescriptors();

            bool anyShown = false;
            foreach (var (category, descriptors) in groups)
            {
                var filtered = descriptors
                    .Where(d => !existingTypes.Contains(d.ComponentType))
                    .Where(d => string.IsNullOrEmpty(_searchFilter) ||
                                d.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count == 0) continue;

                anyShown = true;
                ImGui.TextDisabled(category);

                foreach (var descriptor in filtered)
                {
                    if (ImGui.Selectable($"  {descriptor.Name}"))
                    {
                        OnComponentSelected?.Invoke(entity, descriptor);
                        _searchFilter = "";
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.Spacing();
            }

            if (!anyShown)
            {
                ImGui.TextDisabled("No components available");
            }

            ImGui.EndPopup();
        }
    }

    private List<(string Category, List<IComponentDescriptor> Descriptors)> GetGroupedDescriptors()
    {
        if (_cachedGroups != null) return _cachedGroups;

        _cachedGroups = ComponentRegistry.GetAddableDescriptors()
            .GroupBy(d => d.Category)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.OrderBy(d => d.Name).ToList()))
            .ToList();

        return _cachedGroups;
    }
}
