using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.MouseLook.Actions;
using MouseLock.MouseLook.Activation;
using MouseLock.MouseLook.Compatibility;
using MouseLock.MouseLook.Native;
using PluginSystem = MouseLock.System;

namespace MouseLock.MouseLook;

public sealed class MouseLookService : IDisposable
{
    private readonly ChatTwoTypingState _chatTwoTypingState;
    private readonly CursorOverlayCompatibilityState _cursorOverlayCompatibilityState = new();
    private readonly MouseLookActivationRules _activationRules;
    private readonly NativeCursorVisibilityState _nativeCursorVisibilityState = new();
    private readonly NativeCursorRecenterState _nativeCursorRecenterState = new();
    private readonly NativeMouseDragState _nativeMouseDragState = new();
    private readonly MouseButtonActionExecutor _mouseButtonActionExecutor = new();

    private Hook<AtkModuleHandleInputDelegate>? _atkModuleHandleInputHook;
    private Hook<CameraInputSourceDelegate>? _cameraInputSourceHook;
    private bool _nativeTextInputActive;

    public bool IsActive => _nativeMouseDragState.IsActive || _nativeCursorVisibilityState.IsActive || _nativeCursorRecenterState.IsActive;

    public MouseLookService()
    {
        _chatTwoTypingState = new ChatTwoTypingState();
        _activationRules = new MouseLookActivationRules(_chatTwoTypingState);

        EnableAtkModuleHandleInputHook();
        EnableCameraInputSourceHook();
        Service.Framework.Update += OnFrameworkUpdate;
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
            _nativeMouseDragState.DeactivateWithoutRelease();
            _nativeCursorVisibilityState.Release();
            _nativeCursorRecenterState.Release(restoreCursor: false);
            _cursorOverlayCompatibilityState.Reset();
            return;
        }

        if (IsActive && !_activationRules.ShouldLock(inputData, nativeTextInputActive: _nativeTextInputActive))
        {
            ReleaseMouseLook(inputData);
        }
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
                var shouldLock = _activationRules.ShouldLock(inputData, atkModule, nativeTextInputActive: _nativeTextInputActive);
                _mouseButtonActionExecutor.Update(inputData, allowNewActions: shouldLock);

                if (shouldLock)
                {
                    ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: false);
                }
            }
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "MouseLook pre-input update failed.");
        }

        var result = _atkModuleHandleInputHook!.Original(atkModule, inputData, isPadMouseModeEnabled);
        if (inputData is null)
        {
            _nativeMouseDragState.DeactivateWithoutRelease();
            _nativeCursorVisibilityState.Release();
            _nativeCursorRecenterState.Release(restoreCursor: false);
            _cursorOverlayCompatibilityState.Reset();
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
                _nativeMouseDragState.DeactivateWithoutRelease();
                _nativeCursorVisibilityState.Release();
                _nativeCursorRecenterState.Release(restoreCursor: false);
                _cursorOverlayCompatibilityState.Reset();
                return _cameraInputSourceHook!.Original();
            }

            if (!_activationRules.ShouldLock(inputData, nativeTextInputActive: _nativeTextInputActive))
            {
                if (IsActive || _cursorOverlayCompatibilityState.IsActive)
                {
                    ReleaseMouseLook(inputData);
                }

                return _cameraInputSourceHook!.Original();
            }

            ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: true);
            return 3;
        }
        catch (Exception)
        {
            return _cameraInputSourceHook!.Original();
        }
    }

    private unsafe void UpdateMouseLook(UIInputData* inputData, AtkModule* atkModule)
    {
        if (!_activationRules.ShouldLock(inputData, atkModule))
        {
            if (IsActive || _cursorOverlayCompatibilityState.IsActive)
            {
                ReleaseMouseLook(inputData);
            }

            return;
        }

        ApplyMouseLook(inputData, applyScheduledMoveCompensation: true, applyCursorOverlayCompatibility: true);
    }

    private unsafe void ApplyMouseLook(
        UIInputData* inputData,
        bool applyScheduledMoveCompensation,
        bool applyCursorOverlayCompatibility)
    {
        _cursorOverlayCompatibilityState.Release(inputData);
        var physicalHeldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;

        _nativeCursorRecenterState.Apply(inputData, applyScheduledMoveCompensation);
        MouseButtonSuppressionState.Apply(inputData);
        _nativeMouseDragState.Apply(inputData);
        _nativeCursorVisibilityState.Apply();

        if (applyCursorOverlayCompatibility)
        {
            ApplyCursorOverlayCompatibility(inputData, physicalHeldButtons);
        }
    }

    private unsafe void ApplyCursorOverlayCompatibility(UIInputData* inputData, MouseButtonFlags physicalHeldButtons)
    {
        if (!PluginSystem.Config.General.Compatibility.HideCursorOverlayPluginsDuringMouseLook)
        {
            _cursorOverlayCompatibilityState.Release(inputData);
            return;
        }

        _cursorOverlayCompatibilityState.Apply(inputData, physicalHeldButtons);
    }

    private unsafe void ReleaseMouseLook(UIInputData* inputData)
    {
        _mouseButtonActionExecutor.ReleaseAll(inputData);
        _nativeMouseDragState.Release(inputData);
        _nativeCursorVisibilityState.Release();
        _nativeCursorRecenterState.Release(inputData, restoreCursor: inputData->CursorInputs.IsGameWindowFocused);
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

        _nativeMouseDragState.DeactivateWithoutRelease();
        _nativeCursorVisibilityState.Release();
        _nativeCursorRecenterState.Release(restoreCursor: true);
        _cursorOverlayCompatibilityState.Reset();
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
            Service.Logger.Information("Hooked AtkModule.HandleInput at 0x{Address:X}.", address);
        }
    }

    private void DisposeAtkModuleHandleInputHook()
    {
        _atkModuleHandleInputHook?.Dispose();
        _atkModuleHandleInputHook = null;
    }

    private void EnableCameraInputSourceHook()
    {
        if (!Service.SigScanner.TryScanText(NativeMouseLookSignatures.CameraInputSource, out var address) ||
            address == 0)
        {
            Service.Logger.Error("Could not hook camera input source");
            return;
        }

        _cameraInputSourceHook = Service.GameInteropProvider.HookFromAddress<CameraInputSourceDelegate>(
            address,
            CameraInputSourceDetour);
        _cameraInputSourceHook.Enable();
        Service.Logger.Information("Hooked camera input source at 0x{Address:X}.", address);
    }

    private void DisposeCameraInputSourceHook()
    {
        _cameraInputSourceHook?.Dispose();
        _cameraInputSourceHook = null;
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private unsafe delegate byte AtkModuleHandleInputDelegate(
        AtkModule* atkModule,
        UIInputData* inputData,
        byte isPadMouseModeEnabled);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CameraInputSourceDelegate();
}
