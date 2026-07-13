using MouseLock.MouseLook.Activation;

namespace MouseLock.MouseLook;

internal readonly record struct MouseLookStatus(MouseLookStatusKind Kind, MouseLookPauseReason Reason)
{
    public static MouseLookStatus Off() => new(MouseLookStatusKind.Off, MouseLookPauseReason.PluginDisabled);

    public static MouseLookStatus Ready() => new(MouseLookStatusKind.Ready, MouseLookPauseReason.None);

    public static MouseLookStatus Active() => new(MouseLookStatusKind.Active, MouseLookPauseReason.None);

    public static MouseLookStatus Paused(MouseLookPauseReason reason) => new(MouseLookStatusKind.Paused, reason);

    public static MouseLookStatus Unavailable(MouseLookPauseReason reason) => new(MouseLookStatusKind.Unavailable, reason);
}
