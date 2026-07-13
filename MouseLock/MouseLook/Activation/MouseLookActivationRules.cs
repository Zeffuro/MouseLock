using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Actions;
using MouseLock.Compatibility;
using MouseLock.Configuration;
using MouseLock.Game;
using MouseLock.MouseLook;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookActivationRules(ChatTwoTypingState chatTwoTypingState)
{
    public unsafe bool ShouldLock(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        bool nativeTextInputActive = false)
        => Evaluate(inputData, atkModule, nativeTextInputActive).ShouldLock;

    public unsafe MouseLookDecision Evaluate(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        bool nativeTextInputActive = false)
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

        if (inputData is null)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
        }

        if (!inputData->CursorInputs.IsGameWindowFocused)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.GameUnfocused);
        }

        var conditions = PluginState.Config.General.Conditions;
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

        if (conditions.DisableWhileTextInputActive && IsAnyTextInputActive(atkModule, nativeTextInputActive))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TextInput);
        }

        if (conditions.DisableWhenTalkAddonVisible && NativeUiState.IsAddonVisible("Talk"))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TalkAddon);
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

        if (PluginState.Config.General.Compatibility.DisableDuringTPieRing &&
            TPieCompatibilityState.IsRingActive())
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.TPie);
        }

        if (ExternalSuspensionState.IsSuspended)
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.ExternalSuspension);
        }

        if (ReleaseModifierState.IsHeld(inputData))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.ReleaseModifier);
        }

        if (IsTemporaryReleaseActionHeld(inputData))
        {
            return MouseLookDecision.Pause(MouseLookPauseReason.MouseActionRelease);
        }

        return MouseLookDecision.Allow();
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

    private static bool IsGroundTargetingActive() => NativeUiState.IsAddonVisible("_TargetCursorGround");

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

    private static unsafe bool IsTemporaryReleaseActionHeld(UIInputData* inputData)
    {
        var heldButtons = inputData->CursorInputs.MouseButtonHeldFlags;
        var actions = PluginState.Config.General.MouseActions;

        return IsTemporaryReleaseHeld(MouseButtonActionResolver.ResolveLeft(inputData, actions), heldButtons, MouseButtonFlags.LBUTTON) ||
               IsTemporaryReleaseHeld(MouseButtonActionResolver.ResolveRight(inputData, actions), heldButtons, MouseButtonFlags.RBUTTON);
    }

    private static bool IsTemporaryReleaseHeld(
        MouseButtonGameInputBinding binding,
        MouseButtonFlags heldButtons,
        MouseButtonFlags button)
        => binding.Kind == MouseButtonBindingKind.TemporaryRelease &&
           (heldButtons & button) != 0;

}
