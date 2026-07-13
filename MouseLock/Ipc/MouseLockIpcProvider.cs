using System;
using Dalamud.Plugin.Ipc;
using MouseLock.Configuration;

namespace MouseLock.Ipc;

internal sealed class MouseLockIpcProvider : IDisposable
{
    public const string IsEnabledName = "MouseLock.IsEnabled";
    public const string IsMouseLookActiveName = "MouseLock.IsMouseLookActive";
    public const string SetEnabledName = "MouseLock.SetEnabled";
    public const string ToggleEnabledName = "MouseLock.ToggleEnabled";

    private readonly ICallGateProvider<bool> _isEnabledProvider;
    private readonly ICallGateProvider<bool> _isMouseLookActiveProvider;
    private readonly ICallGateProvider<bool, bool> _setEnabledProvider;
    private readonly ICallGateProvider<bool> _toggleEnabledProvider;

    public MouseLockIpcProvider()
    {
        _isEnabledProvider = Service.PluginInterface.GetIpcProvider<bool>(IsEnabledName);
        _isMouseLookActiveProvider = Service.PluginInterface.GetIpcProvider<bool>(IsMouseLookActiveName);
        _setEnabledProvider = Service.PluginInterface.GetIpcProvider<bool, bool>(SetEnabledName);
        _toggleEnabledProvider = Service.PluginInterface.GetIpcProvider<bool>(ToggleEnabledName);

        _isEnabledProvider.RegisterFunc(IsEnabled);
        _isMouseLookActiveProvider.RegisterFunc(IsMouseLookActive);
        _setEnabledProvider.RegisterFunc(SetEnabled);
        _toggleEnabledProvider.RegisterFunc(ToggleEnabled);
    }

    public void Dispose()
    {
        _isEnabledProvider.UnregisterFunc();
        _isMouseLookActiveProvider.UnregisterFunc();
        _setEnabledProvider.UnregisterFunc();
        _toggleEnabledProvider.UnregisterFunc();
    }

    private static bool IsEnabled() => PluginState.Config.General.Enabled;

    private static bool IsMouseLookActive() => PluginState.MouseLookService is { IsActive: true};

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
}
