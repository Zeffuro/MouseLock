using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.MouseLook;

namespace MouseLock.Targeting;

internal sealed unsafe class TargetingService : IDisposable
{
    private GameObject* _ownedSoftTarget;
    private GameObjectId _ownedSoftTargetId;
    private bool _failed;
    private CameraMotionTracker _cameraMotion;
    private readonly SoftTargetActionGuard _actionGuard;

    internal string? Error { get; private set; }
    internal string? SoftTargetError => _actionGuard.Error;
    internal bool HasCandidate { get; private set; }

    internal TargetingService()
    {
        _actionGuard = new SoftTargetActionGuard(ShouldPreserveSoftTargetAfterAction);
        Service.Framework.Update += Update;
    }

    public void Dispose()
    {
        Service.Framework.Update -= Update;
        _actionGuard.Dispose();
        TryReleaseSoftTarget();
    }

    internal void TargetUnderReticle()
    {
        if (_failed) return;

        try
        {
            var targets = TargetSystem.Instance();
            var settings = PluginState.Config.Targeting;
            if (targets == null || !TargetingPosition.TryGet(settings.VerticalOffset, out var point, out _))
                return;

            var candidate = ScreenTargetPicker.Pick(targets, point, settings);
            if (candidate != null && (targets->Target != candidate || targets->SoftTarget != null))
                targets->SetHardTarget(candidate);

            if (!OwnsSoftTarget(targets))
                ForgetSoftTarget();
        }
        catch (Exception ex)
        {
            Suspend(ex);
        }
    }

    private void Update(IFramework framework)
    {
        if (_failed) return;

        try
        {
            UpdateTarget();
        }
        catch (Exception ex)
        {
            Suspend(ex);
        }
    }

    private void Suspend(Exception ex)
    {
        HasCandidate = false;
        _failed = true;
        Error = "Targeting is unavailable. Reload MouseLock to retry.";
        Service.Logger.Error(ex, "Screen targeting failed; suspended until plugin reload.");
        _actionGuard.SetEnabled(false);
        TryReleaseSoftTarget();
    }

    private void UpdateTarget()
    {
        HasCandidate = false;
        var settings = PluginState.Config.Targeting;
        var autoTargetingEnabled = IsAutoTargetingEnabled(settings);
        _actionGuard.SetEnabled(autoTargetingEnabled && settings.Mode == AimTargetMode.Soft &&
                                settings.KeepSoftTargetAfterAction);
        if (!autoTargetingEnabled && !settings.ShowReticle && _ownedSoftTarget == null)
        {
            _cameraMotion = default;
            return;
        }

        var targets = TargetSystem.Instance();
        if (targets == null)
        {
            _cameraMotion = default;
            return;
        }
        if (!OwnsSoftTarget(targets))
            ForgetSoftTarget();

        if (!CanTarget() ||
            !TargetingPosition.TryGet(settings.VerticalOffset, out var point, out _))
        {
            _cameraMotion = default;
            ReleaseSoftTarget(targets);
            return;
        }

        if (!autoTargetingEnabled || settings.Mode != AimTargetMode.Soft ||
            (OwnsSoftTarget(targets) && !ScreenTargetPicker.IsEligible(targets->SoftTarget, settings)))
            ReleaseSoftTarget(targets);

        var control = Control.Instance();
        var camera = control == null ? null : control->CameraManager.Camera;
        if (camera == null)
        {
            _cameraMotion = default;
            return;
        }
        var cameraMoved = _cameraMotion.Update(camera, camera->DirH, camera->DirV);
        if (!autoTargetingEnabled)
            _cameraMotion = default;
        if (!settings.ShowReticle && (!autoTargetingEnabled || !cameraMoved)) return;

        var candidate = ScreenTargetPicker.Pick(targets, point, settings);
        HasCandidate = candidate != null;
        if (!autoTargetingEnabled || !cameraMoved) return;
        if (candidate == null)
        {
            if (settings.Mode == AimTargetMode.Soft && settings.KeepSoftTargetOnLookAway &&
                OwnsSoftTarget(targets) && ScreenTargetPicker.IsEligible(targets->SoftTarget, settings))
                return;

            ReleaseSoftTarget(targets);
            return;
        }

        switch (settings.Mode)
        {
            case AimTargetMode.Soft:
                if (targets->SoftTarget != candidate)
                {
                    targets->SetSoftTarget(candidate);
                    if (targets->SoftTarget == candidate)
                    {
                        _ownedSoftTarget = candidate;
                        _ownedSoftTargetId = candidate->GetGameObjectId();
                    }
                }
                break;
            case AimTargetMode.Focus:
                if (targets->FocusTarget != candidate)
                    targets->SetFocusTargetByObjectId(candidate->GetGameObjectId());
                break;
        }
    }

    private bool ShouldPreserveSoftTargetAfterAction()
    {
        if (_failed || !CanTarget()) return false;

        var settings = PluginState.Config.Targeting;
        if (!IsAutoTargetingEnabled(settings) || settings.Mode != AimTargetMode.Soft) return false;
        if (!settings.KeepSoftTargetAfterAction) return false;

        var targets = TargetSystem.Instance();
        return OwnsSoftTarget(targets) && ScreenTargetPicker.IsEligible(targets->SoftTarget, settings);
    }

    private static bool IsAutoTargetingEnabled(TargetingSettings settings)
        => settings.Enabled && (!settings.OnlyInCombat || Service.Condition[ConditionFlag.InCombat]);

    private static bool CanTarget()
    {
        if (!PluginState.Config.General.Enabled || !Service.ClientState.IsLoggedIn) return false;
        if (PluginState.ConfigWindow.IsOpen) return false;
        if (PluginState.MouseLookService?.Status.Kind != MouseLookStatusKind.Active) return false;

        var input = UIInputData.Instance();
        if (input == null || !input->CursorInputs.IsGameWindowFocused) return false;

        var ui = RaptureAtkUnitManager.Instance();
        return ui != null && !ui->IsUiFading;
    }

    private bool OwnsSoftTarget(TargetSystem* targets)
        => _ownedSoftTarget != null && targets != null &&
           targets->SoftTarget == _ownedSoftTarget &&
           targets->SoftTarget->GetGameObjectId() == _ownedSoftTargetId;

    private void TryReleaseSoftTarget()
    {
        if (_ownedSoftTarget == null) return;

        try
        {
            ReleaseSoftTarget(TargetSystem.Instance());
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not release MouseLock's soft target.");
        }
    }

    private void ReleaseSoftTarget(TargetSystem* targets)
    {
        if (OwnsSoftTarget(targets))
            targets->SetSoftTarget(null);

        ForgetSoftTarget();
    }

    private void ForgetSoftTarget()
    {
        _ownedSoftTarget = null;
        _ownedSoftTargetId = default;
    }
}
