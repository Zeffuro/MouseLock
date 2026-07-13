using FFXIVClientStructs.FFXIV.Client.System.Input;
using GameInputManager = FFXIVClientStructs.FFXIV.Client.Game.Control.InputManager;

namespace MouseLock.MouseLook;

internal static class MouseLookButtons
{
    public const MouseButtonFlags VirtualLookButton = MouseButtonFlags.RBUTTON;
    public const MouseButtonFlags PhysicalLookButtons = MouseButtonFlags.LBUTTON | MouseButtonFlags.RBUTTON;
    public const GameInputManager.MouseButtonHoldState VirtualDragState = GameInputManager.MouseButtonHoldState.Right;
}
