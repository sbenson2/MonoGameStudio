using Arch.Core;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.UI;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

public class GumUISystem
{
    private readonly WorldManager _worldManager;
    private readonly GumUIManager _gumUIManager;

    public GumUISystem(WorldManager worldManager, GumUIManager gumUIManager)
    {
        _worldManager = worldManager;
        _gumUIManager = gumUIManager;
    }

    public void LoadActiveScreens()
    {
        var world = _worldManager.World;
        var query = new QueryDescription().WithAll<GumScreen>();

        string? projectPath = null;
        string? screenName = null;

        world.Query(query, (Entity entity, ref GumScreen gumScreen) =>
        {
            if (gumScreen.IsActive && projectPath == null)
            {
                projectPath = gumScreen.GumProjectPath;
                screenName = gumScreen.ScreenName;
            }
        });

        if (projectPath != null && screenName != null)
        {
            _gumUIManager.LoadProject(projectPath, screenName);
        }
    }

    public void OnSceneLoaded()
    {
        _gumUIManager.ClearUI();
        LoadActiveScreens();
    }
}
