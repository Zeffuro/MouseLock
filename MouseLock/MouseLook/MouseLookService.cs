using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Configuration;
using MouseLock.Input;
using MouseLock.MouseLook.Activation;
using MouseLock.MouseLook.Native;

namespace MouseLock.MouseLook;

internal sealed class MouseLookService : IDisposable
{
    private readonly TextInputMonitor _textInputMonitor;
    private readonly MouseLookHooks _hooks;
    private readonly MouseLookController _controller = new();
    private readonly MouseLookActivationRules _activationRules;
    private readonly MouseLookReleaseState _releaseState = new();

    private MouseLookDecision _lastDecision = MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable);
    private MouseLookStatus _status = MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
    private bool _isPadMouseModeEnabled;

    public bool IsActive => _controller.IsActive;

    internal MouseLookStatus Status => _status;

    internal MouseLookDecision LastDecision => _lastDecision;

    internal bool IsAtkModuleHandleInputHookReady => _hooks.IsAtkModuleHandleInputHookReady;

    internal bool IsCameraInputSourceHookReady => _hooks.IsCameraInputSourceHookReady;

    internal bool AreDetoursReady => _hooks.AreDetoursReady;

    internal bool IsMouseDragAvailable => _controller.IsMouseDragAvailable;

    internal bool IsCursorRecenterAvailable => _controller.IsCursorRecenterAvailable;

    internal bool IsMouseLookAvailable => AreDetoursReady && _controller.IsAvailable;

    internal event Action<MouseLookStatus>? StatusChanged;

    internal void RetryHooks()
    {
        _hooks.Retry();
        RefreshStatus();
    }

    internal void ForceReleaseCursor()
    {
        _releaseState.ClearStickyReleaseState();
        _releaseState.ClearResumeGate();
        ReleaseMouseLook();
        RefreshStatus();
    }

    internal void RefreshCurrentStatus()
    {
        if (!PluginState.Config.General.Enabled)
        {
            _releaseState.ClearStickyReleaseState();
        }

        RefreshStatus();
    }

    internal MouseLookService(TextInputMonitor textInputMonitor)
    {
        _textInputMonitor = textInputMonitor;
        _activationRules = new MouseLookActivationRules(_textInputMonitor);
        unsafe
        {
            _hooks = new MouseLookHooks(AtkModuleHandleInputDetour, CameraInputSourceDetour);
        }

        Service.Framework.Update += OnFrameworkUpdate;
        RefreshStatus();
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        ReleaseMouseLook();
        _hooks.Dispose();
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        var inputData = UIInputData.Instance();
        if (inputData is null)
        {
            HandleInputUnavailable();
            return;
        }

        var decision = EvaluateDecision(inputData);
        if (IsActive && !decision.ShouldLock)
        {
            ReleaseMouseLook(inputData);
        }

        UpdateStatus(decision);
    }

    private unsafe byte AtkModuleHandleInputDetour(
        AtkModule* atkModule,
        UIInputData* inputData,
        bool isPadMouseModeEnabled)
    {
        try
        {
            if (inputData is not null)
            {
                _isPadMouseModeEnabled = isPadMouseModeEnabled;
                var decision = EvaluateDecision(inputData, atkModule);
                _controller.UpdateMouseActions(inputData, allowNewActions: decision.ShouldLock);

                if (decision.ShouldLock)
                {
                    ApplyMouseLookBeforeNativeInput(inputData);
                }

                UpdateStatus(decision);
            }
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "MouseLook pre-input update failed.");
        }

        var result = _hooks.RunOriginalAtkModuleHandleInput(atkModule, inputData, isPadMouseModeEnabled);
        if (inputData is null)
        {
            HandleInputUnavailable();
            return result;
        }

        try
        {
            _textInputMonitor.UpdateNativeTextInput(atkModule);
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
                HandleInputUnavailable();
                return _hooks.RunOriginalCameraInputSource();
            }

            var decision = EvaluateDecision(inputData);
            if (!decision.ShouldLock)
            {
                if (_controller.ShouldRelease)
                {
                    ReleaseMouseLook(inputData);
                }

                UpdateStatus(decision);
                return _hooks.RunOriginalCameraInputSource();
            }

            ApplyMouseLook(inputData);
            UpdateStatus(decision);
            return 3;
        }
        catch (Exception ex)
        {
            Service.Logger.Debug(ex, "MouseLook camera-input detour failed. Falling back to original input source.");
            return _hooks.RunOriginalCameraInputSource();
        }
    }

    private unsafe void UpdateMouseLook(UIInputData* inputData, AtkModule* atkModule)
    {
        var decision = EvaluateDecision(inputData, atkModule);
        if (!decision.ShouldLock)
        {
            if (_controller.ShouldRelease)
            {
                ReleaseMouseLook(inputData);
            }

            UpdateStatus(decision);
            return;
        }

        ApplyMouseLook(inputData);
        UpdateStatus(decision);
    }

    private unsafe void ApplyMouseLook(UIInputData* inputData)
        => _controller.Apply(inputData, CreateApplyOptions(applyCursorOverlayCompatibility: true));

    private unsafe void ApplyMouseLookBeforeNativeInput(UIInputData* inputData)
        => _controller.Apply(inputData, CreateApplyOptions(applyCursorOverlayCompatibility: false));

    private static MouseLookApplyOptions CreateApplyOptions(bool applyCursorOverlayCompatibility)
        => new(
            ApplyCursorOverlayCompatibility: applyCursorOverlayCompatibility,
            RememberCursorPosition: PluginState.Config.General.RestoreCursorPositionOnRelease,
            HideCursorOverlayPlugins: PluginState.Config.Compatibility.HideCursorOverlayPluginsDuringMouseLook);

    private unsafe void ReleaseMouseLook(UIInputData* inputData)
        => _controller.Release(
            inputData,
            inputData->CursorInputs.IsGameWindowFocused &&
            PluginState.Config.General.RestoreCursorPositionOnRelease);

    private void HandleInputUnavailable()
    {
        ReleaseMouseLookWithoutInput(restoreCursor: false);
        UpdateStatus(MouseLookDecision.Pause(MouseLookPauseReason.InputUnavailable));
    }

    private unsafe void ReleaseMouseLook()
    {
        var inputData = UIInputData.Instance();
        if (inputData is not null)
        {
            ReleaseMouseLook(inputData);
            return;
        }

        ReleaseMouseLookWithoutInput(PluginState.Config.General.RestoreCursorPositionOnRelease);
        RefreshStatus();
    }

    private void ReleaseMouseLookWithoutInput(bool restoreCursor)
        => _controller.ReleaseWithoutInput(restoreCursor);

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

        var availabilityFailure = GetAvailabilityFailureReason();
        if (availabilityFailure != MouseLookPauseReason.None)
        {
            return MouseLookStatus.Unavailable(availabilityFailure);
        }

        if (decision.Reason is MouseLookPauseReason.HookUnavailable or MouseLookPauseReason.NativeSubsystemUnavailable)
        {
            return MouseLookStatus.Unavailable(decision.Reason);
        }

        if (IsActive)
        {
            return MouseLookStatus.Active();
        }

        return decision.ShouldLock
            ? MouseLookStatus.Ready()
            : MouseLookStatus.Paused(decision.Reason);
    }

    private MouseLookPauseReason GetAvailabilityFailureReason()
    {
        if (!AreDetoursReady)
        {
            return MouseLookPauseReason.HookUnavailable;
        }

        return _controller.IsAvailable
            ? MouseLookPauseReason.None
            : MouseLookPauseReason.NativeSubsystemUnavailable;
    }

    private MouseLookDecision WithAvailability(MouseLookDecision decision)
    {
        var failureReason = GetAvailabilityFailureReason();
        return failureReason == MouseLookPauseReason.None
            ? decision
            : MouseLookDecision.Pause(failureReason);
    }

    private unsafe MouseLookDecision EvaluateDecision(
        UIInputData* inputData,
        AtkModule* atkModule = null)
    {
        var activationDecision = WithAvailability(_activationRules.Evaluate(inputData, atkModule));
        if (activationDecision.ShouldLock &&
            PluginState.Config.Activation.Conditions.DisableDuringGamepadMouseMode &&
            _isPadMouseModeEnabled)
        {
            activationDecision = MouseLookDecision.Pause(MouseLookPauseReason.GamepadMouseMode);
        }

        return _releaseState.Apply(inputData, activationDecision);
    }

}
