using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;

namespace MouseLock.Actions;

internal static class MouseButtonActionResolver
{
    public static unsafe MouseButtonGameInputBinding ResolveLeft(UIInputData* inputData, MouseActionSettings actions)
        => Resolve(
            inputData,
            actions.LeftButton,
            actions.LeftAltButton,
            actions.LeftControlButton,
            actions.LeftShiftButton);

    public static unsafe MouseButtonGameInputBinding ResolveRight(UIInputData* inputData, MouseActionSettings actions)
        => Resolve(
            inputData,
            actions.RightButton,
            actions.RightAltButton,
            actions.RightControlButton,
            actions.RightShiftButton);

    private static unsafe MouseButtonGameInputBinding Resolve(
        UIInputData* inputData,
        MouseButtonGameInputBinding defaultBinding,
        MouseButtonGameInputBinding altBinding,
        MouseButtonGameInputBinding controlBinding,
        MouseButtonGameInputBinding shiftBinding)
    {
        var activeLayer = InputModifierState.GetActiveLayer(inputData);
        var layeredBinding = activeLayer switch
        {
            ReleaseModifierKey.Shift => shiftBinding,
            ReleaseModifierKey.Control => controlBinding,
            ReleaseModifierKey.Alt => altBinding,
            _ => defaultBinding,
        };

        return layeredBinding.Kind == MouseButtonBindingKind.None
            ? defaultBinding
            : layeredBinding;
    }
}
