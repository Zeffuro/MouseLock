using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.Game;

internal static unsafe class NativeUiState
{
    public static bool IsAddonVisible(string addonName)
    {
        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        var addon = unitManager->GetAddonByName(addonName);
        return addon is not null && addon->IsVisible && addon->IsReady;
    }

    public static bool IsBlockingAddonFocused()
    {
        return TryGetFocusedBlockingAddonName(out _);
    }

    public static bool IsBlockingAddonHovered(UIInputData* inputData)
        => TryGetHoveredBlockingAddonName(inputData, out _);

    public static bool TryGetFocusedBlockingAddonName(out string addonName)
    {
        addonName = string.Empty;

        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        return TryGetBlockingAddonName(unitManager->FocusedAddon, out addonName);
    }

    public static bool TryGetHoveredBlockingAddonName(UIInputData* inputData, out string addonName)
    {
        addonName = string.Empty;

        if (inputData is null)
        {
            return false;
        }

        return TryGetHoveredBlockingAddonName(
            (short)inputData->CursorInputs.PositionX,
            (short)inputData->CursorInputs.PositionY,
            out addonName);
    }

    public static bool TryGetHoveredBlockingAddonName(short cursorX, short cursorY, out string addonName)
    {
        addonName = string.Empty;

        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        var collision = new AddonCollision();
        unitManager->GetAddonCollision(&collision, cursorX, cursorY);
        if (TryGetBlockingAddonName(collision.UnitBase, out addonName))
        {
            return true;
        }

        var stage = AtkStage.Instance();
        if (stage is null || stage->AtkInputManager is null || stage->AtkInputManager->FocusedNode is null)
        {
            return false;
        }

        _ = TryGetFocusedBlockingAddonName(out addonName);
        return true;
    }

    private static bool IsBlockingAddon(AtkUnitBase* addon)
        => addon is not null && addon->IsVisible && addon->IsReady && !addon->ShouldIgnoreInputs();

    private static bool TryGetBlockingAddonName(AtkUnitBase* addon, out string addonName)
    {
        addonName = string.Empty;

        if (!IsBlockingAddon(addon))
        {
            return false;
        }

        addonName = addon->NameString;
        return true;
    }
}
