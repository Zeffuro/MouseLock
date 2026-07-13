using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Hotbars;
using MouseLock.MouseLook;

namespace MouseLock.Actions;

internal sealed unsafe class MouseButtonActionExecutor
{
    private readonly ButtonGameInputState _leftButtonGameInputState = new();
    private readonly ButtonGameInputState _rightButtonGameInputState = new();

    public void Update(UIInputData* inputData, bool allowNewActions)
    {
        var pressedButtons = inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons;
        var heldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;
        var releasedButtons = inputData->CursorInputs.MouseButtonReleasedFlags & MouseLookButtons.PhysicalLookButtons;
        var actions = PluginState.Config.General.MouseActions;

        UpdateButton(
            inputData,
            _leftButtonGameInputState,
            MouseButtonFlags.LBUTTON,
            actions.LeftButton,
            pressedButtons,
            heldButtons,
            releasedButtons,
            allowNewActions);

        UpdateButton(
            inputData,
            _rightButtonGameInputState,
            MouseButtonFlags.RBUTTON,
            actions.RightButton,
            pressedButtons,
            heldButtons,
            releasedButtons,
            allowNewActions);
    }

    public void ReleaseAll(UIInputData* inputData)
    {
        _leftButtonGameInputState.Clear(inputData);
        _rightButtonGameInputState.Clear(inputData);
    }

    private static void UpdateButton(
        UIInputData* inputData,
        ButtonGameInputState gameInputState,
        MouseButtonFlags button,
        MouseButtonGameInputBinding binding,
        MouseButtonFlags pressedButtons,
        MouseButtonFlags heldButtons,
        MouseButtonFlags releasedButtons,
        bool allowNewActions)
    {
        binding.Clamp();

        var buttonPressed = (pressedButtons & button) != 0;
        var buttonHeld = (heldButtons & button) != 0;
        var buttonReleased = (releasedButtons & button) != 0;

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.HotbarSlot:
                gameInputState.AdvanceRelease(inputData);
                if (allowNewActions && buttonPressed)
                {
                    HotbarSlotInterop.Execute(binding.Hotbar, binding.Slot);
                }

                return;

            case MouseButtonBindingKind.GameInput:
                gameInputState.Update(inputData, binding.GameInput, buttonPressed, buttonHeld, buttonReleased, allowNewActions);
                return;

            default:
                gameInputState.AdvanceRelease(inputData);
                return;
        }
    }
}
