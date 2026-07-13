using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using MouseLock.Extensions;

namespace MouseLock.Hotbars;

internal readonly unsafe struct HotbarSlotHandle(
    RaptureHotbarModule* module,
    RaptureHotbarModule.HotbarSlot* slot,
    int hotbarNumber,
    int slotNumber)
{
    public int HotbarNumber { get; } = hotbarNumber;
    public int SlotNumber { get; } = slotNumber;
    public HotbarSlotAppearance Appearance { get; } = HotbarSlotAppearance.From(module, slot);
    public bool IsHqItem => Appearance.IsHqItem;
    public bool IsItemSlot => Appearance.IsItemSlot;
    public uint BaseActionId => Appearance.LookupActionId;

    public uint IconId
    {
        get
        {
            var resolvedIconId = slot->GetIconIdForSlot(Appearance.SlotType, Appearance.LookupActionId);
            if (resolvedIconId > 0)
            {
                return (uint)resolvedIconId;
            }

            return Appearance.IsItemSlot ? 0 : slot->IconId;
        }
    }

    public string PlainTextDisplayName
        => Appearance.IsItemSlot
            ? ItemUtil.GetItemName(Appearance.ActionId, false).ToString()
            : slot->GetDisplayNameForSlot(Appearance.SlotType, Appearance.LookupActionId).ToPlainText();

    public void Execute()
    {
        module->ExecuteSlot(slot);
    }
}
