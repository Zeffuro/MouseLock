using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace MouseLock.Targeting;

internal static class ReticleDepth
{
    internal static unsafe bool TryGet(float distance, out float depth)
    {
        depth = 0;
        var manager = Manager.Instance();
        var camera = manager == null ? null : manager->Views[(int)Manager.RenderViews.Main].SubViews[12].Camera;
        return camera != null && TryCalculate(camera->ProjectionMatrix, distance, out depth);
    }

    internal static bool TryCalculate(Matrix4x4 projection, float distance, out float depth)
    {
        depth = 0;
        if (!float.IsFinite(distance) || distance <= 0) return false;

        var projected = Vector4.Transform(new Vector4(0, 0, -distance, 1), projection);
        if (!float.IsFinite(projected.Z) || !float.IsFinite(projected.W) || projected.W <= 0) return false;

        var value = projected.Z / projected.W;
        if (!float.IsFinite(value)) return false;
        depth = Math.Clamp(value, 0, 1);
        return true;
    }
}
