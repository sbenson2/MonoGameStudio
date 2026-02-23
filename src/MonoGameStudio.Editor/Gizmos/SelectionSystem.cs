using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameStudio.Core.Components;
using MonoGameStudio.Core.World;
using MonoGameStudio.Editor.Editor;
using MonoGameStudio.Editor.Viewport;

namespace MonoGameStudio.Editor.Gizmos;

public class SelectionSystem
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private readonly EditorCamera _camera;

    private bool _isBoxSelecting;
    private Vector2 _boxStart;
    private Vector2 _boxEnd;

    public bool IsBoxSelecting => _isBoxSelecting;
    public Vector2 BoxStart => _boxStart;
    public Vector2 BoxEnd => _boxEnd;

    private const float SelectionRadius = 20f;

    public SelectionSystem(WorldManager worldManager, EditorState editorState, EditorCamera camera)
    {
        _worldManager = worldManager;
        _editorState = editorState;
        _camera = camera;
    }

    /// <summary>
    /// Returns true if selection consumed the click.
    /// </summary>
    public bool Update(MouseState mouse, MouseState prevMouse, Vector2 viewportOrigin, Vector2 viewportSize, bool isViewportHovered)
    {
        if (!isViewportHovered) { _isBoxSelecting = false; return false; }

        var localMouse = new Vector2(mouse.X, mouse.Y) - viewportOrigin;
        var worldMouse = _camera.ScreenToWorld(localMouse, viewportSize);

        // Left click detection
        bool justPressed = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool justReleased = mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;
        bool isHeld = mouse.LeftButton == ButtonState.Pressed;

        if (justPressed)
        {
            _boxStart = localMouse;
        }

        if (isHeld && !_isBoxSelecting)
        {
            float dragDist = Vector2.Distance(localMouse, _boxStart);
            if (dragDist > 5f)
            {
                _isBoxSelecting = true;
            }
        }

        if (_isBoxSelecting)
        {
            _boxEnd = localMouse;

            if (justReleased)
            {
                _isBoxSelecting = false;
                PerformBoxSelect(viewportSize);
                return true;
            }
            return false;
        }

        if (justReleased)
        {
            // Click-to-select
            var nearest = FindNearestEntity(worldMouse);
            if (nearest.HasValue)
            {
                var io = ImGuiNET.ImGui.GetIO();
                if (io.KeyCtrl)
                    _editorState.ToggleSelection(nearest.Value);
                else
                    _editorState.Select(nearest.Value);
            }
            else
            {
                _editorState.ClearSelection();
            }
            return true;
        }

        return false;
    }

    private Entity? FindNearestEntity(Vector2 worldPos)
    {
        Entity? nearest = null;
        float nearestDist = SelectionRadius;

        var query = new QueryDescription().WithAll<Position, EntityName>();
        _worldManager.World.Query(query, (Entity entity, ref Position pos) =>
        {
            float dist = Vector2.Distance(new Vector2(pos.X, pos.Y), worldPos);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = entity;
            }
        });

        return nearest;
    }

    private void PerformBoxSelect(Vector2 viewportSize)
    {
        var worldMin = _camera.ScreenToWorld(
            new Vector2(MathF.Min(_boxStart.X, _boxEnd.X), MathF.Min(_boxStart.Y, _boxEnd.Y)),
            viewportSize);
        var worldMax = _camera.ScreenToWorld(
            new Vector2(MathF.Max(_boxStart.X, _boxEnd.X), MathF.Max(_boxStart.Y, _boxEnd.Y)),
            viewportSize);

        _editorState.ClearSelection();

        var query = new QueryDescription().WithAll<Position, EntityName>();
        _worldManager.World.Query(query, (Entity entity, ref Position pos) =>
        {
            if (pos.X >= worldMin.X && pos.X <= worldMax.X &&
                pos.Y >= worldMin.Y && pos.Y <= worldMax.Y)
            {
                _editorState.AddToSelection(entity);
            }
        });
    }
}
