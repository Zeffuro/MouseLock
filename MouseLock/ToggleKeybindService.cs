using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using MouseLock.Configuration;

namespace MouseLock;

public sealed class ToggleKeybindService : IDisposable
{
    private bool _wasDown;

    public ToggleKeybindService()
    {
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var settings = PluginState.Config.General.ToggleKeybind;
        if (!settings.Enabled || settings.Key == VirtualKey.NO_KEY)
        {
            _wasDown = false;
            return;
        }

        if (settings.IgnoreWhileTextInputActive && ImGui.GetIO().WantTextInput)
        {
            _wasDown = Service.KeyState[settings.Key];
            return;
        }

        var isDown = Service.KeyState[settings.Key];
        if (isDown && !_wasDown)
        {
            MouseLockSettingsActions.ToggleEnabled();
        }

        _wasDown = isDown;
    }
}
