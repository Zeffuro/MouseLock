using System;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class MouseDragState
{
    // The game checks how many pixels the mouse has been moved and will treat it as a click if it's 10 pixels or under.
    private const float NativeClickSuppressionDragDistance = 11.0f;

    private readonly InputManager* _inputManager;

    public MouseDragState()
    {
        try
        {
            _inputManager = InputManager.Instance();
            if (_inputManager is null)
            {
                Service.Logger.Error("Could not resolve InputManager instance.");
            }
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not resolve InputManager instance.");
        }
    }

    public bool IsAvailable => _inputManager is not null;
    public bool IsActive { get; private set; }

    public void Apply(UIInputData* inputData, bool classicForwardHeld)
    {
        if (_inputManager is null)
        {
            return;
        }

        var heldMouseButtons = MouseLookButtons.VirtualDragState;
        if (classicForwardHeld)
        {
            heldMouseButtons |= InputManager.MouseButtonHoldState.Left;
        }

        _inputManager->HeldMouseButtons = heldMouseButtons;
        _inputManager->MouseDragDistance = NativeClickSuppressionDragDistance;
        _inputManager->MouseDragDeltaX = -inputData->CursorInputs.DeltaX;
        _inputManager->MouseDragDeltaY = -inputData->CursorInputs.DeltaY;
        _inputManager->MouseDragStartX = inputData->CursorInputs.PositionX;
        _inputManager->MouseDragStartY = inputData->CursorInputs.PositionY;
        _inputManager->MouseDragActive = true;

        IsActive = true;
    }

    public void Release(UIInputData* inputData)
    {
        if (_inputManager is null || !IsActive)
        {
            return;
        }

        ReleaseNativeState(inputData->CursorInputs.PositionX, inputData->CursorInputs.PositionY);
    }

    public void Release()
    {
        if (_inputManager is not null && IsActive)
        {
            ReleaseNativeState(0, 0);
            return;
        }

        IsActive = false;
    }

    private void ReleaseNativeState(int startX, int startY)
    {
        _inputManager->HeldMouseButtons = InputManager.MouseButtonHoldState.None;
        _inputManager->MouseDragDistance = 0;
        _inputManager->MouseDragDeltaX = 0;
        _inputManager->MouseDragDeltaY = 0;
        _inputManager->MouseDragStartX = startX;
        _inputManager->MouseDragStartY = startY;
        _inputManager->MouseDragActive = false;

        IsActive = false;
    }
}
