namespace MouseLock.Configuration;

public sealed class MouseLookConditionSettings
{
    public bool DisableWhileTextInputActive { get; set; } = true;
    public bool DisableWhenTalkAddonVisible { get; set; } = true;
    public bool DisableWhenNativeAddonFocused { get; set; } = true;
    public bool DisableWhenNativeAddonHovered { get; set; }
    public bool RequireCombat { get; set; }
    public bool CountCountdownAsCombat { get; set; } = true;
}
