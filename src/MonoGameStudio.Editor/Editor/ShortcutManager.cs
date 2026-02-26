using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace MonoGameStudio.Editor.Editor;

public class ShortcutManager
{
    public event Action? OnSave;
    public event Action? OnOpen;
    public event Action? OnNew;
    public event Action? OnUndo;
    public event Action? OnRedo;
    public event Action? OnDelete;
    public event Action? OnDuplicate;
    public event Action? OnRename;
    public event Action? OnSelectAll;
    public event Action? OnFocusSelected;
    public event Action? OnGizmoNone;
    public event Action? OnGizmoBoundingBox;
    public event Action? OnGizmoMove;
    public event Action? OnGizmoRotate;
    public event Action? OnGizmoScale;
    public event Action? OnCopy;
    public event Action? OnPaste;
    public event Action? OnGizmoCollider;

    private KeyboardState _prevKeyboard;

    public void Update(KeyboardState keyboard)
    {
        // Don't process shortcuts when ImGui wants keyboard
        if (ImGui.GetIO().WantCaptureKeyboard)
        {
            _prevKeyboard = keyboard;
            return;
        }

        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl) ||
                     keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (ctrl && JustPressed(keyboard, Keys.S)) OnSave?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.O)) OnOpen?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.N)) OnNew?.Invoke();
        if (ctrl && shift && JustPressed(keyboard, Keys.Z)) OnRedo?.Invoke();
        else if (ctrl && JustPressed(keyboard, Keys.Z)) OnUndo?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.D)) OnDuplicate?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.C)) OnCopy?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.V)) OnPaste?.Invoke();
        if (ctrl && JustPressed(keyboard, Keys.A)) OnSelectAll?.Invoke();

        if (JustPressed(keyboard, Keys.Delete)) OnDelete?.Invoke();
        if (JustPressed(keyboard, Keys.F2)) OnRename?.Invoke();
        if (JustPressed(keyboard, Keys.F)) OnFocusSelected?.Invoke();

        // Gizmo mode shortcuts (only without modifiers)
        if (!ctrl && !shift)
        {
            if (JustPressed(keyboard, Keys.Q)) OnGizmoNone?.Invoke();
            if (JustPressed(keyboard, Keys.W)) OnGizmoBoundingBox?.Invoke();
            if (JustPressed(keyboard, Keys.E)) OnGizmoRotate?.Invoke();
            if (JustPressed(keyboard, Keys.R)) OnGizmoScale?.Invoke();
            if (JustPressed(keyboard, Keys.T)) OnGizmoMove?.Invoke();
            if (JustPressed(keyboard, Keys.C)) OnGizmoCollider?.Invoke();
        }

        _prevKeyboard = keyboard;
    }

    private bool JustPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);
    }
}
