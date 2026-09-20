using System;
using System.Numerics;

namespace MouseLock.Targeting;

internal static class AimTolerance
{
    internal const int SampleCount = 16;

    internal static void GetSamples(Vector2 center, float radius, Span<Vector2> samples)
    {
        const int pointsPerRing = SampleCount / 2;
        for (var i = 0; i < pointsPerRing; i++)
        {
            var (sin, cos) = MathF.SinCos(i * MathF.Tau / pointsPerRing);
            var far = new Vector2(cos, sin) * radius;
            var near = far / 2;
            samples[i] = center + new Vector2((int)near.X, (int)near.Y);
            samples[i + pointsPerRing] = center + new Vector2((int)far.X, (int)far.Y);
        }
    }
}
