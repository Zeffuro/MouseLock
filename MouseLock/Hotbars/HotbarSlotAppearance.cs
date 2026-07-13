using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace MouseLock.Hotbars;

internal readonly unsafe record struct HotbarSlotAppearance(
    RaptureHotbarModule.HotbarSlotType SlotType,
    uint ActionId)
{
    public bool IsItemSlot => SlotType.IsItemSlot();
    public bool IsHqItem => IsItemSlot && ItemUtil.IsHighQuality(ActionId);
    public uint LookupActionId => IsItemSlot ? ItemUtil.GetBaseId(ActionId).ItemId : ActionId;

    public static HotbarSlotAppearance From(
        RaptureHotbarModule* module,
        RaptureHotbarModule.HotbarSlot* slot)
    {
        var slotType = slot->ApparentSlotType;
        var actionId = slot->ApparentActionId;
        ushort ignoredC4 = 0;

        RaptureHotbarModule.GetSlotAppearance(&slotType, &actionId, &ignoredC4, module, slot);
        return new HotbarSlotAppearance(slotType, actionId);
    }
}
