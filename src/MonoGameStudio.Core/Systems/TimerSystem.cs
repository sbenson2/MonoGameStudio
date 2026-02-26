namespace MonoGameStudio.Core.Systems;

public abstract class YieldInstruction { }

public class WaitForSeconds : YieldInstruction
{
    public float Duration;
    public float Elapsed;
    public WaitForSeconds(float seconds) { Duration = seconds; }
}

public class WaitForFrames : YieldInstruction
{
    public int Frames;
    public int Waited;
    public WaitForFrames(int frames) { Frames = frames; }
}

public class WaitUntil : YieldInstruction
{
    public Func<bool> Predicate;
    public WaitUntil(Func<bool> predicate) { Predicate = predicate; }
}

public class TimerSystem
{
    private readonly List<TimerEntry> _timers = new();
    private readonly List<TimerEntry> _toAdd = new();
    private readonly List<CoroutineEntry> _coroutines = new();
    private readonly List<CoroutineEntry> _coroutinesToAdd = new();

    public void After(float delay, Action callback)
    {
        _toAdd.Add(new TimerEntry { Delay = delay, Callback = callback, RepeatCount = 1 });
    }

    public void Every(float interval, Action callback, int count = -1)
    {
        _toAdd.Add(new TimerEntry { Delay = interval, Interval = interval, Callback = callback, RepeatCount = count });
    }

    public void StartCoroutine(IEnumerator<YieldInstruction> coroutine)
    {
        _coroutinesToAdd.Add(new CoroutineEntry { Enumerator = coroutine });
    }

    public void CancelAll()
    {
        _timers.Clear();
        _toAdd.Clear();
        _coroutines.Clear();
        _coroutinesToAdd.Clear();
    }

    public void Update(float deltaTime)
    {
        // Timers
        if (_toAdd.Count > 0) { _timers.AddRange(_toAdd); _toAdd.Clear(); }

        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var timer = _timers[i];
            timer.Elapsed += deltaTime;

            if (timer.Elapsed >= timer.Delay)
            {
                timer.Callback?.Invoke();
                timer.FireCount++;

                if (timer.RepeatCount > 0 && timer.FireCount >= timer.RepeatCount)
                {
                    _timers.RemoveAt(i);
                }
                else
                {
                    timer.Elapsed -= timer.Interval > 0 ? timer.Interval : timer.Delay;
                }
            }
        }

        // Coroutines
        if (_coroutinesToAdd.Count > 0) { _coroutines.AddRange(_coroutinesToAdd); _coroutinesToAdd.Clear(); }

        for (int i = _coroutines.Count - 1; i >= 0; i--)
        {
            var co = _coroutines[i];

            if (co.CurrentYield == null)
            {
                if (!co.Enumerator.MoveNext())
                {
                    _coroutines.RemoveAt(i);
                    continue;
                }
                co.CurrentYield = co.Enumerator.Current;
            }

            bool yieldDone = CheckYield(co.CurrentYield, deltaTime);

            if (yieldDone)
            {
                co.CurrentYield = null;
            }
        }
    }

    private static bool CheckYield(YieldInstruction yield, float deltaTime)
    {
        switch (yield)
        {
            case WaitForSeconds wfs:
                wfs.Elapsed += deltaTime;
                return wfs.Elapsed >= wfs.Duration;
            case WaitForFrames wff:
                wff.Waited++;
                return wff.Waited >= wff.Frames;
            case WaitUntil wu:
                return wu.Predicate();
            default:
                return true;
        }
    }

    private class TimerEntry
    {
        public float Delay;
        public float Interval;
        public Action? Callback;
        public int RepeatCount; // -1 = infinite
        public float Elapsed;
        public int FireCount;
    }

    private class CoroutineEntry
    {
        public IEnumerator<YieldInstruction> Enumerator = null!;
        public YieldInstruction? CurrentYield;
    }
}
