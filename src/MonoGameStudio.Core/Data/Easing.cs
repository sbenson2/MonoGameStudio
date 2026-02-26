namespace MonoGameStudio.Core.Data;

public enum EaseType
{
    Linear,
    InQuad, OutQuad, InOutQuad,
    InCubic, OutCubic, InOutCubic,
    InQuart, OutQuart, InOutQuart,
    InSine, OutSine, InOutSine,
    InExpo, OutExpo, InOutExpo,
    InBack, OutBack, InOutBack,
}

public static class Easing
{
    public static float Evaluate(EaseType type, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return type switch
        {
            EaseType.Linear => t,
            EaseType.InQuad => t * t,
            EaseType.OutQuad => t * (2f - t),
            EaseType.InOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
            EaseType.InCubic => t * t * t,
            EaseType.OutCubic => OutCubicCalc(t),
            EaseType.InOutCubic => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f,
            EaseType.InQuart => t * t * t * t,
            EaseType.OutQuart => OutQuartCalc(t),
            EaseType.InOutQuart => t < 0.5f ? 8f * t * t * t * t : InOutQuartCalc(t),
            EaseType.InSine => 1f - MathF.Cos(t * MathF.PI * 0.5f),
            EaseType.OutSine => MathF.Sin(t * MathF.PI * 0.5f),
            EaseType.InOutSine => 0.5f * (1f - MathF.Cos(MathF.PI * t)),
            EaseType.InExpo => t == 0f ? 0f : MathF.Pow(2f, 10f * (t - 1f)),
            EaseType.OutExpo => t == 1f ? 1f : 1f - MathF.Pow(2f, -10f * t),
            EaseType.InOutExpo => InOutExpoCalc(t),
            EaseType.InBack => InBackCalc(t),
            EaseType.OutBack => OutBackCalc(t),
            EaseType.InOutBack => InOutBackCalc(t),
            _ => t,
        };
    }

    private static float OutCubicCalc(float t) { float f = t - 1f; return f * f * f + 1f; }
    private static float OutQuartCalc(float t) { float f = t - 1f; return 1f - f * f * f * f; }
    private static float InOutQuartCalc(float t) { float f = t - 1f; return 1f - 8f * f * f * f * f; }

    private static float InOutExpoCalc(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        return t < 0.5f
            ? MathF.Pow(2f, 20f * t - 10f) * 0.5f
            : (2f - MathF.Pow(2f, -20f * t + 10f)) * 0.5f;
    }

    private static float InBackCalc(float t)
    {
        const float s = 1.70158f;
        return t * t * ((s + 1f) * t - s);
    }

    private static float OutBackCalc(float t)
    {
        const float s = 1.70158f;
        float f = t - 1f;
        return f * f * ((s + 1f) * f + s) + 1f;
    }

    private static float InOutBackCalc(float t)
    {
        const float s = 1.70158f * 1.525f;
        float p = t * 2f;
        if (p < 1f) return 0.5f * (p * p * ((s + 1f) * p - s));
        p -= 2f;
        return 0.5f * (p * p * ((s + 1f) * p + s) + 2f);
    }
}
