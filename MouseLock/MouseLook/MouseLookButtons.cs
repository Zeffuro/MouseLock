using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace MouseLock.MouseLook;

internal static class MouseLookButtons
{
    public const MouseButtonFlags VirtualLookButton = MouseButtonFlags.RBUTTON;
    public const MouseButtonFlags PhysicalLookButtons = MouseButtonFlags.LBUTTON | MouseButtonFlags.RBUTTON;
    public const InputManager.MouseButtonHoldState VirtualDragState = InputManager.MouseButtonHoldState.Right;
}
