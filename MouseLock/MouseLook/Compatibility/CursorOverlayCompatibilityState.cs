using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MouseLock.MouseLook.Compatibility;

internal sealed class CursorOverlayCompatibilityState
{
    private const MouseButtonFlags OverlayHiddenButton = MouseLookButtons.VirtualLookButton;

    private MouseButtonFlags _lastPhysicalHeldButtons;

    public bool IsActive { get; private set; }

    public unsafe void Apply(UIInputData* inputData, MouseButtonFlags physicalHeldButtons)
    {
        _lastPhysicalHeldButtons = physicalHeldButtons & MouseLookButtons.PhysicalLookButtons;
        IsActive = true;

        inputData->CursorInputs.MouseButtonHeldFlags |= OverlayHiddenButton;
        inputData->CursorInputs.MouseButtonHeldThrottledFlags |= OverlayHiddenButton;
    }

    public unsafe void Release(UIInputData* inputData)
    {
        if (!IsActive)
        {
            return;
        }

        if ((_lastPhysicalHeldButtons & OverlayHiddenButton) == 0)
        {
            inputData->CursorInputs.MouseButtonHeldFlags &= ~OverlayHiddenButton;
            inputData->CursorInputs.MouseButtonHeldThrottledFlags &= ~OverlayHiddenButton;
        }

        Reset();
    }

    public void Reset()
    {
        IsActive = false;
        _lastPhysicalHeldButtons = MouseButtonFlags.None;
    }
}
