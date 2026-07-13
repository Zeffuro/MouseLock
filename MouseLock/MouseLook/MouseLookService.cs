using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Actions;
using MouseLock.Compatibility;
using MouseLock.Configuration;
using MouseLock.Game;
using MouseLock.MouseLook.Activation;
using MouseLock.MouseLook.Native;

namespace MouseLock.MouseLook;

public sealed class MouseLookService : IDisposable
{
    private const int StickyReleaseTapThresholdMilliseconds = 250;

    private readonly ChatTwoTypingState _chatTwoTypingState;
    private readonly CursorOverlayCompatibilityState _cursorOverlayCompatibilityState = new();
    private readonly MouseLookActivationRules _activationRules;
    private readonly CursorVisibilityState _cursorVisibilityState = new();
    private readonly CursorRecenterState _cursorRecenterState = new();
    private readonly MouseDragState _mouseDragState = new();
    private readonly MouseButtonActionExecutor _mouseButtonActionExecutor = new();
    private readonly Stopwatch _releaseModifierPressTimer = new();
    private readonly Stopwatch _resumeDelayTimer = new();

    private Hook<AtkModuleHandleInputDelegate>? _atkModuleHandleInputHook;
    private Hook<CameraInputSourceDelegate>? _cameraInputSourceHook;
    private MouseLookDecision _lastDecision = MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
    private MouseLookDecision _lastActivationDecision = MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
    private MouseLookStatus _status = MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
    private bool _atkModuleHandleInputHookReady;
    private bool _cameraInputSourceHookReady;
    private bool _stickyReleaseActive;
    private bool _resumeGateActive;
    private bool _releaseModifierWasDown;
    private bool _releaseModifierPressedWhileSticky;
    private bool _nativeTextInputActive;
    private bool _isPadMouseModeEnabled;

    public bool IsActive => _mouseDragState.IsActive || _cursorVisibilityState.IsActive || _cursorRecenterState.IsActive;

    internal MouseLookStatus Status => _status;

    internal MouseLookDecision LastDecision => _lastDecision;

    internal bool IsAtkModuleHandleInputHookReady => _atkModuleHandleInputHookReady;

    internal bool IsCameraInputSourceHookReady => _cameraInputSourceHookReady;

    internal event Action<MouseLookStatus>? StatusChanged;

    internal void RetryHooks()
    {
        if (!_atkModuleHandleInputHookReady)
        {
            DisposeAtkModuleHandleInputHook();
            EnableAtkModuleHandleInputHook();
        }

        if (!_cameraInputSourceHookReady)
        {
            DisposeCameraInputSourceHook();
            EnableCameraInputSourceHook();
        }

        RefreshStatus();
    }

    internal void ForceReleaseCursor()
    {
        ClearStickyReleaseState();
        ClearResumeGate();
        ReleaseMouseLook();
        RefreshStatus();
    }

    internal void RefreshCurrentStatus()
    {
        if (!PluginState.Config.General.Enabled)
        {
            ClearStickyReleaseState();
        }

        RefreshStatus();
    }

    public MouseLookService()
    {
        _chatTwoTypingState = new ChatTwoTypingState();
        _activationRules = new MouseLookActivationRules(_chatTwoTypingState);

        EnableAtkModuleHandleInputHook();
        EnableCameraInputSourceHook();
        Service.Framework.Update += OnFrameworkUpdate;
        RefreshStatus();
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        ReleaseMouseLook();
        DisposeCameraInputSourceHook();
        DisposeAtkModuleHandleInputHook();
        _chatTwoTypingState.Dispose();
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        var inputData = UIInputData.Instance();
        if (inputData is null)
        {
            _mouseDragState.DeactivateWithoutRelease();
            _cursorVisibilityState.Release();
            _cursorRecenterState.Release(restoreCursor: false);
            _cursorOverlayCompatibilityState.Reset();
            UpdateStatus(MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable));
            return;
        }

        var decision = EvaluateDecision(inputData, nativeTextInputActive: _nativeTextInputActive);
        if (IsActive && !decision.ShouldLock)
        {
            ReleaseMouseLook(inputData);
        }

        UpdateStatus(decision);
    }

    private unsafe byte AtkModuleHandleInputDetour(
        AtkModule* atkModule,
        UIInputData* inputData,
        byte isPadMouseModeEnabled)
    {
        try
        {
            if (inputData is not null)
            {
                _isPadMouseModeEnabled = isPadMouseModeEnabled != 0;
                var decision = EvaluateDecision(inputData, atkModule, _nativeTextInputActive);
                _mouseButtonActionExecutor.Update(inputData, allowNewActions: decision.ShouldLock);

                if (decision.ShouldLock)
                {
                    ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: false);
                }

                UpdateStatus(decision);
            }
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "MouseLook pre-input update failed.");
        }

        var result = _atkModuleHandleInputHook!.Original(atkModule, inputData, isPadMouseModeEnabled);
        if (inputData is null)
        {
            _mouseDragState.DeactivateWithoutRelease();
            _cursorVisibilityState.Release();
            _cursorRecenterState.Release(restoreCursor: false);
            _cursorOverlayCompatibilityState.Reset();
            UpdateStatus(MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable));
            return result;
        }

        try
        {
            _nativeTextInputActive = atkModule->IsTextInputActive();
            UpdateMouseLook(inputData, atkModule);
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "MouseLook post-input update failed.");
        }

        return result;
    }

    private unsafe long CameraInputSourceDetour()
    {
        try
        {
            var inputData = UIInputData.Instance();
            if (inputData is null)
            {
                _mouseDragState.DeactivateWithoutRelease();
                _cursorVisibilityState.Release();
                _cursorRecenterState.Release(restoreCursor: false);
                _cursorOverlayCompatibilityState.Reset();
                UpdateStatus(MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable));
                return _cameraInputSourceHook!.Original();
            }

            var decision = EvaluateDecision(inputData, nativeTextInputActive: _nativeTextInputActive);
            if (!decision.ShouldLock)
            {
                if (IsActive || _cursorOverlayCompatibilityState.IsActive)
                {
                    ReleaseMouseLook(inputData);
                }

                UpdateStatus(decision);
                return _cameraInputSourceHook!.Original();
            }

            ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: true);
            UpdateStatus(decision);
            return 3;
        }
        catch (Exception ex)
        {
            Service.Logger.Debug(ex, "MouseLook camera-input detour failed. Falling back to original input source.");
            return _cameraInputSourceHook!.Original();
        }
    }

    private unsafe void UpdateMouseLook(UIInputData* inputData, AtkModule* atkModule)
    {
        var decision = EvaluateDecision(inputData, atkModule);
        if (!decision.ShouldLock)
        {
            if (IsActive || _cursorOverlayCompatibilityState.IsActive)
            {
                ReleaseMouseLook(inputData);
            }

            UpdateStatus(decision);
            return;
        }

        ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: true);
        UpdateStatus(decision);
    }

    private unsafe void ApplyMouseLook(
        UIInputData* inputData,
        bool applyScheduledMoveCompensation,
        bool applyCursorOverlayCompatibility)
    {
        _cursorOverlayCompatibilityState.Release(inputData);
        var physicalHeldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;

        _cursorRecenterState.Apply(
            inputData,
            applyScheduledMoveCompensation,
            PluginState.Config.General.RestoreCursorPositionOnRelease);
        MouseButtonSuppressionState.Apply(inputData);
        _mouseDragState.Apply(inputData);
        _cursorVisibilityState.Apply();

        if (applyCursorOverlayCompatibility)
        {
            ApplyCursorOverlayCompatibility(inputData, physicalHeldButtons);
        }
    }

    private unsafe void ApplyCursorOverlayCompatibility(UIInputData* inputData, MouseButtonFlags physicalHeldButtons)
    {
        if (!PluginState.Config.General.Compatibility.HideCursorOverlayPluginsDuringMouseLook)
        {
            _cursorOverlayCompatibilityState.Release(inputData);
            return;
        }

        _cursorOverlayCompatibilityState.Apply(inputData, physicalHeldButtons);
    }

    private unsafe void ReleaseMouseLook(UIInputData* inputData)
    {
        _mouseButtonActionExecutor.ReleaseAll(inputData);
        _mouseDragState.Release(inputData);
        _cursorVisibilityState.Release();
        _cursorRecenterState.Release(
            inputData,
            restoreCursor: inputData->CursorInputs.IsGameWindowFocused &&
                           PluginState.Config.General.RestoreCursorPositionOnRelease);
        _cursorOverlayCompatibilityState.Release(inputData);
    }

    private unsafe void ReleaseMouseLook()
    {
        var inputData = UIInputData.Instance();
        if (inputData is not null)
        {
            ReleaseMouseLook(inputData);
            return;
        }

        _mouseDragState.DeactivateWithoutRelease();
        _cursorVisibilityState.Release();
        _cursorRecenterState.Release(restoreCursor: PluginState.Config.General.RestoreCursorPositionOnRelease);
        _cursorOverlayCompatibilityState.Reset();
        RefreshStatus();
    }

    private void EnableAtkModuleHandleInputHook()
    {
        unsafe
        {
            var address = AtkModule.Addresses.HandleInput.Value;
            if (address == 0)
            {
                Service.Logger.Error("Could not hook AtkModule.HandleInput: address was not resolved.");
                return;
            }

            _atkModuleHandleInputHook = Service.GameInteropProvider.HookFromAddress<AtkModuleHandleInputDelegate>(
                address,
                AtkModuleHandleInputDetour);
            _atkModuleHandleInputHook.Enable();
            _atkModuleHandleInputHookReady = true;
            Service.Logger.Information("Hooked AtkModule.HandleInput at 0x{Address:X}.", address);
        }
    }

    private void DisposeAtkModuleHandleInputHook()
    {
        _atkModuleHandleInputHook?.Dispose();
        _atkModuleHandleInputHook = null;
        _atkModuleHandleInputHookReady = false;
        RefreshStatus();
    }

    private void EnableCameraInputSourceHook()
    {
        if (!Service.SigScanner.TryScanText(MouseLookSignatures.CameraInputSource, out var address) ||
            address == 0)
        {
            Service.Logger.Error("Could not hook camera input source");
            return;
        }

        _cameraInputSourceHook = Service.GameInteropProvider.HookFromAddress<CameraInputSourceDelegate>(
            address,
            CameraInputSourceDetour);
        _cameraInputSourceHook.Enable();
        _cameraInputSourceHookReady = true;
        Service.Logger.Information("Hooked camera input source at 0x{Address:X}.", address);
    }

    private void DisposeCameraInputSourceHook()
    {
        _cameraInputSourceHook?.Dispose();
        _cameraInputSourceHook = null;
        _cameraInputSourceHookReady = false;
        RefreshStatus();
    }

    private void UpdateStatus(MouseLookDecision decision)
    {
        _lastDecision = decision;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var next = BuildStatus(_lastDecision);
        if (next == _status)
        {
            return;
        }

        _status = next;
        StatusChanged?.Invoke(next);
    }

    private MouseLookStatus BuildStatus(MouseLookDecision decision)
    {
        if (!PluginState.Config.General.Enabled)
        {
            return MouseLookStatus.Off();
        }

        if (!AreHooksReady || decision.Reason == MouseLookPauseReason.HookUnavailable)
        {
            return MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
        }

        if (IsActive)
        {
            return MouseLookStatus.Active();
        }

        return decision.ShouldLock
            ? MouseLookStatus.Ready()
            : MouseLookStatus.Paused(decision.Reason);
    }

    private bool AreHooksReady => _atkModuleHandleInputHookReady && _cameraInputSourceHookReady;

    private MouseLookDecision WithHookReadiness(MouseLookDecision decision)
        => AreHooksReady ? decision : MouseLookDecision.Pause(MouseLookPauseReason.HookUnavailable);

    private unsafe MouseLookDecision EvaluateDecision(
        UIInputData* inputData,
        AtkModule* atkModule = null,
        bool nativeTextInputActive = false)
    {
        var releaseModifierTapped = UpdateReleaseModifierTap(inputData);
        var activationDecision = WithHookReadiness(_activationRules.Evaluate(inputData, atkModule, nativeTextInputActive));
        if (activationDecision.ShouldLock &&
            PluginState.Config.General.Conditions.DisableDuringGamepadMouseMode &&
            _isPadMouseModeEnabled)
        {
            activationDecision = MouseLookDecision.Pause(MouseLookPauseReason.GamepadMouseMode);
        }
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

    private unsafe bool ShouldResumeFromWorldClick(UIInputData* inputData)
    {
        if ((inputData->CursorInputs.MouseButtonPressedFlags & MouseLookButtons.PhysicalLookButtons) == 0)
        {
            return false;
        }

        return !NativeUiState.IsBlockingAddonFocused() &&
               !NativeUiState.IsBlockingAddonHovered(inputData);
    }

    private void ClearStickyReleaseState()
    {
        _stickyReleaseActive = false;
        _releaseModifierWasDown = false;
        _releaseModifierPressedWhileSticky = false;
        _releaseModifierPressTimer.Reset();
    }

    private void ClearResumeGate()
    {
        _resumeGateActive = false;
        _resumeDelayTimer.Reset();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private unsafe delegate byte AtkModuleHandleInputDelegate(
        AtkModule* atkModule,
        UIInputData* inputData,
        byte isPadMouseModeEnabled);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CameraInputSourceDelegate();
}
