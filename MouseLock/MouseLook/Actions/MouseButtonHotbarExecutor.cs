using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MouseLock.MouseLook.Actions;

internal static unsafe class MouseButtonHotbarExecutor
{
    public static void Execute(int hotbar, int slot)
    {
        if (!TryGetSlot(hotbar, slot, out var module, out var hotbarSlot))
        {
            return;
        }

        module->ExecuteSlot(hotbarSlot);
    }

    public static bool TryGetIconId(int hotbar, int slot, out uint iconId)
    {
        iconId = 0;
        if (!TryGetSlot(hotbar, slot, out _, out var hotbarSlot))
        {
            return false;
        }

        iconId = hotbarSlot->IconId;
        if (iconId == 0)
        {
            var resolvedIconId = hotbarSlot->GetIconIdForSlot(hotbarSlot->ApparentSlotType, hotbarSlot->ApparentActionId);
            if (resolvedIconId > 0)
            {
                iconId = (uint)resolvedIconId;
            }
        }

        return iconId != 0;
    }

    public static bool TryGetDisplayName(int hotbar, int slot, out string displayName)
    {
        displayName = string.Empty;
        if (!TryGetSlot(hotbar, slot, out _, out var hotbarSlot))
        {
            return false;
        }

        displayName = hotbarSlot
            ->GetDisplayNameForSlot(hotbarSlot->ApparentSlotType, hotbarSlot->ApparentActionId)
            .ToString();

        return !string.IsNullOrWhiteSpace(displayName);
    }

    private static bool TryGetSlot(
        int hotbar,
        int slot,
        out RaptureHotbarModule* module,
        out RaptureHotbarModule.HotbarSlot* hotbarSlot)
    {
        module = RaptureHotbarModule.Instance();
        hotbarSlot = null;

        if (module is null || !module->ModuleReady)
        {
            return false;
        }

        hotbar = int.Clamp(hotbar, 1, 10);
        slot = int.Clamp(slot, 1, 12);

        hotbarSlot = module->StandardHotbars[hotbar - 1].GetHotbarSlot((uint)(slot - 1));
        return hotbarSlot is not null && !hotbarSlot->IsEmpty;
    }
}
