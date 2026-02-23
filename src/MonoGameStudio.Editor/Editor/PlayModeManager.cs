using MonoGameStudio.Core.Data;
using MonoGameStudio.Core.Logging;
using MonoGameStudio.Core.Serialization;
using MonoGameStudio.Core.World;

namespace MonoGameStudio.Editor.Editor;

public class PlayModeManager
{
    private readonly WorldManager _worldManager;
    private readonly EditorState _editorState;
    private string? _snapshot;

    public PlayModeManager(WorldManager worldManager, EditorState editorState)
    {
        _worldManager = worldManager;
        _editorState = editorState;
    }

    public void Play()
    {
        if (_editorState.Mode != EditorMode.Edit) return;

        _snapshot = SceneSerializer.Serialize(_worldManager);
        _editorState.Mode = EditorMode.Play;
        _editorState.ClearSelection();
        Log.Info("Entering Play mode");
    }

    public void Pause()
    {
        if (_editorState.Mode != EditorMode.Play) return;

        _editorState.Mode = EditorMode.Pause;
        Log.Info("Paused");
    }

    public void Stop()
    {
        if (_editorState.Mode == EditorMode.Edit) return;

        if (_snapshot != null)
        {
            SceneSerializer.Deserialize(_snapshot, _worldManager);
            _snapshot = null;
        }

        _editorState.Mode = EditorMode.Edit;
        _editorState.ClearSelection();
        Log.Info("Stopped — world restored");
    }
}
