using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Game;

namespace MouseLock.MouseLook.Activation;

internal sealed class MouseLookReleaseState
{
    private const int StickyReleaseTapThresholdMilliseconds = 250;

    private readonly Stopwatch _releaseModifierPressTimer = new();
    private readonly Stopwatch _resumeDelayTimer = new();

    private MouseLookDecision _lastActivationDecision = MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
    private bool _stickyReleaseActive;
    private bool _resumeGateActive;
    private bool _releaseModifierWasDown;
    private bool _releaseModifierPressedWhileSticky;

    public unsafe MouseLookDecision Apply(UIInputData* inputData, MouseLookDecision activationDecision)
    {
        var releaseModifierTapped = UpdateReleaseModifierTap(inputData);
        var shouldStartResumeGate = activationDecision.ShouldLock && ShouldGateAfterPause(_lastActivationDecision.Reason);
        _lastActivationDecision = activationDecision;

        var decision = ApplyResumePolicy(inputData, activationDecision, shouldStartResumeGate);

        if (releaseModifierTapped && decision.ShouldLock)
        {
            _stickyReleaseActive = true;
        }

        if (!_stickyReleaseActive || !decision.ShouldLock)
        {
            return decision;
        }

        if (ShouldResumeFromWorldClick(inputData))
        {
            _stickyReleaseActive = false;
            return decision;
        }

        return MouseLookDecision.Pause(MouseLookPauseReason.StickyRelease);
    }

    public void ClearStickyReleaseState()
    {
        _stickyReleaseActive = false;
        _releaseModifierWasDown = false;
        _releaseModifierPressedWhileSticky = false;
        _releaseModifierPressTimer.Reset();
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

        var settings = PluginState.Config.General;
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
            or MouseLookPauseReason.GameUnfocused
            or MouseLookPauseReason.TextInput
            or MouseLookPauseReason.TalkAddon
            or MouseLookPauseReason.NativeAddonFocused
            or MouseLookPauseReason.NativeAddonHovered
            or MouseLookPauseReason.TPie
            or MouseLookPauseReason.ExternalSuspension;

    private unsafe bool UpdateReleaseModifierTap(UIInputData* inputData)
    {
        if (!CanUseStickyRelease())
        {
            ClearStickyReleaseState();
            return false;
        }

        var isDown = ReleaseModifierState.IsHeld(inputData);
        if (isDown && !_releaseModifierWasDown)
        {
            _releaseModifierPressTimer.Restart();
            _releaseModifierPressedWhileSticky = _stickyReleaseActive;
            _stickyReleaseActive = false;
        }

        var tapped = false;
        if (!isDown && _releaseModifierWasDown)
        {
            tapped = !_releaseModifierPressedWhileSticky &&
                     _releaseModifierPressTimer.ElapsedMilliseconds <= StickyReleaseTapThresholdMilliseconds;
            _releaseModifierPressTimer.Reset();
            _releaseModifierPressedWhileSticky = false;
        }

        _releaseModifierWasDown = isDown;
        return tapped;
    }

    private static bool CanUseStickyRelease()
        => PluginState.Config.General.Enabled &&
           PluginState.Config.General.StickyReleaseEnabled &&
           PluginState.Config.General.ReleaseModifier != ReleaseModifierKey.None &&
           Service.ClientState.IsLoggedIn;

    private static unsafe bool ShouldResumeFromWorldClick(UIInputData* inputData)
    {
        if ((inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons) == 0)
        {
            return false;
        }

        return !NativeUiState.IsBlockingAddonFocused() &&
               !NativeUiState.IsBlockingAddonHovered(inputData);
    }
}
