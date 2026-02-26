using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Core.Systems;

public class SceneManager
{
    private readonly WorldManager _worldManager;
    private readonly ScreenTransitionSystem _transitionSystem;
    private string? _currentScenePath;
    private readonly List<string> _sceneList = new();

    public string? CurrentScenePath => _currentScenePath;
    public IReadOnlyList<string> SceneList => _sceneList;
    public bool IsTransitioning => _transitionSystem.IsTransitioning;

    public event Action<string>? OnSceneLoaded;

    public SceneManager(WorldManager worldManager, ScreenTransitionSystem transitionSystem)
    {
        _worldManager = worldManager;
        _transitionSystem = transitionSystem;
    }

    public void SetSceneList(IEnumerable<string> scenes)
    {
        _sceneList.Clear();
        _sceneList.AddRange(scenes);
    }

    public void LoadScene(string path)
    {
        if (!File.Exists(path))
        {
            Log.Error($"Scene not found: {path}");
            return;
        }

        _worldManager.ResetWorld();
        SceneSerializer.LoadFromFile(path, _worldManager);
        _currentScenePath = path;
        OnSceneLoaded?.Invoke(path);
        Log.Info($"Scene loaded: {path}");
    }

    public void TransitionTo(string path, IScreenTransition transition, GraphicsDevice graphicsDevice,
        RenderTarget2D? currentSceneRT)
    {
        if (!File.Exists(path))
        {
            Log.Error($"Scene not found: {path}");
            return;
        }

        _transitionSystem.StartTransition(
            transition,
            graphicsDevice,
            currentSceneRT,
            onMidpoint: () => LoadScene(path),
            onComplete: () => Log.Info($"Transition to '{path}' complete")
        );
    }

    public void Update(float deltaTime)
    {
        _transitionSystem.Update(deltaTime);
    }
}
