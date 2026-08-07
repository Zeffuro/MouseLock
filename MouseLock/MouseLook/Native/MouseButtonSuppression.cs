using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MouseLock.MouseLook.Native;

internal static class MouseButtonSuppression
{
    public static unsafe void Apply(UIInputData* inputData, MouseButtonFlags buttons)
    {
        if (buttons == MouseButtonFlags.None)
        {
            return;
        }

        inputData->CursorInputs.Clear(false, buttons);
        inputData->UIFilteredCursorInputs.Clear(false, buttons);

        inputData->CurrentMouseDragButtons &= unchecked((byte)~(byte)buttons);
        inputData->UIFilteredCursorInputsButtonsChanged = true;
    }
}
