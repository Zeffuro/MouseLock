using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MouseLock.Targeting;

internal unsafe struct CameraMotionTracker
{
    private Camera* _camera;
    private Vector2 _direction;

    internal bool Update(Camera* camera, float yaw, float pitch)
    {
        var direction = new Vector2(yaw, pitch);
        if (camera == null || camera != _camera)
        {
            _camera = camera;
            _direction = direction;
            return false;
        }

        var yawChange = MathF.Abs(MathF.IEEERemainder(yaw - _direction.X, MathF.Tau));
        if (yawChange < 0.0001f && MathF.Abs(pitch - _direction.Y) < 0.0001f)
        {
            return false;
        }

        _direction = direction;
        return true;
    }
}
