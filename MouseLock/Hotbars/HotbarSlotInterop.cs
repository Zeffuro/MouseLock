using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MouseLock.Hotbars;

internal static unsafe class HotbarSlotInterop
{
    public static bool TryGetSlot(int hotbar, int slot, out HotbarSlotHandle handle)
    {
        hotbar = int.Clamp(hotbar, 1, 10);
        slot = int.Clamp(slot, 1, 12);
        handle = default;

        var module = RaptureHotbarModule.Instance();
        if (module is null || !module->ModuleReady)
        {
            return false;
        }

        var hotbarSlot = module->GetSlotById((uint)(hotbar - 1), (uint)(slot - 1));
        if (hotbarSlot is null || hotbarSlot->IsEmpty)
        {
            return false;
        }

        handle = new HotbarSlotHandle(module, hotbarSlot, hotbar, slot);
        return true;
    }

    public static void Execute(int hotbar, int slot)
    {
        if (TryGetSlot(hotbar, slot, out var handle))
        {
            handle.Execute();
        }
    }

    public static bool TryPulseVisibleSlot(int hotbar, int slot)
    {
        return TryGetVisibleActionBar(hotbar, out var actionBar) &&
               actionBar.TryPulseSlot(slot);
    }

    public static HotbarPickerLayout GetPickerLayout(int hotbar)
    {
        return TryGetVisibleActionBar(hotbar, out var actionBar)
            ? actionBar.PickerLayout
            : HotbarPickerLayout.Default;
    }

    private static bool TryGetVisibleActionBar(int hotbar, out HotbarActionBarHandle handle)
    {
        hotbar = int.Clamp(hotbar, 1, 10);
        handle = default;

        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager is null)
        {
            return false;
        }

        var unitList = unitManager->AllLoadedUnitsList;
        var entries = unitList.Entries;
        var count = Math.Min(unitList.Count, entries.Length);
        var raptureHotbarId = hotbar - 1;

        for (var i = 0; i < count; i++)
        {
            var addon = entries[i].Value;
            if (addon is null ||
                !addon->IsVisible ||
                !addon->IsReady ||
                !addon->NameString.StartsWith("_ActionBar", StringComparison.Ordinal))
            {
                continue;
            }

            var candidate = (AddonActionBarBase*)addon;
            if (candidate->RaptureHotbarId != raptureHotbarId)
            {
                continue;
            }

            handle = new HotbarActionBarHandle(candidate);
            return true;
        }

        return false;
    }
}
