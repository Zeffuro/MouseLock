using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using MouseLock.Input;

namespace MouseLock.Configuration;

public sealed class RecordedKeybind
{
    private List<VirtualKey> _modifiers = [];

    public VirtualKey Key { get; set; } = VirtualKey.NO_KEY;

    public List<VirtualKey> Modifiers
    {
        get => _modifiers;
        set => _modifiers = value ?? [];
    }

    public bool IsEmpty => Key == VirtualKey.NO_KEY;

    public bool IsPressed(bool exactModifiers = true)
    {
        if (IsEmpty || !Service.KeyState.IsVirtualKeyValid(Key) || !Service.KeyState[Key])
        {
            return false;
        }

        foreach (var modifier in Modifiers.Distinct())
        {
            if (!Service.KeyState.IsVirtualKeyValid(modifier) || !Service.KeyState[modifier])
            {
                return false;
            }
        }

        return !exactModifiers || MouseLock.Input.VirtualKeyExtensions.ModifierKeys.All(modifier =>
            Modifiers.Contains(modifier) == (Service.KeyState.IsVirtualKeyValid(modifier) && Service.KeyState[modifier]));
    }

    public override string ToString()
    {
        return IsEmpty
            ? "None"
            : string.Join(" + ", Modifiers.Distinct().OrderByModifier().Append(Key).Select(key => key.GetDisplayName()));
    }
}
