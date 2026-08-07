using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Game;
using MouseLock.Input;
using MouseLock.Integrations;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookActivationRules(TextInputMonitor textInputMonitor)
{
    public unsafe MouseLookDecision Evaluate(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        MouseButtonFlags temporaryReleaseButtons = MouseButtonFlags.None)
    {
        if (!PluginState.Config.General.Enabled)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.PluginDisabled);
        }

        if (!Service.ClientState.IsLoggedIn)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.LoggedOut);
        }

        if (PluginState.ConfigWindow.IsOpen)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.ConfigWindowOpen);
        }

        if (PluginState.FirstRunWindow is { IsOpen: true })
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.FirstRunWindowOpen);
        }

        if (inputData is null)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
        }

        if (!inputData->CursorInputs.IsGameWindowFocused)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.GameUnfocused);
        }

        var conditions = PluginState.Config.Activation.Conditions;
        if (conditions.DisableDuringCutscenes && IsCutsceneActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.Cutscene);
        }

        if (conditions.DisableDuringGpose && Service.ClientState.IsGPosing)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.Gpose);
        }

        if (conditions.DisableDuringCrafting && IsCraftingActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.Crafting);
        }

        if (conditions.DisableDuringGathering && IsGatheringActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.Gathering);
        }

        if (conditions.DisableDuringGroundTargeting && IsGroundTargetingActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.GroundTargeting);
        }

        if (conditions.DisableDuringHousingPlacement && Service.Condition[ConditionFlag.UsingHousingFunctions])
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.HousingPlacement);
        }

        if (conditions.DisableWhileMounted && IsMounted())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.Mounted);
        }

        if (conditions.DisableDuringTerritoryTransitions && IsBetweenAreas())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TerritoryTransition);
        }

        if (conditions.RequireCombat && !IsCombatOrAllowedCountdownActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.CombatRequired);
        }

        if (conditions.DisableWhileTextInputActive && textInputMonitor.IsTextInputActive(atkModule))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TextInput);
        }

        if (conditions.DisableWhenTalkAddonVisible && NativeUiState.IsAddonVisible("Talk"))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TalkAddon);
        }

        if (conditions.DisableWhenDalamudWindowFocused && DalamudUiState.IsBlockingUiActive(conditions))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.DalamudWindowFocused);
        }

        if (conditions.DisableWhenNativeAddonFocused &&
            NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName) &&
            !conditions.IsFocusedAddonIgnored(focusedAddonName))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.NativeAddonFocused);
        }

        if (conditions.DisableWhenNativeAddonHovered &&
            NativeUiState.TryGetHoveredBlockingAddonName(inputData, out var hoveredAddonName) &&
            !conditions.IsHoveredAddonIgnored(hoveredAddonName))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.NativeAddonHovered);
        }

        if (PluginState.Config.Compatibility.DisableDuringTPieRing &&
            TPieIntegration.IsRingActive)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TPie);
        }

        if (SuspensionRegistry.IsSuspended)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.ExternalSuspension);
        }

        if (ReleaseModifierState.IsHeld(inputData))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.ReleaseModifier);
        }

        if (temporaryReleaseButtons != MouseButtonFlags.None)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.MouseActionRelease);
        }

        return MouseLookDecision.Allow();
    }

    public unsafe bool CanRunControlActionWhileDisabled(
        UIInputData* inputData,
        AtkModule* atkModule = null)
    {
        if (!Service.ClientState.IsLoggedIn ||
            PluginState.ConfigWindow.IsOpen ||
            PluginState.FirstRunWindow is { IsOpen: true } ||
            inputData is null ||
            !inputData->CursorInputs.IsGameWindowFocused)
        {
            return false;
        }

        var conditions = PluginState.Config.Activation.Conditions;
        if (textInputMonitor.IsTextInputActive(atkModule) ||
            NativeUiState.IsAddonVisible("Talk") ||
            DalamudUiState.IsBlockingUiActive(conditions))
        {
            return false;
        }

        if (NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName) &&
            !conditions.IsFocusedAddonIgnored(focusedAddonName))
        {
            return false;
        }

        if (NativeUiState.TryGetHoveredBlockingAddonName(inputData, out var hoveredAddonName) &&
            !conditions.IsHoveredAddonIgnored(hoveredAddonName))
        {
            return false;
        }

        if (PluginState.Config.Compatibility.DisableDuringTPieRing &&
            TPieIntegration.IsRingActive)
        {
            return false;
        }

        return !SuspensionRegistry.IsSuspended;
    }

    private static unsafe bool IsCombatOrAllowedCountdownActive()
    {
        var conditions = Conditions.Instance();
        if (conditions is not null && conditions->InCombat)
        {
            return true;
        }

        if (!PluginState.Config.Activation.Conditions.CountCountdownAsCombat)
        {
            return false;
        }

        var countdown = AgentCountDownSettingDialog.Instance();
        return countdown is not null && (countdown->Active || countdown->ShowingCountdown);
    }

    private static bool IsCutsceneActive()
        => Service.Condition.Any(
            ConditionFlag.OccupiedInCutSceneEvent,
            ConditionFlag.WatchingCutscene,
            ConditionFlag.WatchingCutscene78);

    private static bool IsCraftingActive()
        => Service.Condition.Any(
            ConditionFlag.Crafting,
            ConditionFlag.ExecutingCraftingAction,
            ConditionFlag.PreparingToCraft);

    private static bool IsGatheringActive()
        => Service.Condition.Any(
            ConditionFlag.Gathering,
            ConditionFlag.ExecutingGatheringAction);

    private static unsafe bool IsGroundTargetingActive()
    {
        var actionManager = ActionManager.Instance();
        return actionManager is not null && actionManager->AreaTargetingActionId != 0;
    }

    private static bool IsMounted()
        => Service.Condition.Any(
            ConditionFlag.Mounted,
            ConditionFlag.RidingPillion,
            ConditionFlag.Mounting,
            ConditionFlag.Mounting71);

    private static bool IsBetweenAreas()
        => Service.Condition.Any(
            ConditionFlag.BetweenAreas,
            ConditionFlag.BetweenAreas51);
}
