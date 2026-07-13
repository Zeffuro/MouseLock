using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.MouseLook;

namespace MouseLock.MouseLook.Native;

internal static class MouseButtonSuppression
{
    private const MouseButtonFlags SuppressedButtons = MouseLookButtons.PhysicalLookButtons;

    public static unsafe void Apply(UIInputData* inputData)
    {
        inputData->CursorInputs.Clear(false, SuppressedButtons);
        inputData->UIFilteredCursorInputs.Clear(false, SuppressedButtons);

        inputData->CurrentMouseDragButtons &= unchecked((byte)~(byte)SuppressedButtons);
        inputData->UIFilteredCursorInputsButtonsChanged = true;
    }
}
