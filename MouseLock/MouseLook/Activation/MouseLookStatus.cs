using MouseLock.Configuration;

namespace MouseLock.MouseLook.Activation;

internal readonly record struct MouseLookStatus(MouseLookStatusKind Kind, MouseLookPauseReason Reason)
{
    public static MouseLookStatus Off() => new(MouseLookStatusKind.Off, MouseLookPauseReason.PluginDisabled);

    public static MouseLookStatus Ready() => new(MouseLookStatusKind.Ready, MouseLookPauseReason.None);

    public static MouseLookStatus Active() => new(MouseLookStatusKind.Active, MouseLookPauseReason.None);

    public static MouseLookStatus Paused(MouseLookPauseReason reason) => new(MouseLookStatusKind.Paused, reason);

    public static MouseLookStatus Unavailable(MouseLookPauseReason reason) => new(MouseLookStatusKind.Unavailable, reason);

    public string DtrText => GetDtrText(DtrTextMode.Simple);

    public string GetDtrText(DtrTextMode textMode)
        => textMode switch
        {
            DtrTextMode.Detailed => DetailedDtrText,
            DtrTextMode.Compact => CompactDtrText,
            _ => SimpleDtrText,
        };

    private string DetailedDtrText => Kind switch
    {
        MouseLookStatusKind.Off => "MouseLock: Off",
        MouseLookStatusKind.Ready => "MouseLock: On",
        MouseLookStatusKind.Active => "MouseLock: Active",
        MouseLookStatusKind.Paused => $"MouseLock: Paused: {ReasonLabel}",
        MouseLookStatusKind.Unavailable => "MouseLock: Unavailable",
        _ => "MouseLock",
    };

    private string SimpleDtrText => Kind switch
    {
        MouseLookStatusKind.Off => "MouseLock: Off",
        MouseLookStatusKind.Ready => "MouseLock: On",
        MouseLookStatusKind.Active => "MouseLock: Active",
        MouseLookStatusKind.Paused => "MouseLock: Paused",
        MouseLookStatusKind.Unavailable => "MouseLock: Unavailable",
        _ => "MouseLock",
    };

    private string CompactDtrText => Kind switch
    {
        MouseLookStatusKind.Off => "ML: Off",
        MouseLookStatusKind.Ready => "ML: On",
        MouseLookStatusKind.Active => "ML: Active",
        MouseLookStatusKind.Paused => "ML: Paused",
        MouseLookStatusKind.Unavailable => "ML: Unavailable",
        _ => "ML",
    };

    public string Summary => Kind switch
    {
        MouseLookStatusKind.Off => "Off",
        MouseLookStatusKind.Ready => "On",
        MouseLookStatusKind.Active => "Active",
        MouseLookStatusKind.Paused => $"Paused: {ReasonLabel}",
        MouseLookStatusKind.Unavailable => $"Unavailable: {ReasonLabel}",
        _ => Kind.ToString(),
    };

    public string Detail => Kind switch
    {
        MouseLookStatusKind.Off => "MouseLock is disabled.",
        MouseLookStatusKind.Ready => "MouseLock is enabled and ready.",
        MouseLookStatusKind.Active => "MouseLock is currently controlling camera input.",
        MouseLookStatusKind.Paused => $"MouseLock is enabled but paused: {ReasonLabel}.",
        MouseLookStatusKind.Unavailable => $"MouseLock cannot run: {ReasonLabel}.",
        _ => Summary,
    };

    public string IpcStatus => $"{Kind}|{Reason}|{ReasonLabel}";

    public string ReasonLabel => Reason switch
    {
        MouseLookPauseReason.None => "None",
        MouseLookPauseReason.PluginDisabled => "Disabled",
        MouseLookPauseReason.LoggedOut => "Logged out",
        MouseLookPauseReason.ConfigWindowOpen => "Config open",
        MouseLookPauseReason.GameUnfocused => "Game unfocused",
        MouseLookPauseReason.CombatRequired => "Out of combat",
        MouseLookPauseReason.Cutscene => "Cutscene",
        MouseLookPauseReason.Gpose => "GPose",
        MouseLookPauseReason.Crafting => "Crafting",
        MouseLookPauseReason.Gathering => "Gathering",
        MouseLookPauseReason.GroundTargeting => "Ground targeting",
        MouseLookPauseReason.HousingPlacement => "Housing placement",
        MouseLookPauseReason.Mounted => "Mounted",
        MouseLookPauseReason.TerritoryTransition => "Territory transition",
        MouseLookPauseReason.GamepadMouseMode => "Gamepad mouse mode",
        MouseLookPauseReason.TextInput => "Text input",
        MouseLookPauseReason.TalkAddon => "NPC dialogue",
        MouseLookPauseReason.NativeAddonFocused => "Game window focused",
        MouseLookPauseReason.NativeAddonHovered => "Hovering game window",
        MouseLookPauseReason.TPie => "TPie",
        MouseLookPauseReason.ExternalSuspension => "External suspension",
        MouseLookPauseReason.ReleaseModifier => "Release modifier",
        MouseLookPauseReason.StickyRelease => "Sticky release",
        MouseLookPauseReason.MouseActionRelease => "Mouse action release",
        MouseLookPauseReason.ResumeDelay => "Resume delay",
        MouseLookPauseReason.WaitingForWorldClick => "Waiting for world click",
        MouseLookPauseReason.InputUnavailable => "Input unavailable",
        MouseLookPauseReason.HookUnavailable => "Hook unavailable",
        _ => Reason.ToString(),
    };
}
