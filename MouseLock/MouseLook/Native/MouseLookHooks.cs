using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class MouseLookHooks(
    AtkModule.Delegates.HandleInput atkModuleHandleInputDetour,
    Camera.Delegates.GetCameraInputSource cameraInputSourceDetour)
    : IDisposable
{
    private Hook<AtkModule.Delegates.HandleInput>? _atkModuleHandleInputHook;
    private Hook<Camera.Delegates.GetCameraInputSource>? _cameraInputSourceHook;

    public void Enable()
    {
        EnableAtkModuleHandleInputHook();
        EnableCameraInputSourceHook();
    }

    public bool IsAtkModuleHandleInputHookReady { get; private set; }

    public bool IsCameraInputSourceHookReady { get; private set; }

    public bool AreDetoursReady => IsAtkModuleHandleInputHookReady && IsCameraInputSourceHookReady;

    public byte RunOriginalAtkModuleHandleInput(
        AtkModule* atkModule,
        UIInputData* inputData,
        bool isPadMouseModeEnabled)
        => _atkModuleHandleInputHook!.Original(atkModule, inputData, isPadMouseModeEnabled);

    public CameraInputSource RunOriginalCameraInputSource()
        => _cameraInputSourceHook!.Original();

    public void Retry()
    {
        if (!IsAtkModuleHandleInputHookReady)
        {
            DisposeAtkModuleHandleInputHook();
            EnableAtkModuleHandleInputHook();
        }

        if (!IsCameraInputSourceHookReady)
        {
            DisposeCameraInputSourceHook();
            EnableCameraInputSourceHook();
        }
    }

    public void Dispose()
    {
        DisposeCameraInputSourceHook();
        DisposeAtkModuleHandleInputHook();
    }

    private void EnableAtkModuleHandleInputHook()
    {
        var address = (nint)AtkModule.MemberFunctionPointers.HandleInput;
        if (address == 0)
        {
            Service.Logger.Error("Could not hook AtkModule.HandleInput: address was not resolved.");
            return;
        }

        _atkModuleHandleInputHook = Service.GameInteropProvider.HookFromAddress<AtkModule.Delegates.HandleInput>(
            address,
            atkModuleHandleInputDetour);
        _atkModuleHandleInputHook.Enable();
        IsAtkModuleHandleInputHookReady = true;
        Service.Logger.Information("Hooked AtkModule.HandleInput at 0x{Address:X}.", address);
    }

    private void DisposeAtkModuleHandleInputHook()
    {
        _atkModuleHandleInputHook?.Dispose();
        _atkModuleHandleInputHook = null;
        IsAtkModuleHandleInputHookReady = false;
    }

    private void EnableCameraInputSourceHook()
    {
        var address = (nint)Camera.MemberFunctionPointers.GetCameraInputSource;
        if (address == 0)
        {
            Service.Logger.Error("Could not hook Camera.GetCameraInputSource: address was not resolved.");
            return;
        }

        _cameraInputSourceHook = Service.GameInteropProvider.HookFromAddress(
            address,
            cameraInputSourceDetour);
        _cameraInputSourceHook.Enable();
        IsCameraInputSourceHookReady = true;
        Service.Logger.Information("Hooked camera input source at 0x{Address:X}.", address);
    }

    private void DisposeCameraInputSourceHook()
    {
        _cameraInputSourceHook?.Dispose();
        _cameraInputSourceHook = null;
        IsCameraInputSourceHookReady = false;
    }
}
