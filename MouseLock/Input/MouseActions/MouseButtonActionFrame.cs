using FFXIVClientStructs.FFXIV.Client.System.Input;
using MouseLock.Configuration;
using MouseLock.Input;

namespace MouseLock.Input.MouseActions;

internal readonly record struct MouseButtonActionFrame(
    MouseButtonActionState Left,
    MouseButtonActionState Right,
    bool ClassicForwardHeld)
{
    public MouseButtonFlags TemporaryReleaseButtonsHeld
        => GetButtons(MouseButtonBindingKind.TemporaryRelease, heldOnly: true);

    public MouseButtonFlags TemporaryReleaseButtonsActive
        => GetButtons(MouseButtonBindingKind.TemporaryRelease, heldOnly: false);

    public MouseButtonFlags ActiveButtons
    {
        get
        {
            var buttons = MouseButtonFlags.None;

            if (IsActive(Left.State))
            {
                buttons |= Left.Button;
            }

            if (IsActive(Right.State))
            {
                buttons |= Right.Button;
            }

            return buttons;
        }
    }

    public MouseButtonFlags PressedOrHeldButtons
    {
        get
        {
            var buttons = MouseButtonFlags.None;

            if (Left.State.Pressed || Left.State.Held)
            {
                buttons |= Left.Button;
            }

            if (Right.State.Pressed || Right.State.Held)
            {
                buttons |= Right.Button;
            }

            return buttons;
        }
    }

    public bool HasPressedControlAction
        => IsPressedControlAction(Left) || IsPressedControlAction(Right);

    private MouseButtonFlags GetButtons(MouseButtonBindingKind kind, bool heldOnly)
    {
        var buttons = MouseButtonFlags.None;

        if (Left.Binding.Kind == kind &&
            (heldOnly ? Left.State.Held : IsActive(Left.State)))
        {
            buttons |= Left.Button;
        }

        if (Right.Binding.Kind == kind &&
            (heldOnly ? Right.State.Held : IsActive(Right.State)))
        {
            buttons |= Right.Button;
        }

        return buttons;
    }

    private static bool IsActive(MouseButtonState state)
        => state.Pressed || state.Held || state.Released;

    private static bool IsPressedControlAction(MouseButtonActionState action)
        => action.State.Pressed &&
           action.Binding.Kind is MouseButtonBindingKind.ToggleMouseLock
               or MouseButtonBindingKind.OpenConfig;
}

internal readonly record struct MouseButtonActionState(
    MouseButtonFlags Button,
    MouseButtonGameInputBinding Binding,
    MouseButtonState State);

internal readonly record struct MouseButtonActionResult(
    MouseButtonFlags ConsumedButtons,
    bool ActivationChanged);
