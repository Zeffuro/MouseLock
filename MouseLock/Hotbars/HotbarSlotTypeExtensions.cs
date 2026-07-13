using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MouseLock.Hotbars;

internal static class HotbarSlotTypeExtensions
{
    public static bool IsItemSlot(this RaptureHotbarModule.HotbarSlotType slotType)
        => slotType is RaptureHotbarModule.HotbarSlotType.Item
            or RaptureHotbarModule.HotbarSlotType.InventoryItem;
}
