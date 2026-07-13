using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;

namespace MouseLock.Input;

internal static class ModifierState
{
    public static unsafe bool IsHeld(UIInputData* inputData, ReleaseModifierKey modifier)
    {
        var io = ImGui.GetIO();

        return modifier switch
        {
            ReleaseModifierKey.Alt => (inputData->CurrentKeyModifier & KeyModifierFlag.Alt) != 0 || io.KeyAlt,
            ReleaseModifierKey.Control => (inputData->CurrentKeyModifier & KeyModifierFlag.Ctrl) != 0 || io.KeyCtrl,
            ReleaseModifierKey.Shift => (inputData->CurrentKeyModifier & KeyModifierFlag.Shift) != 0 || io.KeyShift,
            _ => false,
        };
    }

    public static unsafe bool IsSatisfied(UIInputData* inputData, ReleaseModifierKey modifier)
        => modifier == ReleaseModifierKey.None || IsHeld(inputData, modifier);

    public static unsafe ReleaseModifierKey GetActiveLayer(UIInputData* inputData)
    {
        if (IsHeld(inputData, ReleaseModifierKey.Shift))
        {
            return ReleaseModifierKey.Shift;
        }

        if (IsHeld(inputData, ReleaseModifierKey.Control))
        {
            return ReleaseModifierKey.Control;
        }

        if (IsHeld(inputData, ReleaseModifierKey.Alt))
        {
            return ReleaseModifierKey.Alt;
        }

        return ReleaseModifierKey.None;
    }
}
