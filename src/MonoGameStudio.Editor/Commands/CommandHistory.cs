using MonoGameStudio.Core.Logging;

namespace MonoGameStudio.Editor.Commands;

public class CommandHistory
{
    private readonly List<ICommand> _undoStack = new();
    private readonly List<ICommand> _redoStack = new();
    private const int MaxCommands = 100;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(ICommand command)
    {
        command.Execute();
        _undoStack.Add(command);
        _redoStack.Clear(); // Branch pruning

        if (_undoStack.Count > MaxCommands)
            _undoStack.RemoveAt(0);

        Log.Info($"Executed: {command.Description}");
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        command.Undo();
        _redoStack.Add(command);

        Log.Info($"Undone: {command.Description}");
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        command.Execute();
        _undoStack.Add(command);

        Log.Info($"Redone: {command.Description}");
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
