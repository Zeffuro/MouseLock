using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Input.MouseActions;
using MouseLock.MouseLook.Native;

namespace MouseLock.MouseLook;

internal sealed unsafe class MouseLookController
{
    private readonly CursorOverlayState _cursorOverlayState = new();
    private readonly CursorVisibilityState _cursorVisibilityState = new();
    private readonly CursorRecenterState _cursorRecenterState = new();
    private readonly MouseDragState _mouseDragState = new();
    private readonly MouseButtonActionExecutor _mouseButtonActionExecutor = new();

    public bool IsActive => _mouseDragState.IsActive || _cursorVisibilityState.IsActive || _cursorRecenterState.IsActive;

    public bool ShouldRelease => IsActive || _cursorOverlayState.IsActive;

    public bool IsMouseDragAvailable => _mouseDragState.IsAvailable;

    public bool IsCursorRecenterAvailable => _cursorRecenterState.IsAvailable;

    public bool IsAvailable => IsMouseDragAvailable && IsCursorRecenterAvailable;

    public void UpdateMouseActions(UIInputData* inputData, bool allowNewActions)
        => _mouseButtonActionExecutor.Update(inputData, allowNewActions);

    public void Apply(
        UIInputData* inputData,
        bool applyScheduledMoveCompensation,
        bool applyCursorOverlayCompatibility,
        bool rememberCursorPosition,
        bool hideCursorOverlayPlugins)
    {
        _cursorOverlayState.Release(inputData);
        var physicalHeldButtons = inputData->CursorInputs.MouseButtonHeldFlags & MouseLookButtons.PhysicalLookButtons;

        _cursorRecenterState.Apply(inputData, applyScheduledMoveCompensation, rememberCursorPosition);
        MouseButtonSuppression.Apply(inputData);
        _mouseDragState.Apply(inputData);
        _cursorVisibilityState.Apply();

        if (applyCursorOverlayCompatibility)
        {
            ApplyCursorOverlayCompatibility(inputData, physicalHeldButtons, hideCursorOverlayPlugins);
        }
    }

    public void Release(UIInputData* inputData, bool restoreCursor)
    {
        _mouseButtonActionExecutor.ReleaseAll(inputData);
        _mouseDragState.Release(inputData);
        _cursorVisibilityState.Release();
        _cursorRecenterState.Release(inputData, restoreCursor);
        _cursorOverlayState.Release(inputData);
    }

    public void ReleaseWithoutInput(bool restoreCursor)
    {
        _mouseButtonActionExecutor.EmergencyReleaseAll();
        _mouseDragState.Release();
        _cursorVisibilityState.Release();
        _cursorRecenterState.Release(restoreCursor);
        _cursorOverlayState.Reset();
    }

    private void ApplyCursorOverlayCompatibility(
        UIInputData* inputData,
        MouseButtonFlags physicalHeldButtons,
        bool hideCursorOverlayPlugins)
    {
        if (!hideCursorOverlayPlugins)
        {
            _cursorOverlayState.Release(inputData);
            return;
        }

        _cursorOverlayState.Apply(inputData, physicalHeldButtons);
    }
}
