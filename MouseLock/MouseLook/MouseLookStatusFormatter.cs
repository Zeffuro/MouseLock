using MouseLock.MouseLook.Activation;

namespace MouseLock.MouseLook;

internal static class MouseLookStatusFormatter
{
    public static string GetSummary(MouseLookStatus status)
        => status.Kind switch
        {
            MouseLookStatusKind.Off => "Off",
            MouseLookStatusKind.Ready => "On",
            MouseLookStatusKind.Active => "Active",
            MouseLookStatusKind.Paused => $"Paused: {GetReasonLabel(status.Reason)}",
            MouseLookStatusKind.Unavailable => $"Unavailable: {GetReasonLabel(status.Reason)}",
            _ => status.Kind.ToString(),
        };

    public static string GetDetail(MouseLookStatus status)
        => status.Kind switch
        {
            MouseLookStatusKind.Off => "MouseLock is disabled.",
            MouseLookStatusKind.Ready => "MouseLock is enabled and ready.",
            MouseLookStatusKind.Active => "MouseLock is currently controlling camera input.",
            MouseLookStatusKind.Paused => $"MouseLock is enabled but paused: {GetReasonLabel(status.Reason)}.",
            MouseLookStatusKind.Unavailable => $"MouseLock cannot run: {GetReasonLabel(status.Reason)}.",
            _ => GetSummary(status),
        };

    public static string GetReasonLabel(MouseLookPauseReason reason)
        => reason switch
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
            MouseLookPauseReason.NativeSubsystemUnavailable => "Native input unavailable",
            _ => reason.ToString(),
        };
}
