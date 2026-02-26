using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Core.Systems;

public interface IScreenTransition
{
    float Duration { get; }
    void Draw(SpriteBatch spriteBatch, Texture2D? oldScene, Texture2D? newScene, float progress, Rectangle viewport);
}

public class FadeTransition : IScreenTransition
{
    public float Duration { get; set; } = 0.5f;

    public void Draw(SpriteBatch spriteBatch, Texture2D? oldScene, Texture2D? newScene, float progress, Rectangle viewport)
    {
        float t = Easing.Evaluate(EaseType.InOutSine, progress);

        if (oldScene != null)
            spriteBatch.Draw(oldScene, viewport, Color.White * (1f - t));

        if (newScene != null)
            spriteBatch.Draw(newScene, viewport, Color.White * t);
    }
}

public class SlideTransition : IScreenTransition
{
    public float Duration { get; set; } = 0.4f;
    public SlideDirection Direction { get; set; } = SlideDirection.Left;

    public void Draw(SpriteBatch spriteBatch, Texture2D? oldScene, Texture2D? newScene, float progress, Rectangle viewport)
    {
        float t = Easing.Evaluate(EaseType.InOutCubic, progress);
        var offset = Direction switch
        {
            SlideDirection.Left => new Vector2(-viewport.Width * t, 0),
            SlideDirection.Right => new Vector2(viewport.Width * t, 0),
            SlideDirection.Up => new Vector2(0, -viewport.Height * t),
            SlideDirection.Down => new Vector2(0, viewport.Height * t),
            _ => Vector2.Zero
        };

        if (oldScene != null)
            spriteBatch.Draw(oldScene, new Vector2(viewport.X, viewport.Y) + offset, Color.White);

        if (newScene != null)
        {
            var newOffset = Direction switch
            {
                SlideDirection.Left => offset + new Vector2(viewport.Width, 0),
                SlideDirection.Right => offset - new Vector2(viewport.Width, 0),
                SlideDirection.Up => offset + new Vector2(0, viewport.Height),
                SlideDirection.Down => offset - new Vector2(0, viewport.Height),
                _ => offset
            };
            spriteBatch.Draw(newScene, new Vector2(viewport.X, viewport.Y) + newOffset, Color.White);
        }
    }
}

public enum SlideDirection { Left, Right, Up, Down }

public enum TransitionState { None, FadingOut, Loading, FadingIn, Complete }

public class ScreenTransitionSystem
{
    private IScreenTransition? _transition;
    private TransitionState _state = TransitionState.None;
    private float _elapsed;
    private RenderTarget2D? _oldSceneSnapshot;
    private Action? _onMidpoint;
    private Action? _onComplete;

    public TransitionState State => _state;
    public bool IsTransitioning => _state != TransitionState.None && _state != TransitionState.Complete;

    public void StartTransition(IScreenTransition transition, GraphicsDevice graphicsDevice,
        RenderTarget2D? currentSceneRT, Action? onMidpoint = null, Action? onComplete = null)
    {
        _transition = transition;
        _onMidpoint = onMidpoint;
        _onComplete = onComplete;
        _elapsed = 0f;
        _state = TransitionState.FadingOut;

        // Snapshot current scene
        if (currentSceneRT != null)
        {
            _oldSceneSnapshot?.Dispose();
            _oldSceneSnapshot = new RenderTarget2D(graphicsDevice, currentSceneRT.Width, currentSceneRT.Height);
            graphicsDevice.SetRenderTarget(_oldSceneSnapshot);
            var sb = new SpriteBatch(graphicsDevice);
            sb.Begin();
            sb.Draw(currentSceneRT, Vector2.Zero, Color.White);
            sb.End();
            sb.Dispose();
            graphicsDevice.SetRenderTarget(null);
        }
    }

    public void Update(float deltaTime)
    {
        if (_transition == null || _state == TransitionState.None || _state == TransitionState.Complete) return;

        _elapsed += deltaTime;
        float halfDuration = _transition.Duration * 0.5f;

        switch (_state)
        {
            case TransitionState.FadingOut:
                if (_elapsed >= halfDuration)
                {
                    _state = TransitionState.Loading;
                    _onMidpoint?.Invoke();
                    _state = TransitionState.FadingIn;
                }
                break;

            case TransitionState.FadingIn:
                if (_elapsed >= _transition.Duration)
                {
                    _state = TransitionState.Complete;
                    _onComplete?.Invoke();
                    _oldSceneSnapshot?.Dispose();
                    _oldSceneSnapshot = null;
                    _transition = null;
                }
                break;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D? newSceneRT, Rectangle viewport)
    {
        if (_transition == null || !IsTransitioning) return;
        float progress = Math.Clamp(_elapsed / _transition.Duration, 0f, 1f);
        _transition.Draw(spriteBatch, _oldSceneSnapshot, newSceneRT, progress, viewport);
    }
}
