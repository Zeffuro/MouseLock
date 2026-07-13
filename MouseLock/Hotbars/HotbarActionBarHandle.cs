using FFXIVClientStructs.FFXIV.Client.UI;

namespace MouseLock.Hotbars;

internal readonly unsafe struct HotbarActionBarHandle(AddonActionBarBase* actionBar)
{
    public HotbarPickerLayout PickerLayout => ToPickerLayout(((AddonActionBarX*)actionBar)->ActionBarLayout);

    public bool TryPulseSlot(int slot)
    {
        slot = int.Clamp(slot, 1, 12);
        if (actionBar->SlotCount < slot)
        {
            return false;
        }

        actionBar->PulseActionBarSlot(slot - 1);
        return true;
    }

    private static HotbarPickerLayout ToPickerLayout(ActionBarLayout layout)
        => layout switch
        {
            ActionBarLayout.Layout12X1 => new HotbarPickerLayout(12, 1),
            ActionBarLayout.Layout6X2 => new HotbarPickerLayout(6, 2),
            ActionBarLayout.Layout4X3 => new HotbarPickerLayout(4, 3),
            ActionBarLayout.Layout3X4 => new HotbarPickerLayout(3, 4),
            ActionBarLayout.Layout2X6 => new HotbarPickerLayout(2, 6),
            ActionBarLayout.Layout1X12 => new HotbarPickerLayout(1, 12),
            _ => HotbarPickerLayout.Default,
        };
}
