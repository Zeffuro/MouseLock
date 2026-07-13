using System;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Ipc;

namespace MouseLock.MouseLook.Activation;

internal sealed class ChatTwoTypingState : IDisposable
{
    private readonly ICallGateSubscriber<object> _getChatInputState;
    private readonly ICallGateSubscriber<object, object?> _chatInputStateChanged;

    private bool _inputFocused;

    public ChatTwoTypingState()
    {
        _getChatInputState = Service.PluginInterface.GetIpcSubscriber<object>("ChatTwo.GetChatInputState");
        _chatInputStateChanged = Service.PluginInterface.GetIpcSubscriber<object, object?>("ChatTwo.ChatInputStateChanged");

        try
        {
            _chatInputStateChanged.Subscribe(OnChatInputStateChanged);
            Refresh();
        }
        catch
        {
            _inputFocused = false;
        }
    }

    public bool IsInputFocused => _inputFocused;

    public void Dispose()
    {
        try
        {
            _chatInputStateChanged.Unsubscribe(OnChatInputStateChanged);
        }
        catch
        {
            // Chat2 not available?
        }
    }

    private void Refresh()
    {
        try
        {
            OnChatInputStateChanged(_getChatInputState.InvokeFunc());
        }
        catch
        {
            _inputFocused = false;
        }
    }

    private void OnChatInputStateChanged(object state)
    {
        _inputFocused = state is ITuple { Length: > 1 } tuple && tuple[1] is true;
    }
}
