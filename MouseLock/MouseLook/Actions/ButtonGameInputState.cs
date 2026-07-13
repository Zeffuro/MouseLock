using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;

namespace MouseLock.MouseLook.Actions;

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
        bool buttonPressed,
        bool buttonHeld,
        bool buttonReleased,
        bool allowNewActions)
    {
        if (buttonReleased || !buttonHeld)
        {
            _oneShotConsumedUntilMouseRelease = false;
            AdvanceRelease(inputData);
            return;
        }

        if (!allowNewActions)
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

        if (!heldInput && !buttonPressed)
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

        var state = buttonPressed || changedInput
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
        _activeChord = default;
        _activeInput = default;
        _hasActiveChord = false;
        _releaseFrameApplied = false;
    }
}
