using Dalamud.Game.ClientState.Keys;

namespace MouseLock.Configuration;

public sealed class ToggleKeybindSettings
{
    private RecordedKeybind _keybind = new();

    public bool Enabled { get; set; }
    public bool ShowChatFeedback { get; set; }
    public VirtualKey Key { get; set; } = VirtualKey.F11;
    public RecordedKeybind Keybind
    {
        get => _keybind;
        set => _keybind = value ?? new RecordedKeybind();
    }

    public bool IgnoreWhileTextInputActive { get; set; } = true;

    public void EnsureInitialized()
    {
        _keybind ??= new RecordedKeybind();
        _keybind.Modifiers ??= [];

        if (_keybind.IsEmpty && Key != VirtualKey.NO_KEY)
        {
            _keybind.Key = Key;
        }
    }

    public void SetKeybind(RecordedKeybind keybind)
    {
        Keybind = keybind;
        Key = keybind.Key;
    }
}
