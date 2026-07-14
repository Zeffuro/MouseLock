using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Input;

namespace MouseLock.Input.GameInput;

internal sealed unsafe class ButtonGameInputState
{
    private KeyChord _activeChord;
    private CuratedGameInput _activeInput;
    private bool _hasActiveChord;
    private bool _oneShotConsumedUntilMouseRelease;
    private bool _releaseFrameApplied;

    public void Update(
        UIInputData* inputData,
        CuratedGameInput input,
        MouseButtonState button)
    {
        if (button.Released || !button.Held)
        {
            _oneShotConsumedUntilMouseRelease = false;
            AdvanceRelease(inputData);
            return;
        }

        if (!button.AllowNewActions)
        {
            AdvanceRelease(inputData);
            return;
        }

        var heldInput = MouseButtonGameInputResolver.IsHeldInput(input);
        if (!heldInput && _oneShotConsumedUntilMouseRelease)
        {
            AdvanceRelease(inputData);
            return;
        }

        if (!heldInput && !button.Pressed)
        {
            AdvanceRelease(inputData);
            return;
        }

        if (!GameInputKeyChordResolver.TryResolve(inputData, input, out var chord))
        {
            AdvanceRelease(inputData);
            return;
        }

        var changedInput = !_hasActiveChord || _activeInput != input || !_activeChord.Equals(chord);
        if (changedInput)
        {
            Clear(inputData);
            _activeInput = input;
            _activeChord = chord;
            _hasActiveChord = true;
            _releaseFrameApplied = false;
        }

        var state = button.Pressed || changedInput
            ? KeyStateFlags.Down | KeyStateFlags.Pressed
            : KeyStateFlags.Down;

        GameInputKeyChordResolver.Apply(inputData, _activeChord, state);

        if (!heldInput)
        {
            _oneShotConsumedUntilMouseRelease = true;
        }
    }

    public void AdvanceRelease(UIInputData* inputData)
    {
        if (!_hasActiveChord)
        {
            return;
        }

        if (!_releaseFrameApplied)
        {
            GameInputKeyChordResolver.Apply(inputData, _activeChord, KeyStateFlags.Released);
            _releaseFrameApplied = true;
            return;
        }

        Clear(inputData);
    }

    public void Clear(UIInputData* inputData)
    {
        if (!_hasActiveChord)
        {
            return;
        }

        GameInputKeyChordResolver.Apply(inputData, _activeChord, KeyStateFlags.None);
        Reset();
    }

    public void EmergencyClear()
    {
        if (!_hasActiveChord)
        {
            return;
        }

        GameInputKeyChordResolver.ClearFramework(_activeChord);
        Reset();
    }

    private void Reset()
    {
        _activeChord = default;
        _activeInput = default;
        _hasActiveChord = false;
        _oneShotConsumedUntilMouseRelease = false;
        _releaseFrameApplied = false;
    }
}
