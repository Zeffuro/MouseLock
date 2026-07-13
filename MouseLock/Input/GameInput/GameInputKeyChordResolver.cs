using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;

namespace MouseLock.Input.GameInput;

internal static unsafe class GameInputKeyChordResolver
{
    public static bool TryResolve(UIInputData* inputData, CuratedGameInput input, out KeyChord chord)
    {
        chord = default;

        if (!MouseButtonGameInputResolver.TryResolveGameInput(input, out var inputId))
        {
            return false;
        }

        var keybind = inputData->GetKeybind(inputId);
        if (keybind is null)
        {
            return false;
        }

        if (TryCreateKeyChord(keybind->KeySettings[0], out chord))
        {
            return true;
        }

        return TryCreateKeyChord(keybind->KeySettings[1], out chord);
    }

    public static void Apply(UIInputData* inputData, KeyChord chord, KeyStateFlags state)
    {
        ApplyChordKeyState(inputData, chord, state);

        if (state == KeyStateFlags.None || state == KeyStateFlags.Released)
        {
            inputData->CurrentKeyModifier &= ~chord.Modifier;
        }
        else
        {
            inputData->CurrentKeyModifier |= chord.Modifier;
        }

        inputData->KeyboardInputsChanged = true;
    }

    public static void ClearFramework(KeyChord chord)
        => ApplyFrameworkChordKeyState(chord, KeyStateFlags.None);

    private static bool TryCreateKeyChord(KeySetting keySetting, out KeyChord chord)
    {
        chord = default;

        if (!IsInjectableKey(keySetting.Key))
        {
            return false;
        }

        chord = new KeyChord(keySetting.Key, keySetting.KeyModifier);
        return true;
    }

    private static bool IsInjectableKey(SeVirtualKey key)
    {
        return key is not (
                   SeVirtualKey.NO_KEY or
                   SeVirtualKey.LBUTTON or
                   SeVirtualKey.RBUTTON or
                   SeVirtualKey.CANCEL or
                   SeVirtualKey.MBUTTON or
                   SeVirtualKey.XBUTTON1 or
                   SeVirtualKey.XBUTTON2)
               && (int)key is > 0 and < 159;
    }

    private static void ApplyChordKeyState(UIInputData* inputData, KeyChord chord, KeyStateFlags state)
    {
        if (chord.HasModifier(KeyModifierFlag.Ctrl))
        {
            ApplyKeyState(inputData, SeVirtualKey.CONTROL, state);
        }

        if (chord.HasModifier(KeyModifierFlag.Shift))
        {
            ApplyKeyState(inputData, SeVirtualKey.SHIFT, state);
        }

        if (chord.HasModifier(KeyModifierFlag.Alt))
        {
            ApplyKeyState(inputData, SeVirtualKey.MENU, state);
        }

        if (!chord.PrimaryKeyIsModifier())
        {
            ApplyKeyState(inputData, chord.PrimaryKey, state);
        }
    }

    private static void ApplyFrameworkChordKeyState(KeyChord chord, KeyStateFlags state)
    {
        if (chord.HasModifier(KeyModifierFlag.Ctrl))
        {
            ApplyFrameworkKeyState(SeVirtualKey.CONTROL, state);
        }

        if (chord.HasModifier(KeyModifierFlag.Shift))
        {
            ApplyFrameworkKeyState(SeVirtualKey.SHIFT, state);
        }

        if (chord.HasModifier(KeyModifierFlag.Alt))
        {
            ApplyFrameworkKeyState(SeVirtualKey.MENU, state);
        }

        if (!chord.PrimaryKeyIsModifier())
        {
            ApplyFrameworkKeyState(chord.PrimaryKey, state);
        }
    }

    private static void ApplyKeyState(UIInputData* inputData, SeVirtualKey key, KeyStateFlags state)
    {
        var keyIndex = (int)key;
        inputData->KeyboardInputs.KeyState[keyIndex] = state;
        ApplyFrameworkKeyState(key, state);
    }

    private static void ApplyFrameworkKeyState(SeVirtualKey key, KeyStateFlags state)
    {
        var keyIndex = (int)key;
        var framework = Framework.Instance();
        if (framework is null)
        {
            return;
        }

        framework->KeyboardInputs.KeyState[keyIndex] = state;
        framework->KeyboardInputs2.KeyState[keyIndex] = state;
    }
}
