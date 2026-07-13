using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;

namespace MouseLock.Actions;

internal static unsafe class GameInputKeyChordResolver
{
    public static bool TryResolve(UIInputData* inputData, CuratedGameInput input, out KeyChord chord)
    {
        chord = default;

        var inputId = MouseButtonGameInputResolver.ResolveGameInput(input);
        var inputIndex = (int)inputId;
        if (inputIndex < 0 || inputIndex >= inputData->NumKeybinds || inputData->Keybinds is null)
        {
            return false;
        }

        var keybind = inputData->Keybinds[inputIndex];
        if (TryCreateKeyChord(keybind.KeySettings[0], out chord))
        {
            return true;
        }

        return TryCreateKeyChord(keybind.KeySettings[1], out chord);
    }

    public static void Apply(UIInputData* inputData, KeyChord chord, KeyStateFlags state)
    {
        var keys = chord.Keys;
        foreach (var key in keys)
        {
            ApplyKeyState(inputData, key, state);
        }

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

    private static void ApplyKeyState(UIInputData* inputData, SeVirtualKey key, KeyStateFlags state)
    {
        var keyIndex = (int)key;
        inputData->KeyboardInputs.KeyState[keyIndex] = state;

        var framework = Framework.Instance();
        if (framework is null)
        {
            return;
        }

        framework->KeyboardInputs.KeyState[keyIndex] = state;
        framework->KeyboardInputs2.KeyState[keyIndex] = state;
    }
}
