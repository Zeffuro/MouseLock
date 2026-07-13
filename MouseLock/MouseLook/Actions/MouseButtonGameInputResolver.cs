using System;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using MouseLock.Configuration;

namespace MouseLock.MouseLook.Actions;

internal static class MouseButtonGameInputResolver
{
    private static InputId? Resolve(MouseButtonGameInputBinding binding)
    {
        binding.Clamp();

        return binding.Kind switch
        {
            MouseButtonBindingKind.None => null,
            MouseButtonBindingKind.GameInput => ResolveGameInput(binding.GameInput),
            MouseButtonBindingKind.HotbarSlot => ResolveHotbarSlot(binding.Hotbar, binding.Slot),
            _ => null,
        };
    }

    public static string GetDisplayName(MouseButtonGameInputBinding binding)
    {
        var input = Resolve(binding);
        return input is { } value ? value.ToString() : "None";
    }

    public static InputId ResolveGameInput(CuratedGameInput input)
        => input switch
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
            _ => InputId.TAB_NEXT,
        };

    public static bool IsHeldInput(CuratedGameInput input)
        => input is CuratedGameInput.MoveForward
            or CuratedGameInput.MoveBack
            or CuratedGameInput.MoveLeft
            or CuratedGameInput.MoveRight
            or CuratedGameInput.StrafeLeft
            or CuratedGameInput.StrafeRight;

    private static InputId ResolveHotbarSlot(int hotbar, int slot)
    {
        hotbar = Math.Clamp(hotbar, 1, 10);
        slot = Math.Clamp(slot, 1, 12);

        var baseInput = hotbar switch
        {
            1 => InputId.HOTBAR_1_1,
            2 => InputId.HOTBAR_2_1,
            3 => InputId.HOTBAR_3_1,
            4 => InputId.HOTBAR_4_1,
            5 => InputId.HOTBAR_5_1,
            6 => InputId.HOTBAR_6_1,
            7 => InputId.HOTBAR_7_1,
            8 => InputId.HOTBAR_8_1,
            9 => InputId.HOTBAR_9_1,
            _ => InputId.HOTBAR_10_1,
        };

        return (InputId)((int)baseInput + slot - 1);
    }
}
