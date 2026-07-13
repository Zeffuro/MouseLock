using System;
using Dalamud.Bindings.ImGui;
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
        if (!settings.Enabled || settings.Keybind.IsEmpty)
        {
            _wasDown = false;
            return;
        }

        if (settings.IgnoreWhileTextInputActive && ImGui.GetIO().WantTextInput)
        {
            _wasDown = settings.Keybind.IsPressed();
            return;
        }

        var isDown = settings.Keybind.IsPressed();
        if (isDown && !_wasDown)
        {
            MouseLockSettingsActions.ToggleEnabled();
            if (settings.ShowChatFeedback)
            {
                Service.ChatGui.Print(
                    PluginState.Config.General.Enabled ? "MouseLock enabled." : "MouseLock disabled.",
                    "MouseLock");
            }
        }

        _wasDown = isDown;
    }
}
