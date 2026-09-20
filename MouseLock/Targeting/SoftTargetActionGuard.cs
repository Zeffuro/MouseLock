using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace MouseLock.Targeting;

internal sealed unsafe class SoftTargetActionGuard(Func<bool> shouldPreserve) : IDisposable
{
    private delegate void CompleteActionDelegate(TargetSystem* targets);

    private Hook<ActionManager.Delegates.UseActionLocation>? _useActionHook;
    private Hook<CompleteActionDelegate>? _completeActionHook;
    private int _actionDepth;
    private bool _enabled;

    internal string? Error { get; private set; }

    internal void SetEnabled(bool enabled)
    {
        if (_enabled == enabled || (enabled && Error != null))
        {
            return;
        }

        try
        {
            if (enabled)
            {
                if (_useActionHook == null)
                {
                    var targets = TargetSystem.Instance();
                    if (targets == null)
                    {
                        return;
                    }

                    var targetActions = (TargetSystemActions*)targets;
                    var useActionAddress = ActionManager.MemberFunctionPointers.UseActionLocation;
                    var completeActionAddress = targetActions->VirtualTable->CompleteAction;
                    if (useActionAddress == null || completeActionAddress == null)
                    {
                        throw new InvalidOperationException("Soft-target action functions are unavailable.");
                    }

                    _completeActionHook = Service.GameInteropProvider.HookFromAddress<CompleteActionDelegate>(
                        completeActionAddress, CompleteActionDetour);
                    _useActionHook = Service.GameInteropProvider.HookFromAddress<ActionManager.Delegates.UseActionLocation>(
                        useActionAddress, UseActionDetour);
                }

                _completeActionHook!.Enable();
                _useActionHook.Enable();
            }
            else
            {
                _useActionHook?.Disable();
                _completeActionHook?.Disable();
            }

            _enabled = enabled;
        }
        catch (Exception ex)
        {
            Dispose();
            Error = "Couldn't keep soft targets after actions. Reload MouseLock to retry.";
            Service.Logger.Error(ex, "Could not enable soft-target action handling.");
        }
    }

    public void Dispose()
    {
        _useActionHook?.Dispose();
        _useActionHook = null;
        _completeActionHook?.Dispose();
        _completeActionHook = null;
        _enabled = false;
    }

    private bool UseActionDetour(ActionManager* manager, ActionType type, uint actionId, ulong targetId,
        Vector3* location, uint extraParam, byte a7)
    {
        _actionDepth++;
        try
        {
            return _useActionHook!.Original(manager, type, actionId, targetId, location, extraParam, a7);
        }
        finally
        {
            _actionDepth--;
        }
    }

    private void CompleteActionDetour(TargetSystem* targets)
    {
        try
        {
            if (Error == null && _actionDepth > 0 && targets == TargetSystem.Instance() && shouldPreserve())
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Error = "Couldn't keep soft targets after actions. Reload MouseLock to retry.";
            Service.Logger.Error(ex, "Could not preserve the soft target after an action.");
        }

        _completeActionHook!.Original(targets);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct TargetSystemActions
    {
        [FieldOffset(0)] public TargetSystemVTable* VirtualTable;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct TargetSystemVTable
    {
        [FieldOffset(0x18)] public delegate* unmanaged<TargetSystem*, void> CompleteAction;
    }
}
