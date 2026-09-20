using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace MouseLock.Targeting;

internal static class TargetingPosition
{
    internal static Vector2 Calculate(int width, int height, float verticalOffset)
        => new(width / 2, Math.Clamp((int)(height * (0.5f - verticalOffset / 100)), 0, height - 1));

    internal static unsafe bool TryGet(float verticalOffset, out Vector2 point, out Vector2 screenSize)
    {
        point = screenSize = default;
        var device = Device.Instance();
        if (device == null || device->Width == 0 || device->Height == 0)
            return false;

        screenSize = new Vector2(device->Width, device->Height);
        point = Calculate((int)device->Width, (int)device->Height, verticalOffset);
        return true;
    }
}
