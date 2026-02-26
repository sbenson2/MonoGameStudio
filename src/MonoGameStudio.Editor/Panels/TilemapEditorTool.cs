using MonoGameStudio.Core.Data;
using MonoGameStudio.Editor.Commands;

namespace MonoGameStudio.Editor.Panels;

/// <summary>
/// Common interface for tilemap editing tools (paint, erase, fill, rect).
/// Tools receive tile coordinates and produce TileChange arrays for undo/redo.
/// </summary>
public interface ITilemapTool
{
    string Name { get; }

    /// <summary>
    /// Called when the mouse button is first pressed on a tile.
    /// </summary>
    void OnMouseDown(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId);

    /// <summary>
    /// Called while the mouse is held and dragged across tiles.
    /// </summary>
    void OnMouseDrag(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId);

    /// <summary>
    /// Called when the mouse button is released. Returns the accumulated tile changes for undo/redo.
    /// </summary>
    TileChange[] OnMouseUp(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId);
}

/// <summary>
/// Paints the selected tile wherever the cursor moves.
/// </summary>
public class PaintTool : ITilemapTool
{
    public string Name => "Paint";

    private readonly List<TileChange> _changes = new();
    private readonly HashSet<long> _painted = new(); // track already-painted positions

    public void OnMouseDown(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _changes.Clear();
        _painted.Clear();
        ApplyTile(doc, layerIndex, tileX, tileY, selectedTileId);
    }

    public void OnMouseDrag(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        ApplyTile(doc, layerIndex, tileX, tileY, selectedTileId);
    }

    public TileChange[] OnMouseUp(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        var result = _changes.ToArray();
        _changes.Clear();
        _painted.Clear();
        return result;
    }

    private void ApplyTile(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        long key = ((long)tileX << 32) | (uint)tileY;
        if (_painted.Contains(key)) return;
        _painted.Add(key);

        int oldTile = doc.GetTile(layerIndex, tileX, tileY);
        if (oldTile == selectedTileId) return;

        doc.SetTile(layerIndex, tileX, tileY, selectedTileId);
        _changes.Add(new TileChange(tileX, tileY, oldTile, selectedTileId));
    }
}

/// <summary>
/// Erases tiles (sets to -1) wherever the cursor moves.
/// </summary>
public class EraseTool : ITilemapTool
{
    public string Name => "Erase";

    private readonly List<TileChange> _changes = new();
    private readonly HashSet<long> _erased = new();

    public void OnMouseDown(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _changes.Clear();
        _erased.Clear();
        EraseTile(doc, layerIndex, tileX, tileY);
    }

    public void OnMouseDrag(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        EraseTile(doc, layerIndex, tileX, tileY);
    }

    public TileChange[] OnMouseUp(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        var result = _changes.ToArray();
        _changes.Clear();
        _erased.Clear();
        return result;
    }

    private void EraseTile(TilemapDocument doc, int layerIndex, int tileX, int tileY)
    {
        long key = ((long)tileX << 32) | (uint)tileY;
        if (_erased.Contains(key)) return;
        _erased.Add(key);

        int oldTile = doc.GetTile(layerIndex, tileX, tileY);
        if (oldTile == -1) return;

        doc.SetTile(layerIndex, tileX, tileY, -1);
        _changes.Add(new TileChange(tileX, tileY, oldTile, -1));
    }
}

/// <summary>
/// Flood-fill tool using BFS. Fills a contiguous region of same-type tiles
/// with the selected tile ID.
/// </summary>
public class FillTool : ITilemapTool
{
    public string Name => "Fill";

    private TileChange[]? _lastFill;

    public void OnMouseDown(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _lastFill = PerformFill(doc, layerIndex, tileX, tileY, selectedTileId);
    }

    public void OnMouseDrag(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        // Fill only applies on click, not drag
    }

    public TileChange[] OnMouseUp(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        var result = _lastFill ?? [];
        _lastFill = null;
        return result;
    }

    private static TileChange[] PerformFill(TilemapDocument doc, int layerIndex, int startX, int startY, int fillTileId)
    {
        int targetTile = doc.GetTile(layerIndex, startX, startY);
        if (targetTile == fillTileId) return [];

        var changes = new List<TileChange>();
        var visited = new HashSet<long>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            if (x < 0 || x >= doc.MapWidth || y < 0 || y >= doc.MapHeight) continue;

            long key = ((long)x << 32) | (uint)y;
            if (visited.Contains(key)) continue;
            visited.Add(key);

            int current = doc.GetTile(layerIndex, x, y);
            if (current != targetTile) continue;

            doc.SetTile(layerIndex, x, y, fillTileId);
            changes.Add(new TileChange(x, y, current, fillTileId));

            queue.Enqueue((x + 1, y));
            queue.Enqueue((x - 1, y));
            queue.Enqueue((x, y + 1));
            queue.Enqueue((x, y - 1));
        }

        return changes.ToArray();
    }
}

/// <summary>
/// Rectangle selection tool. Click to set one corner, drag to the opposite corner,
/// release to fill the rectangle with the selected tile.
/// </summary>
public class RectTool : ITilemapTool
{
    public string Name => "Rect";

    private int _startX, _startY;
    private int _endX, _endY;
    private bool _isDragging;

    /// <summary>
    /// Current rectangle bounds in tile coordinates (for preview rendering).
    /// </summary>
    public (int minX, int minY, int maxX, int maxY) CurrentRect
    {
        get
        {
            if (!_isDragging) return (0, 0, 0, 0);
            return (
                Math.Min(_startX, _endX),
                Math.Min(_startY, _endY),
                Math.Max(_startX, _endX),
                Math.Max(_startY, _endY));
        }
    }

    public bool IsDragging => _isDragging;

    public void OnMouseDown(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _startX = tileX;
        _startY = tileY;
        _endX = tileX;
        _endY = tileY;
        _isDragging = true;
    }

    public void OnMouseDrag(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _endX = tileX;
        _endY = tileY;
    }

    public TileChange[] OnMouseUp(TilemapDocument doc, int layerIndex, int tileX, int tileY, int selectedTileId)
    {
        _endX = tileX;
        _endY = tileY;
        _isDragging = false;

        int minX = Math.Min(_startX, _endX);
        int minY = Math.Min(_startY, _endY);
        int maxX = Math.Max(_startX, _endX);
        int maxY = Math.Max(_startY, _endY);

        // Clamp to map bounds
        minX = Math.Max(0, minX);
        minY = Math.Max(0, minY);
        maxX = Math.Min(doc.MapWidth - 1, maxX);
        maxY = Math.Min(doc.MapHeight - 1, maxY);

        var changes = new List<TileChange>();
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int oldTile = doc.GetTile(layerIndex, x, y);
                if (oldTile == selectedTileId) continue;

                doc.SetTile(layerIndex, x, y, selectedTileId);
                changes.Add(new TileChange(x, y, oldTile, selectedTileId));
            }
        }

        return changes.ToArray();
    }
}
