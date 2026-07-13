using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Configuration;
using PluginSystem = MouseLock.System;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookActivationRules(ChatTwoTypingState chatTwoTypingState)
{
    public unsafe bool ShouldLock(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        bool nativeTextInputActive = false)
    {
        if (!PluginSystem.Config.General.Enabled || !Service.ClientState.IsLoggedIn)
        {
            return false;
        }

        if (PluginSystem.ConfigWindow.IsOpen)
        {
            return false;
        }

        if (!inputData->CursorInputs.IsGameWindowFocused)
        {
            return false;
        }

        var conditions = PluginSystem.Config.General.Conditions;
        if (conditions.RequireCombat && !IsCombatOrAllowedCountdownActive())
        {
            return false;
        }

        if (conditions.DisableWhileTextInputActive && IsAnyTextInputActive(atkModule, nativeTextInputActive))
        {
            return false;
        }

        if (conditions.DisableWhenNativeAddonFocused && IsNativeAddonFocused())
        {
            return false;
        }

        if (conditions.DisableWhenNativeAddonHovered && IsNativeAddonHovered(inputData))
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

        if (!PluginSystem.Config.General.Conditions.CountCountdownAsCombat)
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

    private static unsafe bool IsNativeAddonFocused()
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return false;
        }

        var unitManager = stage->RaptureAtkUnitManager;
        if (unitManager is null)
        {
            return false;
        }

        return unitManager->FocusedAddon is not null && IsBlockingAddon(unitManager->FocusedAddon);
    }

    private static unsafe bool IsNativeAddonHovered(UIInputData* inputData)
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return false;
        }

        if (stage->AtkInputManager is not null && stage->AtkInputManager->FocusedNode is not null)
        {
            return true;
        }

        var unitManager = stage->RaptureAtkUnitManager;
        if (unitManager is null)
        {
            return false;
        }

        var collision = new AddonCollision();
        unitManager->GetAddonCollision(
            &collision,
            (short)inputData->CursorInputs.PositionX,
            (short)inputData->CursorInputs.PositionY);

        return collision.UnitBase is not null && IsBlockingAddon(collision.UnitBase);
    }

    private static unsafe bool IsBlockingAddon(AtkUnitBase* addon)
    {
        return addon is not null &&
               addon->IsVisible &&
               addon->IsReady &&
               !addon->ShouldIgnoreInputs();
    }

    private static unsafe bool IsReleaseModifierHeld(UIInputData* inputData)
    {
        var modifier = PluginSystem.Config.General.ReleaseModifier;
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
