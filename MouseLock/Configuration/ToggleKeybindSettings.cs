using Dalamud.Game.ClientState.Keys;

namespace MouseLock.Configuration;

public sealed class ToggleKeybindSettings
{
    public bool Enabled { get; set; }
    public VirtualKey Key { get; set; } = VirtualKey.F10;
    public bool IgnoreWhileTextInputActive { get; set; } = true;
}
