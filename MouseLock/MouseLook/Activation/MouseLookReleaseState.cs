using System.Diagnostics;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Game;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookReleaseState
{
    private const int ReleaseModifierTapThresholdMilliseconds = 250;

    private readonly Stopwatch _releaseModifierPressTimer = new();
    private readonly Stopwatch _resumeDelayTimer = new();
    private readonly HashSet<VirtualKey> _releaseModifierPreviousHeldKeys = [];

    private MouseLookDecision _lastActivationDecision = MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
    private ReleaseModifierTapBehavior _latchedReleaseBehavior = ReleaseModifierTapBehavior.None;
    private bool _resumeGateActive;
    private bool _releaseModifierWasDown;
    private bool _releaseModifierPressedWhileLatched;
    private bool _releaseModifierTapCanceled;

    private bool IsLatchedReleaseActive => _latchedReleaseBehavior != ReleaseModifierTapBehavior.None;

    public unsafe MouseLookDecision Apply(UIInputData* inputData, MouseLookDecision activationDecision)
    {
        var releaseModifierTapped = UpdateReleaseModifierTap(inputData);
        var shouldStartResumeGate = activationDecision.ShouldLock && ShouldGateAfterPause(_lastActivationDecision.Reason);
        _lastActivationDecision = activationDecision;

        var decision = ApplyResumePolicy(inputData, activationDecision, shouldStartResumeGate);

        if (releaseModifierTapped && decision.ShouldLock)
        {
            var tapBehavior = PluginState.Config.General.ReleaseModifierTapBehavior;
            if (tapBehavior != ReleaseModifierTapBehavior.None)
            {
                _latchedReleaseBehavior = tapBehavior;
            }
        }

        if (!IsLatchedReleaseActive || !decision.ShouldLock)
        {
            return decision;
        }

        if (_latchedReleaseBehavior == ReleaseModifierTapBehavior.UntilWorldClick &&
            ShouldResumeFromWorldClick(inputData))
        {
            ClearLatchedRelease();
            return decision;
        }

        return MouseLookDecision.Pause(
            _latchedReleaseBehavior == ReleaseModifierTapBehavior.UntilNextTap
                ? MouseLookPauseReason.ToggleRelease
                : MouseLookPauseReason.StickyRelease);
    }

    public void ClearLatchedReleaseState()
    {
        ClearLatchedRelease();
        _releaseModifierWasDown = false;
        _releaseModifierPressedWhileLatched = false;
        _releaseModifierTapCanceled = false;
        _releaseModifierPressTimer.Reset();
        _releaseModifierPreviousHeldKeys.Clear();
    }

    public void ClearResumeGate()
    {
        _resumeGateActive = false;
        _resumeDelayTimer.Reset();
    }

    private unsafe MouseLookDecision ApplyResumePolicy(
        UIInputData* inputData,
        MouseLookDecision decision,
        bool shouldStartResumeGate)
    {
        if (!decision.ShouldLock)
        {
            ClearResumeGate();
            return decision;
        }

        var settings = PluginState.Config.Activation;
        if (settings.ResumePolicy == MouseLookResumePolicy.Immediate)
        {
            ClearResumeGate();
            return decision;
        }

        if (shouldStartResumeGate && !_resumeGateActive)
        {
            _resumeGateActive = true;
            _resumeDelayTimer.Restart();
        }

        if (!_resumeGateActive)
        {
            return decision;
        }

        if (settings.ResumePolicy == MouseLookResumePolicy.Delay)
        {
            if (_resumeDelayTimer.ElapsedMilliseconds >= settings.ResumeDelayMilliseconds)
            {
                ClearResumeGate();
                return decision;
            }

            return MouseLookDecision.Pause(MouseLookPauseReason.ResumeDelay);
        }

        if (settings.ResumePolicy == MouseLookResumePolicy.WorldClick)
        {
            if (ShouldResumeFromWorldClick(inputData))
            {
                ClearResumeGate();
                return decision;
            }

            return MouseLookDecision.Pause(MouseLookPauseReason.WaitingForWorldClick);
        }

        ClearResumeGate();
        return decision;
    }

    private static bool ShouldGateAfterPause(MouseLookPauseReason reason)
        => reason is MouseLookPauseReason.ConfigWindowOpen
            or MouseLookPauseReason.FirstRunWindowOpen
            or MouseLookPauseReason.GameUnfocused
            or MouseLookPauseReason.TextInput
            or MouseLookPauseReason.TalkAddon
            or MouseLookPauseReason.DalamudWindowFocused
            or MouseLookPauseReason.NativeAddonFocused
            or MouseLookPauseReason.NativeAddonHovered
            or MouseLookPauseReason.TPie
            or MouseLookPauseReason.ExternalSuspension;

    private unsafe bool UpdateReleaseModifierTap(UIInputData* inputData)
    {
        if (!CanUseReleaseModifierTap())
        {
            ClearLatchedReleaseState();
            return false;
        }

        if (_releaseModifierWasDown && !inputData->CursorInputs.IsGameWindowFocused)
        {
            _releaseModifierTapCanceled = true;
        }

        var isDown = ReleaseModifierState.IsHeld(inputData);
        if (isDown && !_releaseModifierWasDown)
        {
            _releaseModifierPressTimer.Restart();
            _releaseModifierPressedWhileLatched = IsLatchedReleaseActive;
            SnapshotHeldKeys();
            _releaseModifierTapCanceled = !inputData->CursorInputs.IsGameWindowFocused ||
                                         HasMouseButtonInput(inputData);
            ClearLatchedRelease();
        }
        else if (isDown)
        {
            if (!_releaseModifierTapCanceled && HasTapCancelingInput(inputData))
            {
                _releaseModifierTapCanceled = true;
            }

            SnapshotHeldKeys();
        }

        var tapped = false;
        if (!isDown && _releaseModifierWasDown)
        {
            tapped = !_releaseModifierPressedWhileLatched &&
                     !_releaseModifierTapCanceled &&
                     inputData->CursorInputs.IsGameWindowFocused &&
                     _releaseModifierPressTimer.ElapsedMilliseconds <= ReleaseModifierTapThresholdMilliseconds;
            _releaseModifierPressTimer.Reset();
            _releaseModifierPressedWhileLatched = false;
            _releaseModifierTapCanceled = false;
            _releaseModifierPreviousHeldKeys.Clear();
        }

        _releaseModifierWasDown = isDown;
        return tapped;
    }

    private static bool CanUseReleaseModifierTap()
        => PluginState.Config.General.Enabled &&
           PluginState.Config.General.ReleaseModifierTapBehavior != ReleaseModifierTapBehavior.None &&
           PluginState.Config.General.ReleaseModifier != ReleaseModifierKey.None &&
           Service.ClientState.IsLoggedIn;

    private void ClearLatchedRelease()
        => _latchedReleaseBehavior = ReleaseModifierTapBehavior.None;

    private static unsafe bool ShouldResumeFromWorldClick(UIInputData* inputData)
    {
        if ((inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons) == 0)
        {
            return false;
        }

        var conditions = PluginState.Config.Activation.Conditions;
        return !NativeUiState.IsBlockingAddonFocused() &&
               !NativeUiState.IsBlockingAddonHovered(inputData) &&
               !DalamudUiState.IsBlockingUiActive(conditions);
    }

    private unsafe bool HasTapCancelingInput(UIInputData* inputData)
        => !inputData->CursorInputs.IsGameWindowFocused ||
           HasMouseButtonInput(inputData) ||
           WasNonReleaseModifierKeyPressed();

    private static unsafe bool HasMouseButtonInput(UIInputData* inputData)
        => inputData->CursorInputs.MouseButtonPressedFlags != MouseButtonFlags.None;

    private bool WasNonReleaseModifierKeyPressed()
    {
        var releaseModifier = PluginState.Config.General.ReleaseModifier;
        foreach (var key in Service.KeyState.GetValidVirtualKeys())
        {
            if (IsReleaseModifierKey(key, releaseModifier) ||
                IsMouseButtonKey(key) ||
                !Service.KeyState[key])
            {
                continue;
            }

            if (!_releaseModifierPreviousHeldKeys.Contains(key))
            {
                return true;
            }
        }

        return false;
    }

    private void SnapshotHeldKeys()
    {
        _releaseModifierPreviousHeldKeys.Clear();

        var releaseModifier = PluginState.Config.General.ReleaseModifier;
        foreach (var key in Service.KeyState.GetValidVirtualKeys())
        {
            if (IsReleaseModifierKey(key, releaseModifier) ||
                IsMouseButtonKey(key) ||
                !Service.KeyState[key])
            {
                continue;
            }

            _releaseModifierPreviousHeldKeys.Add(key);
        }
    }

    private static bool IsReleaseModifierKey(VirtualKey key, ReleaseModifierKey releaseModifier)
        => releaseModifier switch
        {
            ReleaseModifierKey.Alt => key is VirtualKey.MENU or VirtualKey.LMENU or VirtualKey.RMENU,
            ReleaseModifierKey.Control => key is VirtualKey.CONTROL or VirtualKey.LCONTROL or VirtualKey.RCONTROL,
            ReleaseModifierKey.Shift => key is VirtualKey.SHIFT or VirtualKey.LSHIFT or VirtualKey.RSHIFT,
            _ => false,
        };

    private static bool IsMouseButtonKey(VirtualKey key)
        => key is VirtualKey.LBUTTON
            or VirtualKey.RBUTTON
            or VirtualKey.MBUTTON
            or VirtualKey.XBUTTON1
            or VirtualKey.XBUTTON2;
}
