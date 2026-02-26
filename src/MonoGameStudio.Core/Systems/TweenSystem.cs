using MonoGameStudio.Core.Data;

namespace MonoGameStudio.Core.Systems;

public class Tween
{
    public float From;
    public float To;
    public float Duration;
    public EaseType Ease;
    public Action<float>? OnUpdate;
    public Action? OnComplete;
    public float Elapsed;
    public bool IsComplete;

    public float CurrentValue => From + (To - From) * Easing.Evaluate(Ease, Elapsed / Duration);
}

public class TweenSystem
{
    private readonly List<Tween> _tweens = new();
    private readonly List<Tween> _toAdd = new();

    public Tween To(float from, float to, float duration, EaseType ease, Action<float>? onUpdate = null, Action? onComplete = null)
    {
        var tween = new Tween
        {
            From = from,
            To = to,
            Duration = Math.Max(0.001f, duration),
            Ease = ease,
            OnUpdate = onUpdate,
            OnComplete = onComplete,
        };
        _toAdd.Add(tween);
        return tween;
    }

    public void Cancel(Tween tween)
    {
        tween.IsComplete = true;
    }

    public void CancelAll()
    {
        foreach (var t in _tweens) t.IsComplete = true;
        _toAdd.Clear();
    }

    public int ActiveCount => _tweens.Count;

    public void Update(float deltaTime)
    {
        if (_toAdd.Count > 0)
        {
            _tweens.AddRange(_toAdd);
            _toAdd.Clear();
        }

        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            var tween = _tweens[i];
            if (tween.IsComplete)
            {
                _tweens.RemoveAt(i);
                continue;
            }

            tween.Elapsed += deltaTime;
            if (tween.Elapsed >= tween.Duration)
            {
                tween.Elapsed = tween.Duration;
                tween.IsComplete = true;
                tween.OnUpdate?.Invoke(tween.To);
                tween.OnComplete?.Invoke();
                _tweens.RemoveAt(i);
            }
            else
            {
                tween.OnUpdate?.Invoke(tween.CurrentValue);
            }
        }
    }
}
