using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Actions;
using MouseLock.Configuration;

namespace MouseLock.MouseLook.Activation;

internal static class ReleaseModifierState
{
    public static unsafe bool IsHeld(UIInputData* inputData)
    {
        var modifier = PluginState.Config.General.ReleaseModifier;
        return InputModifierState.IsHeld(inputData, modifier);
    }
}
