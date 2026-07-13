using System;
using Dalamud.Plugin.Ipc;
using MouseLock.Compatibility;
using MouseLock.Configuration;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Ipc;

internal sealed class MouseLockIpcProvider : IDisposable
{
    public const string IsEnabledName = "MouseLock.IsEnabled";
    public const string IsMouseLookActiveName = "MouseLock.IsMouseLookActive";
    public const string SetEnabledName = "MouseLock.SetEnabled";
    public const string ToggleEnabledName = "MouseLock.ToggleEnabled";
    public const string GetStatusName = "MouseLock.GetStatus";
    public const string GetPauseReasonName = "MouseLock.GetPauseReason";
    public const string SetSuspendedName = "MouseLock.SetSuspended";
    public const string IsSuspendedName = "MouseLock.IsSuspended";
    public const string GetSuspensionSourcesName = "MouseLock.GetSuspensionSources";
    public const string ClearSuspensionsName = "MouseLock.ClearSuspensions";
    public const string StateChangedName = "MouseLock.StateChanged";

    private readonly ICallGateProvider<bool> _isEnabledProvider;
    private readonly ICallGateProvider<bool> _isMouseLookActiveProvider;
    private readonly ICallGateProvider<bool, bool> _setEnabledProvider;
    private readonly ICallGateProvider<bool> _toggleEnabledProvider;
    private readonly ICallGateProvider<string> _getStatusProvider;
    private readonly ICallGateProvider<string> _getPauseReasonProvider;
    private readonly ICallGateProvider<string, bool, bool> _setSuspendedProvider;
    private readonly ICallGateProvider<bool> _isSuspendedProvider;
    private readonly ICallGateProvider<string> _getSuspensionSourcesProvider;
    private readonly ICallGateProvider<int> _clearSuspensionsProvider;
    private readonly ICallGateProvider<string, bool> _stateChangedProvider;

    public MouseLockIpcProvider()
    {
        _isEnabledProvider = Service.PluginInterface.GetIpcProvider<bool>(IsEnabledName);
        _isMouseLookActiveProvider = Service.PluginInterface.GetIpcProvider<bool>(IsMouseLookActiveName);
        _setEnabledProvider = Service.PluginInterface.GetIpcProvider<bool, bool>(SetEnabledName);
        _toggleEnabledProvider = Service.PluginInterface.GetIpcProvider<bool>(ToggleEnabledName);
        _getStatusProvider = Service.PluginInterface.GetIpcProvider<string>(GetStatusName);
        _getPauseReasonProvider = Service.PluginInterface.GetIpcProvider<string>(GetPauseReasonName);
        _setSuspendedProvider = Service.PluginInterface.GetIpcProvider<string, bool, bool>(SetSuspendedName);
        _isSuspendedProvider = Service.PluginInterface.GetIpcProvider<bool>(IsSuspendedName);
        _getSuspensionSourcesProvider = Service.PluginInterface.GetIpcProvider<string>(GetSuspensionSourcesName);
        _clearSuspensionsProvider = Service.PluginInterface.GetIpcProvider<int>(ClearSuspensionsName);
        _stateChangedProvider = Service.PluginInterface.GetIpcProvider<string, bool>(StateChangedName);

        _isEnabledProvider.RegisterFunc(IsEnabled);
        _isMouseLookActiveProvider.RegisterFunc(IsMouseLookActive);
        _setEnabledProvider.RegisterFunc(SetEnabled);
        _toggleEnabledProvider.RegisterFunc(ToggleEnabled);
        _getStatusProvider.RegisterFunc(GetStatus);
        _getPauseReasonProvider.RegisterFunc(GetPauseReason);
        _setSuspendedProvider.RegisterFunc(SetSuspended);
        _isSuspendedProvider.RegisterFunc(IsSuspended);
        _getSuspensionSourcesProvider.RegisterFunc(GetSuspensionSources);
        _clearSuspensionsProvider.RegisterFunc(ClearSuspensions);

        if (PluginState.MouseLookService is not null)
        {
            PluginState.MouseLookService.StatusChanged += OnStatusChanged;
        }
    }

    public void Dispose()
    {
        if (PluginState.MouseLookService is not null)
        {
            PluginState.MouseLookService.StatusChanged -= OnStatusChanged;
        }

        _isEnabledProvider.UnregisterFunc();
        _isMouseLookActiveProvider.UnregisterFunc();
        _setEnabledProvider.UnregisterFunc();
        _toggleEnabledProvider.UnregisterFunc();
        _getStatusProvider.UnregisterFunc();
        _getPauseReasonProvider.UnregisterFunc();
        _setSuspendedProvider.UnregisterFunc();
        _isSuspendedProvider.UnregisterFunc();
        _getSuspensionSourcesProvider.UnregisterFunc();
        _clearSuspensionsProvider.UnregisterFunc();
    }

    private static bool IsEnabled() => PluginState.Config.General.Enabled;

    private static bool IsMouseLookActive() => PluginState.MouseLookService is { IsActive: true };

    private static string GetStatus() => CurrentStatus().IpcStatus;

    private static string GetPauseReason() => CurrentStatus().ReasonLabel;

    private static bool IsSuspended() => ExternalSuspensionState.IsSuspended;

    private static string GetSuspensionSources() => ExternalSuspensionState.SourcesSummary;

    private static bool SetEnabled(bool enabled)
    {
        MouseLockSettingsActions.SetEnabled(enabled);
        return IsEnabled();
    }

    private static bool ToggleEnabled()
    {
        MouseLockSettingsActions.ToggleEnabled();
        return IsEnabled();
    }

    private static bool SetSuspended(string source, bool suspended)
    {
        var isSuspended = ExternalSuspensionState.SetSuspended(source, suspended);
        PluginState.MouseLookService?.RefreshCurrentStatus();
        return isSuspended;
    }

    private static int ClearSuspensions()
    {
        var clearedCount = ExternalSuspensionState.Clear();
        PluginState.MouseLookService?.RefreshCurrentStatus();
        return clearedCount;
    }

    private void OnStatusChanged(MouseLookStatus status)
    {
        _stateChangedProvider.SendMessage(status.IpcStatus);
    }

    private static MouseLookStatus CurrentStatus()
        => PluginState.MouseLookService?.Status
           ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
}
