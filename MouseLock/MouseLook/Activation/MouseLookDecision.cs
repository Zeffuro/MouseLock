namespace MouseLock.MouseLook.Activation;

internal readonly record struct MouseLookDecision(bool ShouldLock, MouseLookPauseReason Reason, string WindowName = "")
{
    public static MouseLookDecision Allow() => new(true, MouseLookPauseReason.None);

    public static MouseLookDecision Pause(MouseLookPauseReason reason, string windowName = "") => new(false, reason, windowName);
}
