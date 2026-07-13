using System;
using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace MouseLock.Input.GameInput;

internal readonly struct KeyChord(SeVirtualKey primaryKey, KeyModifierFlag modifier) : IEquatable<KeyChord>
{
    public readonly SeVirtualKey PrimaryKey = primaryKey;
    public readonly KeyModifierFlag Modifier = modifier;

    public bool Equals(KeyChord other)
        => PrimaryKey == other.PrimaryKey && Modifier == other.Modifier;

    public override bool Equals(object? obj)
        => obj is KeyChord other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(PrimaryKey, Modifier);

    public bool HasModifier(KeyModifierFlag modifier)
        => (Modifier & modifier) != 0;

    public bool PrimaryKeyIsModifier()
        => PrimaryKey switch
        {
            SeVirtualKey.CONTROL => HasModifier(KeyModifierFlag.Ctrl),
            SeVirtualKey.SHIFT => HasModifier(KeyModifierFlag.Shift),
            SeVirtualKey.MENU => HasModifier(KeyModifierFlag.Alt),
            _ => false,
        };
}
