using Arch.Core;
using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Editor.Editor;

public class EditorState
{
    public ApplicationPhase Phase { get; set; } = ApplicationPhase.StartScreen;
    public EditorMode Mode { get; set; } = EditorMode.Edit;
    public string? CurrentScenePath { get; set; }
    public bool IsDirty { get; set; }

    // Panel visibility (fields for ref access)
    public bool ShowHierarchy = true;
    public bool ShowInspector = true;
    public bool ShowConsole = true;
    public bool ShowAssetBrowser = true;
    public bool ShowViewport = true;
    public bool ShowSpriteSheet = false;
    public bool ShowAnimation = false;
    public bool ShowSettings = false;

    // Virtual resolution preview
    public bool ShowVirtualResolution = false;
    public int VirtualResolutionPreset = 0; // index into VirtualResolutionPresets
    public static readonly (string Label, int Width, int Height)[] VirtualResolutionPresets =
    {
        ("1920x1080 (Full HD)", 1920, 1080),
        ("1280x720 (HD)", 1280, 720),
        ("640x360 (SD)", 640, 360),
        ("800x600", 800, 600),
        ("480x270 (Low)", 480, 270),
        ("320x180 (Pixel)", 320, 180),
    };

    public (int Width, int Height) CurrentVirtualResolution =>
        (VirtualResolutionPresets[VirtualResolutionPreset].Width,
         VirtualResolutionPresets[VirtualResolutionPreset].Height);

    public List<Entity> SelectedEntities { get; } = new();

    public void ClearSelection()
    {
        SelectedEntities.Clear();
    }

    public void Select(Entity entity)
    {
        ClearSelection();
        SelectedEntities.Add(entity);
    }

    public void ToggleSelection(Entity entity)
    {
        var idx = SelectedEntities.FindIndex(e => e.Equals(entity));
        if (idx >= 0)
            SelectedEntities.RemoveAt(idx);
        else
            SelectedEntities.Add(entity);
    }

    public void AddToSelection(Entity entity)
    {
        if (!SelectedEntities.Any(e => e.Equals(entity)))
            SelectedEntities.Add(entity);
    }

    public bool IsSelected(Entity entity) => SelectedEntities.Any(e => e.Equals(entity));

    public Entity? PrimarySelection => SelectedEntities.Count > 0 ? SelectedEntities[0] : null;
}
