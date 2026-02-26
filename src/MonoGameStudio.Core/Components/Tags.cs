namespace MonoGameStudio.Core.Components;

public struct SelectedTag { }
public struct EditorOnlyTag { }

[ComponentCategory("General")]
public struct EntityTag
{
    public string Tag;
    public int Layer;

    public EntityTag()
    {
        Tag = "";
        Layer = 0;
    }
}
