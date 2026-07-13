namespace MouseLock.MouseLook.Activation;

internal readonly record struct MouseLookDecision(bool ShouldLock, MouseLookPauseReason Reason)
{
    public static MouseLookDecision Allow() => new(true, MouseLookPauseReason.None);

    public static MouseLookDecision Pause(MouseLookPauseReason reason) => new(false, reason);
}
