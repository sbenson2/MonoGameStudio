namespace MonoGameStudio.Core.Components;

[ComponentCategory("Audio")]
public struct AudioSource
{
    public string ClipPath;
    public float Volume;
    public float Pitch;
    public bool Loop;
    public bool PlayOnStart;

    public AudioSource()
    {
        ClipPath = "";
        Volume = 1f;
        Pitch = 1f;
        Loop = false;
        PlayOnStart = false;
    }
}
