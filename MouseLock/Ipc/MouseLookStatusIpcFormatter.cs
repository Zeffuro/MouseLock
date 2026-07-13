using MouseLock.MouseLook;

namespace MouseLock.Ipc;

internal static class MouseLookStatusIpcFormatter
{
    public static string GetStatus(MouseLookStatus status)
        => $"{status.Kind}|{status.Reason}|{GetPauseReason(status)}";

    public static string GetPauseReason(MouseLookStatus status)
        => MouseLookStatusFormatter.GetReasonLabel(status.Reason);
}
