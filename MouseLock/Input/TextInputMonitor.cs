using System;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.Integrations;

namespace MouseLock.Input;

internal sealed class TextInputMonitor : IDisposable
{
    private readonly ChatTwoTextInputMonitor _chatTwoTextInputMonitor = new();
    private bool _nativeTextInputActive;

    public bool IsTextInputActive()
    {
        if (_nativeTextInputActive)
        {
            return true;
        }

        return IsPluginTextInputActive();
    }

    public unsafe bool IsTextInputActive(AtkModule* atkModule)
    {
        if (_nativeTextInputActive)
        {
            return true;
        }

        if (atkModule is not null && atkModule->IsTextInputActive())
        {
            return true;
        }

        return IsPluginTextInputActive();
    }

    private bool IsPluginTextInputActive()
    {
        if (ImGui.GetIO().WantTextInput)
        {
            return true;
        }

        return _chatTwoTextInputMonitor.IsInputFocused;
    }

    public unsafe void UpdateNativeTextInput(AtkModule* atkModule)
        => _nativeTextInputActive = atkModule is not null && atkModule->IsTextInputActive();

    public void Dispose()
    {
        _chatTwoTextInputMonitor.Dispose();
    }
}
