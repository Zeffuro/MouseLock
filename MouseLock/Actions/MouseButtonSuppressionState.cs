using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.MouseLook;

namespace MouseLock.Actions;

internal static class MouseButtonSuppressionState
{
    private const MouseButtonFlags SuppressedButtons = MouseLookButtons.PhysicalLookButtons;

    public static unsafe void Apply(UIInputData* inputData)
    {
        ClearMouseButtons(&inputData->CursorInputs);
        ClearMouseButtons(&inputData->UIFilteredCursorInputs);

        inputData->CurrentMouseDragButtons &= unchecked((byte)~(byte)SuppressedButtons);
        inputData->UIFilteredCursorInputsButtonsChanged = true;
    }

    private static unsafe void ClearMouseButtons(CursorInputData* cursorInputs)
    {
        const MouseButtonFlags mask = ~SuppressedButtons;

        cursorInputs->MouseButtonHeldFlags &= mask;
        cursorInputs->MouseButtonPressedFlags &= mask;
        cursorInputs->MouseButtonReleasedFlags &= mask;
        cursorInputs->MouseButtonHeldThrottledFlags &= mask;
    }
}
