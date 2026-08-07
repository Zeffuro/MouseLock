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

    private MouseButtonGameInputBinding? _latchedLeftBinding;
    private MouseButtonGameInputBinding? _latchedRightBinding;
    private MouseButtonFlags _controlButtonsConsumedUntilRelease;

    public MouseButtonActionFrame Capture(UIInputData* inputData)
    {
        var pressedButtons = inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons;
        var heldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;
        var releasedButtons = inputData->CursorInputs.MouseButtonReleasedFlags & MouseLookButtons.PhysicalLookButtons;
        var actions = PluginState.Config.MouseActions;

        MouseButtonState GetButtonState(MouseButtonFlags button) => new(
            (pressedButtons & button) != 0,
            (heldButtons & button) != 0,
            (releasedButtons & button) != 0,
            AllowNewActions: false);

        var leftState = GetButtonState(MouseButtonFlags.LBUTTON);
        var rightState = GetButtonState(MouseButtonFlags.RBUTTON);

        var leftBinding = ResolveLatchedBinding(
            leftState,
            MouseButtonActionResolver.ResolveLeft(inputData, actions),
            ref _latchedLeftBinding);

        var rightBinding = ResolveLatchedBinding(
            rightState,
            MouseButtonActionResolver.ResolveRight(inputData, actions),
            ref _latchedRightBinding);

        var classicForwardHeld = actions.ClassicMouseMovementEnabled &&
                                 leftBinding.Kind == MouseButtonBindingKind.None &&
                                 rightBinding.Kind == MouseButtonBindingKind.None &&
                                 (leftState.Pressed || leftState.Held) &&
                                 (rightState.Pressed || rightState.Held);

        return new MouseButtonActionFrame(
            new MouseButtonActionState(MouseButtonFlags.LBUTTON, leftBinding, leftState),
            new MouseButtonActionState(MouseButtonFlags.RBUTTON, rightBinding, rightState),
            classicForwardHeld);
    }

    public MouseButtonActionResult Execute(
        UIInputData* inputData,
        MouseButtonActionFrame frame,
        bool allowGameplayActions,
        bool allowControlActions)
    {
        var activationChanged = false;

        if (UpdateButton(
                inputData,
                _leftButtonGameInputState,
                frame.Left,
                allowGameplayActions,
                allowControlActions))
        {
            _controlButtonsConsumedUntilRelease |= frame.Left.Button;
            activationChanged = true;
        }

        if (UpdateButton(
                inputData,
                _rightButtonGameInputState,
                frame.Right,
                allowGameplayActions,
                allowControlActions))
        {
            _controlButtonsConsumedUntilRelease |= frame.Right.Button;
            activationChanged = true;
        }

        var consumedButtons = _controlButtonsConsumedUntilRelease & frame.ActiveButtons;

        ClearActionAfterRelease(frame.Left);
        ClearActionAfterRelease(frame.Right);

        return new MouseButtonActionResult(consumedButtons, activationChanged);
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
        _latchedLeftBinding = null;
        _latchedRightBinding = null;
        _controlButtonsConsumedUntilRelease = MouseButtonFlags.None;
    }

    private static bool UpdateButton(
        UIInputData* inputData,
        ButtonGameInputState gameInputState,
        MouseButtonActionState action,
        bool allowGameplayActions,
        bool allowControlActions)
    {
        var binding = action.Binding;
        var button = action.State;
        binding.Clamp();

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.HotbarSlot:
                gameInputState.AdvanceRelease(inputData);
                if (allowGameplayActions && button.Pressed)
                {
                    HotbarSlotInterop.Execute(binding.Hotbar, binding.Slot);
                }

                return false;

            case MouseButtonBindingKind.GameInput:
                gameInputState.Update(
                    inputData,
                    binding.GameInput,
                    button with { AllowNewActions = allowGameplayActions });
                return false;

            case MouseButtonBindingKind.ToggleMouseLock:
                gameInputState.AdvanceRelease(inputData);
                if (allowControlActions && button.Pressed)
                {
                    MouseLockStateController.ToggleEnabled();
                    return true;
                }

                return false;

            case MouseButtonBindingKind.OpenConfig:
                gameInputState.AdvanceRelease(inputData);
                if (allowControlActions && button.Pressed)
                {
                    PluginState.ConfigWindow.Toggle();
                    return true;
                }

                return false;

            default:
                gameInputState.AdvanceRelease(inputData);
                return false;
        }
    }

    private void ClearActionAfterRelease(MouseButtonActionState action)
    {
        if (action.State.Released ||
            (!action.State.Pressed && !action.State.Held))
        {
            _controlButtonsConsumedUntilRelease &= ~action.Button;
        }
    }

    private static MouseButtonGameInputBinding ResolveLatchedBinding(
        MouseButtonState button,
        MouseButtonGameInputBinding resolvedBinding,
        ref MouseButtonGameInputBinding? latchedBinding)
    {
        if (button.Pressed || (button.Held && latchedBinding is null))
        {
            latchedBinding = resolvedBinding;
        }

        var binding = latchedBinding ?? resolvedBinding;

        if (button.Released || (!button.Pressed && !button.Held))
        {
            latchedBinding = null;
        }

        return binding;
    }
}
