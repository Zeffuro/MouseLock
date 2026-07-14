using System;
using Dalamud.Plugin.Services;
using MouseLock.Configuration;
using MouseLock.MouseLook;

namespace MouseLock.Input.Keybinds;

internal sealed class ToggleKeybindListener : IDisposable
{
    private readonly TextInputMonitor _textInputMonitor;
    private bool _wasDown;

    public ToggleKeybindListener(TextInputMonitor textInputMonitor)
    {
        _textInputMonitor = textInputMonitor;
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var settings = PluginState.Config.ToggleKeybind;
        if (!settings.Enabled || settings.Keybind.IsEmpty)
        {
            _wasDown = false;
            return;
        }

        if (settings.IgnoreWhileTextInputActive && _textInputMonitor.IsTextInputActive())
        {
            _wasDown = settings.Keybind.IsPressed();
            return;
        }

        var isDown = settings.Keybind.IsPressed();
        if (isDown && !_wasDown)
        {
            MouseLockStateController.ToggleEnabled();
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
