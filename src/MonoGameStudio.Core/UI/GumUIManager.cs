using Microsoft.Xna.Framework;
using MonoGameGum;
using Gum.DataTypes;
using RenderingLibrary;

namespace MonoGameStudio.Core.UI;

public class GumUIManager
{
    private Game _game = null!;
    private GumProjectSave? _loadedProject;
    private string? _loadedProjectPath;

    public bool IsInputEnabled { get; set; }

    public void Initialize(Game game)
    {
        _game = game;
        // Initialize Gum with no project — projects load on demand via ECS
        GumService.Default.Initialize(game);
    }

    public void LoadProject(string path, string screenName)
    {
        ClearUI();

        // Reload project if path changed
        if (_loadedProjectPath != path)
        {
            _loadedProject = GumService.Default.Initialize(_game, path);
            _loadedProjectPath = path;
        }

        if (_loadedProject == null) return;

        var screen = _loadedProject.Screens.Find(item => item.Name == screenName);
        if (screen == null) return;

        var screenRuntime = screen.ToGraphicalUiElement();
        screenRuntime.AddToRoot();
    }

    public void ClearUI()
    {
        var root = GumService.Default.Root;
        if (root != null)
        {
            while (root.Children.Count > 0)
            {
                root.Children[^1].RemoveFromManagers();
            }
        }
    }

    public void UpdateCanvasSize(int width, int height)
    {
        GumService.Default.CanvasWidth = width;
        GumService.Default.CanvasHeight = height;
    }

    public void SetViewportOffset(float x, float y)
    {
        // Offset Gum's cursor so mouse coordinates are viewport-local
        var cursor = GumService.Default.Cursor;
        cursor.TransformMatrix = Matrix.CreateTranslation(-x, -y, 0);
    }

    public void Update(GameTime gameTime)
    {
        if (IsInputEnabled)
        {
            GumService.Default.Update(gameTime);
        }
    }

    public void Draw()
    {
        GumService.Default.Draw();
    }
}
