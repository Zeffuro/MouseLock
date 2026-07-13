using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Compatibility;
using MouseLock.Configuration;
using MouseLock.Game;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookActivationRules(ChatTwoTypingState chatTwoTypingState)
{
    public unsafe bool ShouldLock(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        bool nativeTextInputActive = false)
    {
        if (!PluginState.Config.General.Enabled || !Service.ClientState.IsLoggedIn)
        {
            return false;
        }

        if (PluginState.ConfigWindow.IsOpen)
        {
            return false;
        }

        if (!inputData->CursorInputs.IsGameWindowFocused)
        {
            return false;
        }

        var conditions = PluginState.Config.General.Conditions;
        if (conditions.RequireCombat && !IsCombatOrAllowedCountdownActive())
        {
            return false;
        }

        if (conditions.DisableWhileTextInputActive && IsAnyTextInputActive(atkModule, nativeTextInputActive))
        {
            return false;
        }

        if (conditions.DisableWhenTalkAddonVisible && NativeUiState.IsAddonVisible("Talk"))
        {
            return false;
        }

        if (conditions.DisableWhenNativeAddonFocused && NativeUiState.IsBlockingAddonFocused())
        {
            return false;
        }

        if (conditions.DisableWhenNativeAddonHovered && NativeUiState.IsBlockingAddonHovered(inputData))
        {
            return false;
        }

        if (PluginState.Config.General.Compatibility.DisableDuringTPieRing &&
            TPieCompatibilityState.IsRingActive())
        {
            return false;
        }

        if (IsReleaseModifierHeld(inputData))
        {
            return false;
        }

        return true;
    }

    private static unsafe bool IsCombatOrAllowedCountdownActive()
    {
        var conditions = Conditions.Instance();
        if (conditions is not null && conditions->InCombat)
        {
            return true;
        }

        if (!PluginState.Config.General.Conditions.CountCountdownAsCombat)
        {
            return false;
        }

        var countdown = AgentCountDownSettingDialog.Instance();
        return countdown is not null && (countdown->Active || countdown->ShowingCountdown);
    }

    private unsafe bool IsAnyTextInputActive(AtkModule* atkModule, bool nativeTextInputActive)
    {
        if (nativeTextInputActive)
        {
            return true;
        }

        if (atkModule is not null && atkModule->IsTextInputActive())
        {
            return true;
        }

        if (ImGui.GetIO().WantTextInput)
        {
            return true;
        }

        return chatTwoTypingState.IsInputFocused;
    }

    private static unsafe bool IsReleaseModifierHeld(UIInputData* inputData)
    {
        var modifier = PluginState.Config.General.ReleaseModifier;
        var io = ImGui.GetIO();

        return modifier switch
        {
            ReleaseModifierKey.Alt => (inputData->CurrentKeyModifier & KeyModifierFlag.Alt) != 0 || io.KeyAlt,
            ReleaseModifierKey.Control => (inputData->CurrentKeyModifier & KeyModifierFlag.Ctrl) != 0 || io.KeyCtrl,
            ReleaseModifierKey.Shift => (inputData->CurrentKeyModifier & KeyModifierFlag.Shift) != 0 || io.KeyShift,
            ReleaseModifierKey.None => false,
            _ => false,
        };
    }

}
