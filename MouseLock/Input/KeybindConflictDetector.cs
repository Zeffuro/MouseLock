using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Input;
using GameKeybind = FFXIVClientStructs.FFXIV.Client.System.Input.Keybind;

namespace MouseLock.Input;

internal static class KeybindConflictDetector
{
    public static unsafe IReadOnlyList<InputId> GetGameConflicts(RecordedKeybind keybind)
    {
        var conflicts = new List<InputId>();
        if (keybind.IsEmpty)
        {
            return conflicts;
        }

        var inputData = UIInputData.Instance();
        if (inputData is null)
        {
            return conflicts;
        }

        var keybinds = inputData->GetKeybindSpan();
        for (var index = 0; index < keybinds.Length; index++)
        {
            if (IsGameKeybindMatch(keybinds[index], keybind))
            {
                conflicts.Add((InputId)index);
            }
        }

        return conflicts;
    }

    private static bool IsGameKeybindMatch(GameKeybind gameKeybind, RecordedKeybind keybind)
    {
        foreach (var keySetting in gameKeybind.KeySettings)
        {
            if (IsGameKeySettingMatch(keySetting, keybind))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGameKeySettingMatch(KeySetting keySetting, RecordedKeybind keybind)
    {
        if ((int)keybind.Key != (int)keySetting.Key)
        {
            return false;
        }

        return GetGameModifiers(keySetting.KeyModifier).SetEquals(keybind.Modifiers);
    }

    private static HashSet<VirtualKey> GetGameModifiers(KeyModifierFlag modifier)
    {
        var modifiers = new HashSet<VirtualKey>();
        if (modifier.HasFlag(KeyModifierFlag.Ctrl))
        {
            modifiers.Add(VirtualKey.CONTROL);
        }

        if (modifier.HasFlag(KeyModifierFlag.Shift))
        {
            modifiers.Add(VirtualKey.SHIFT);
        }

        if (modifier.HasFlag(KeyModifierFlag.Alt))
        {
            modifiers.Add(VirtualKey.MENU);
        }

        return modifiers;
    }
}
