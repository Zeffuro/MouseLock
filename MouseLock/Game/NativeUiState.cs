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
        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        return unitManager->FocusedAddon is not null && IsBlockingAddon(unitManager->FocusedAddon);
    }

    public static bool IsBlockingAddonHovered(UIInputData* inputData)
    {
        if (inputData is null)
        {
            return false;
        }

        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return false;
        }

        if (stage->AtkInputManager is not null && stage->AtkInputManager->FocusedNode is not null)
        {
            return true;
        }

        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        var collision = new AddonCollision();
        unitManager->GetAddonCollision(
            &collision,
            (short)inputData->CursorInputs.PositionX,
            (short)inputData->CursorInputs.PositionY);

        return collision.UnitBase is not null && IsBlockingAddon(collision.UnitBase);
    }

    private static bool IsBlockingAddon(AtkUnitBase* addon)
        => addon is not null && addon->IsVisible && addon->IsReady && !addon->ShouldIgnoreInputs();
}
