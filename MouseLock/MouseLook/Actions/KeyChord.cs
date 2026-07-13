using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace MouseLock.MouseLook.Actions;

internal readonly struct KeyChord(SeVirtualKey primaryKey, KeyModifierFlag modifier) : IEquatable<KeyChord>
{
    public readonly SeVirtualKey PrimaryKey = primaryKey;
    public readonly KeyModifierFlag Modifier = modifier;
    public readonly SeVirtualKey[] Keys = CreateKeys(primaryKey, modifier);

    public bool Equals(KeyChord other)
        => PrimaryKey == other.PrimaryKey && Modifier == other.Modifier;

    public override bool Equals(object? obj)
        => obj is KeyChord other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(PrimaryKey, Modifier);

    private static SeVirtualKey[] CreateKeys(SeVirtualKey primaryKey, KeyModifierFlag modifier)
    {
        var keys = new List<SeVirtualKey>(4);
        if ((modifier & KeyModifierFlag.Ctrl) != 0)
        {
            keys.Add(SeVirtualKey.CONTROL);
        }

        if ((modifier & KeyModifierFlag.Shift) != 0)
        {
            keys.Add(SeVirtualKey.SHIFT);
        }

        if ((modifier & KeyModifierFlag.Alt) != 0)
        {
            keys.Add(SeVirtualKey.MENU);
        }

        if (!keys.Contains(primaryKey))
        {
            keys.Add(primaryKey);
        }

        return keys.ToArray();
    }
}
