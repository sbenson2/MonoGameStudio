using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Editor.Commands;

/// <summary>
/// Undo/redo command for painting a single tile.
/// </summary>
public class PaintTileCommand : ICommand
{
    private readonly TilemapDocument _doc;
    private readonly int _layerIndex;
    private readonly int _x;
    private readonly int _y;
    private readonly int _oldTileId;
    private readonly int _newTileId;

    public string Description => $"Paint tile ({_x}, {_y})";

    public PaintTileCommand(TilemapDocument doc, int layerIndex, int x, int y, int newTileId)
    {
        _doc = doc;
        _layerIndex = layerIndex;
        _x = x;
        _y = y;
        _newTileId = newTileId;
        _oldTileId = doc.GetTile(layerIndex, x, y);
    }

    public void Execute()
    {
        _doc.SetTile(_layerIndex, _x, _y, _newTileId);
    }

    public void Undo()
    {
        _doc.SetTile(_layerIndex, _x, _y, _oldTileId);
    }
}

/// <summary>
/// Undo/redo command for painting multiple tiles in a batch (e.g., from a drag stroke or rect fill).
/// Stores per-tile old/new values for precise undo.
/// </summary>
public class PaintTilesCommand : ICommand
{
    private readonly TilemapDocument _doc;
    private readonly int _layerIndex;
    private readonly TileChange[] _changes;

    public string Description { get; }

    public PaintTilesCommand(TilemapDocument doc, int layerIndex, TileChange[] changes, string description = "Paint tiles")
    {
        _doc = doc;
        _layerIndex = layerIndex;
        _changes = changes;
        Description = description;
    }

    public void Execute()
    {
        foreach (var change in _changes)
            _doc.SetTile(_layerIndex, change.X, change.Y, change.NewTileId);
    }

    public void Undo()
    {
        foreach (var change in _changes)
            _doc.SetTile(_layerIndex, change.X, change.Y, change.OldTileId);
    }
}

/// <summary>
/// Undo/redo command for a flood-fill operation.
/// Records all tiles changed by the fill for full undo support.
/// </summary>
public class FillTilesCommand : ICommand
{
    private readonly TilemapDocument _doc;
    private readonly int _layerIndex;
    private readonly TileChange[] _changes;

    public string Description => $"Fill tiles ({_changes.Length} tiles)";

    public FillTilesCommand(TilemapDocument doc, int layerIndex, TileChange[] changes)
    {
        _doc = doc;
        _layerIndex = layerIndex;
        _changes = changes;
    }

    public void Execute()
    {
        foreach (var change in _changes)
            _doc.SetTile(_layerIndex, change.X, change.Y, change.NewTileId);
    }

    public void Undo()
    {
        foreach (var change in _changes)
            _doc.SetTile(_layerIndex, change.X, change.Y, change.OldTileId);
    }
}

/// <summary>
/// Records a single tile change with its position and old/new values.
/// </summary>
public struct TileChange
{
    public int X;
    public int Y;
    public int OldTileId;
    public int NewTileId;

    public TileChange(int x, int y, int oldTileId, int newTileId)
    {
        X = x;
        Y = y;
        OldTileId = oldTileId;
        NewTileId = newTileId;
    }
}
