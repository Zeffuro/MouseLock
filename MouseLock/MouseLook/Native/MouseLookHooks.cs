using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using AtkModuleHandleInputDelegate = FFXIVClientStructs.FFXIV.Component.GUI.AtkModule.Delegates.HandleInput;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class MouseLookHooks : IDisposable
{
    private readonly AtkModuleHandleInputDelegate _atkModuleHandleInputDetour;
    private readonly CameraInputSourceDelegate _cameraInputSourceDetour;

    private Hook<AtkModuleHandleInputDelegate>? _atkModuleHandleInputHook;
    private Hook<CameraInputSourceDelegate>? _cameraInputSourceHook;

    public MouseLookHooks(
        AtkModuleHandleInputDelegate atkModuleHandleInputDetour,
        CameraInputSourceDelegate cameraInputSourceDetour)
    {
        _atkModuleHandleInputDetour = atkModuleHandleInputDetour;
        _cameraInputSourceDetour = cameraInputSourceDetour;

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

    public long RunOriginalCameraInputSource()
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

        _atkModuleHandleInputHook = Service.GameInteropProvider.HookFromAddress<AtkModuleHandleInputDelegate>(
            address,
            _atkModuleHandleInputDetour);
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
        if (!Service.SigScanner.TryScanText(MouseLookSignatures.CameraInputSource, out var address) ||
            address == 0)
        {
            Service.Logger.Error("Could not hook camera input source");
            return;
        }

        _cameraInputSourceHook = Service.GameInteropProvider.HookFromAddress(
            address,
            _cameraInputSourceDetour);
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long CameraInputSourceDelegate();
}
