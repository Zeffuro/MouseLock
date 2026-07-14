using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Hotbars;
using MouseLock.Input;
using MouseLock.Input.GameInput;
using MouseLock.MouseLook;

namespace MouseLock.Input.MouseActions;

internal sealed unsafe class MouseButtonActionExecutor
{
    private readonly ButtonGameInputState _leftButtonGameInputState = new();
    private readonly ButtonGameInputState _rightButtonGameInputState = new();

    public void Update(UIInputData* inputData, bool allowNewActions)
    {
        var pressedButtons = inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons;
        var heldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;
        var releasedButtons = inputData->CursorInputs.MouseButtonReleasedFlags & MouseLookButtons.PhysicalLookButtons;
        var actions = PluginState.Config.MouseActions;

        MouseButtonState GetButtonState(MouseButtonFlags button) => new(
            (pressedButtons & button) != 0,
            (heldButtons & button) != 0,
            (releasedButtons & button) != 0,
            allowNewActions);

        UpdateButton(
            inputData,
            _leftButtonGameInputState,
            MouseButtonActionResolver.ResolveLeft(inputData, actions),
            GetButtonState(MouseButtonFlags.LBUTTON));

        UpdateButton(
            inputData,
            _rightButtonGameInputState,
            MouseButtonActionResolver.ResolveRight(inputData, actions),
            GetButtonState(MouseButtonFlags.RBUTTON));
    }

    public void ReleaseAll(UIInputData* inputData)
    {
        _leftButtonGameInputState.Clear(inputData);
        _rightButtonGameInputState.Clear(inputData);
    }

    public void EmergencyReleaseAll()
    {
        _leftButtonGameInputState.EmergencyClear();
        _rightButtonGameInputState.EmergencyClear();
    }

    private static void UpdateButton(
        UIInputData* inputData,
        ButtonGameInputState gameInputState,
        MouseButtonGameInputBinding binding,
        MouseButtonState button)
    {
        binding.Clamp();

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.HotbarSlot:
                gameInputState.AdvanceRelease(inputData);
                if (button.AllowNewActions && button.Pressed)
                {
                    HotbarSlotInterop.Execute(binding.Hotbar, binding.Slot);
                }

                return;

            case MouseButtonBindingKind.GameInput:
                gameInputState.Update(
                    inputData,
                    binding.GameInput,
                    button);
                return;

            case MouseButtonBindingKind.ToggleMouseLock:
                gameInputState.AdvanceRelease(inputData);
                if (button.AllowNewActions && button.Pressed)
                {
                    MouseLockStateController.ToggleEnabled();
                }

                return;

            case MouseButtonBindingKind.OpenConfig:
                gameInputState.AdvanceRelease(inputData);
                if (button.AllowNewActions && button.Pressed)
                {
                    PluginState.ConfigWindow.Toggle();
                }

                return;

            default:
                gameInputState.AdvanceRelease(inputData);
                return;
        }
    }
}
