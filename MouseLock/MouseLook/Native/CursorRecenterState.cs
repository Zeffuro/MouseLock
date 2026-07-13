using System;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class CursorRecenterState
{
    private readonly delegate* unmanaged<int, int, void> _scheduleCursorMove;

    private bool _hasRestorePosition;
    private bool _hasScheduledCursorMoveDelta;
    private bool _clearScheduledCursorMoveDeltaOnCompensation;
    private int _restorePositionX;
    private int _restorePositionY;
    private int _scheduledCursorMoveDeltaX;
    private int _scheduledCursorMoveDeltaY;

    public CursorRecenterState()
    {
        try
        {
            if (!Service.SigScanner.TryScanText(MouseLookSignatures.ScheduleCursorMove, out var address) ||
                address == 0)
            {
                Service.Logger.Error("Could not resolve cursor move scheduler.");
                return;
            }

            _scheduleCursorMove = (delegate* unmanaged<int, int, void>)address;
            Service.Logger.Information("Resolved cursor move scheduler at 0x{Address:X}.", address);
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not resolve cursor move scheduler.");
        }
    }

    public bool IsActive { get; private set; }

    public bool IsAvailable => _scheduleCursorMove != null;

    public void Apply(
        UIInputData* inputData,
        bool applyScheduledMoveCompensation,
        bool rememberRestorePosition)
    {
        if (applyScheduledMoveCompensation)
        {
            CompensateForScheduledCursorMove(inputData);
        }

        if (_scheduleCursorMove == null || !TryGetViewportCenter(out var centerX, out var centerY))
        {
            Reset();
            return;
        }

        var wasActive = IsActive;
        if (!wasActive)
        {
            if (rememberRestorePosition)
            {
                _restorePositionX = inputData->CursorInputs.PositionX;
                _restorePositionY = inputData->CursorInputs.PositionY;
                _hasRestorePosition = true;
            }
            else
            {
                ClearRestorePosition();
            }

            IsActive = true;
        }
        else if (!rememberRestorePosition)
        {
            ClearRestorePosition();
        }

        var currentX = inputData->CursorInputs.PositionX;
        var currentY = inputData->CursorInputs.PositionY;

        SetInputCursorPosition(inputData, centerX, centerY);
        if (!wasActive)
        {
            ClearInputCursorDelta(inputData);
        }

        if (currentX == centerX && currentY == centerY)
        {
            return;
        }

        _scheduleCursorMove(centerX, centerY);

        if (!_hasScheduledCursorMoveDelta)
        {
            _scheduledCursorMoveDeltaX = centerX - currentX;
            _scheduledCursorMoveDeltaY = centerY - currentY;
            _hasScheduledCursorMoveDelta = true;
            _clearScheduledCursorMoveDeltaOnCompensation = !wasActive;
        }
    }

    public void Release(bool restoreCursor)
    {
        if (!IsActive && !_hasScheduledCursorMoveDelta)
        {
            return;
        }

        if (restoreCursor && _hasRestorePosition && _scheduleCursorMove != null)
        {
            _scheduleCursorMove(_restorePositionX, _restorePositionY);
        }

        Reset();
    }

    public void Release(UIInputData* inputData, bool restoreCursor)
    {
        CompensateForScheduledCursorMove(inputData);
        Release(restoreCursor);
    }

    private void Reset()
    {
        IsActive = false;
        _hasRestorePosition = false;
        _hasScheduledCursorMoveDelta = false;
        _clearScheduledCursorMoveDeltaOnCompensation = false;
        _restorePositionX = 0;
        _restorePositionY = 0;
        _scheduledCursorMoveDeltaX = 0;
        _scheduledCursorMoveDeltaY = 0;
    }

    private void ClearRestorePosition()
    {
        _hasRestorePosition = false;
        _restorePositionX = 0;
        _restorePositionY = 0;
    }

    private void CompensateForScheduledCursorMove(UIInputData* inputData)
    {
        if (!_hasScheduledCursorMoveDelta)
        {
            return;
        }

        var originalDeltaX = inputData->CursorInputs.DeltaX;
        var originalDeltaY = inputData->CursorInputs.DeltaY;

        if (_clearScheduledCursorMoveDeltaOnCompensation)
        {
            ClearInputCursorDelta(inputData);
            ClearScheduledCursorMoveDelta();
            return;
        }

        ApplyScheduledCursorMoveDelta(&inputData->CursorInputs);
        if (inputData->UIFilteredCursorInputs.DeltaX == originalDeltaX &&
            inputData->UIFilteredCursorInputs.DeltaY == originalDeltaY)
        {
            ApplyScheduledCursorMoveDelta(&inputData->UIFilteredCursorInputs);
        }

        ClearScheduledCursorMoveDelta();
    }

    private void ClearScheduledCursorMoveDelta()
    {
        _hasScheduledCursorMoveDelta = false;
        _clearScheduledCursorMoveDeltaOnCompensation = false;
        _scheduledCursorMoveDeltaX = 0;
        _scheduledCursorMoveDeltaY = 0;
    }

    private void ApplyScheduledCursorMoveDelta(CursorInputData* cursorInputs)
    {
        cursorInputs->DeltaX -= _scheduledCursorMoveDeltaX;
        cursorInputs->DeltaY -= _scheduledCursorMoveDeltaY;
    }

    private static bool TryGetViewportCenter(out int centerX, out int centerY)
    {
        centerX = 0;
        centerY = 0;

        var stage = AtkStage.Instance();
        if (stage is null || stage->ScreenSize.Width <= 0 || stage->ScreenSize.Height <= 0)
        {
            return false;
        }

        centerX = stage->ScreenSize.Width / 2;
        centerY = stage->ScreenSize.Height / 2;
        return true;
    }

    private static void SetInputCursorPosition(UIInputData* inputData, int positionX, int positionY)
    {
        inputData->CursorInputs.PositionX = positionX;
        inputData->CursorInputs.PositionY = positionY;

        inputData->UIFilteredCursorInputs.PositionX = positionX;
        inputData->UIFilteredCursorInputs.PositionY = positionY;
    }

    private static void ClearInputCursorDelta(UIInputData* inputData)
    {
        inputData->CursorInputs.DeltaX = 0;
        inputData->CursorInputs.DeltaY = 0;

        inputData->UIFilteredCursorInputs.DeltaX = 0;
        inputData->UIFilteredCursorInputs.DeltaY = 0;
    }
}
