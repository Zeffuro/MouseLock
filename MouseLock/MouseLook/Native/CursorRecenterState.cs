using System;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MouseLock.MouseLook.Diagnostics;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class CursorRecenterState
{
    private bool _hasRestorePosition;
    private int _restorePositionX;
    private int _restorePositionY;
    private readonly MouseInputTrace _trace;

    public CursorRecenterState(MouseInputTrace trace)
    {
        _trace = trace;
        try
        {
            var address = (nint)MouseDevice.MemberFunctionPointers.ScheduleCursorMove;
            if (address == 0)
            {
                Service.Logger.Error("Could not resolve cursor move scheduler.");
                return;
            }

            IsAvailable = true;
            Service.Logger.Information("Resolved cursor move scheduler at 0x{Address:X}.", address);
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not resolve cursor move scheduler.");
        }
    }

    public bool IsActive { get; private set; }

    public bool IsAvailable { get; }

    public void Apply(
        UIInputData* inputData,
        bool rememberRestorePosition)
    {
        if (!IsAvailable || !TryGetViewportCenter(out var centerX, out var centerY))
        {
            Reset();
            return;
        }

        var wasActive = IsActive;
        if (!wasActive)
        {
            if (rememberRestorePosition &&
                IsInsideViewport(
                    inputData->CursorInputs.PositionX,
                    inputData->CursorInputs.PositionY))
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

        var traceMoveId = _trace.NextMoveId();
        _trace.Record("MoveScheduled", inputData, traceMoveId, centerX - currentX, centerY - currentY);
        MouseDevice.ScheduleCursorMove(centerX, centerY);
    }

    public void Release(bool restoreCursor)
    {
        if (!IsActive)
        {
            return;
        }

        if (restoreCursor && _hasRestorePosition && IsAvailable)
        {
            MouseDevice.ScheduleCursorMove(_restorePositionX, _restorePositionY);
        }

        Reset();
    }

    public void Release(UIInputData* inputData, bool restoreCursor)
    {
        _trace.Record("ReleaseBefore", inputData);
        if (restoreCursor && _hasRestorePosition && IsAvailable)
        {
            _trace.Record("RestoreScheduled", inputData, _trace.NextMoveId(),
                _restorePositionX - inputData->CursorInputs.PositionX,
                _restorePositionY - inputData->CursorInputs.PositionY);
        }
        Release(restoreCursor);
        _trace.Record("ReleaseAfter", inputData);
    }

    private void Reset()
    {
        IsActive = false;
        _hasRestorePosition = false;
        _restorePositionX = 0;
        _restorePositionY = 0;
    }

    private void ClearRestorePosition()
    {
        _hasRestorePosition = false;
        _restorePositionX = 0;
        _restorePositionY = 0;
    }

    private static bool IsInsideViewport(int positionX, int positionY)
    {
        var stage = AtkStage.Instance();
        return stage is not null &&
               positionX >= 0 &&
               positionY >= 0 &&
               positionX < stage->ScreenSize.Width &&
               positionY < stage->ScreenSize.Height;
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
