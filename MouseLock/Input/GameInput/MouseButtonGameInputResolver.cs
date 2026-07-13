using FFXIVClientStructs.FFXIV.Client.System.Input;
using MouseLock.Configuration;

namespace MouseLock.Input.GameInput;

internal static class MouseButtonGameInputResolver
{
    public static bool TryResolveGameInput(CuratedGameInput input, out InputId inputId)
    {
        inputId = input switch
        {
            CuratedGameInput.TabTargetNext => InputId.TARGET_NEXT,
            CuratedGameInput.TabTargetPrevious => InputId.TARGET_PREV,
            CuratedGameInput.TargetNearestEnemy => InputId.TARGET_CLOSEST_ENEMY,
            CuratedGameInput.MoveForward => InputId.MOVE_FORE,
            CuratedGameInput.MoveBack => InputId.MOVE_BACK,
            CuratedGameInput.MoveLeft => InputId.MOVE_LEFT,
            CuratedGameInput.MoveRight => InputId.MOVE_RIGHT,
            CuratedGameInput.StrafeLeft => InputId.MOVE_STRIFE_L,
            CuratedGameInput.StrafeRight => InputId.MOVE_STRIFE_R,
            CuratedGameInput.Jump => InputId.JUMP,
            CuratedGameInput.Autorun => InputId.AUTORUN_KEY,
            _ => (InputId)(-1),
        };

        return inputId != (InputId)(-1);
    }

    public static bool IsHeldInput(CuratedGameInput input)
        => input is CuratedGameInput.MoveForward
            or CuratedGameInput.MoveBack
            or CuratedGameInput.MoveLeft
            or CuratedGameInput.MoveRight
            or CuratedGameInput.StrafeLeft
            or CuratedGameInput.StrafeRight;
}
