using System;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class NativeMouseDragState
{
    // The game checks how many pixels the mouse has been moved and will treat it as a click if it's 10 pixels or under.
    private const float NativeClickSuppressionDragDistance = 11.0f;

    private readonly NativeInputManager* _inputManager;

    public NativeMouseDragState()
    {
        try
        {
            _inputManager = (NativeInputManager*)InputManager.Instance();
            if (_inputManager is null)
            {
                Service.Logger.Error("Could not resolve InputManager instance.");
                return;
            }
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not resolve InputManager instance.");
        }
    }

    public bool IsAvailable => _inputManager is not null;
    public bool IsActive { get; private set; }

    public void Apply(UIInputData* inputData)
    {
        if (_inputManager is null)
        {
            return;
        }

        _inputManager->MouseButtonHoldState = MouseLookButtons.VirtualDragState;
        _inputManager->MouseDragDistance = NativeClickSuppressionDragDistance;
        _inputManager->MouseDeltaX = -inputData->CursorInputs.DeltaX;
        _inputManager->MouseDeltaY = -inputData->CursorInputs.DeltaY;
        _inputManager->MouseDragStartX = inputData->CursorInputs.PositionX;
        _inputManager->MouseDragStartY = inputData->CursorInputs.PositionY;
        _inputManager->MouseDragActive = 1;

        IsActive = true;
    }

    public void Release(UIInputData* inputData)
    {
        if (_inputManager is null || !IsActive)
        {
            return;
        }

        _inputManager->MouseButtonHoldState = InputManager.MouseButtonHoldState.None;
        _inputManager->MouseDragDistance = 0;
        _inputManager->MouseDeltaX = 0;
        _inputManager->MouseDeltaY = 0;
        _inputManager->MouseDragStartX = inputData->CursorInputs.PositionX;
        _inputManager->MouseDragStartY = inputData->CursorInputs.PositionY;
        _inputManager->MouseDragActive = 0;

        IsActive = false;
    }

    public void DeactivateWithoutRelease()
    {
        IsActive = false;
    }
}
