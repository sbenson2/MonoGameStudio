namespace MonoGameStudio.Core.Components;

public struct GumScreen
{
    public string GumProjectPath;
    public string ScreenName;
    public bool IsActive;

    public GumScreen(string gumProjectPath, string screenName, bool isActive)
    {
        GumProjectPath = gumProjectPath;
        ScreenName = screenName;
        IsActive = isActive;
    }
}
