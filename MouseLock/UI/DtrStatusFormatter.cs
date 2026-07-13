using MouseLock.Configuration;
using MouseLock.MouseLook;

namespace MouseLock.UI;

internal static class DtrStatusFormatter
{
    public static string GetText(MouseLookStatus status, DtrTextMode textMode)
        => textMode switch
        {
            DtrTextMode.Detailed => GetDetailedText(status),
            DtrTextMode.Compact => GetCompactText(status),
            _ => GetSimpleText(status),
        };

    private static string GetDetailedText(MouseLookStatus status)
        => status.Kind switch
        {
            MouseLookStatusKind.Off => "MouseLock: Off",
            MouseLookStatusKind.Ready => "MouseLock: On",
            MouseLookStatusKind.Active => "MouseLock: Active",
            MouseLookStatusKind.Paused => $"MouseLock: Paused: {MouseLookStatusFormatter.GetReasonLabel(status.Reason)}",
            MouseLookStatusKind.Unavailable => "MouseLock: Unavailable",
            _ => "MouseLock",
        };

    private static string GetSimpleText(MouseLookStatus status)
        => status.Kind switch
        {
            MouseLookStatusKind.Off => "MouseLock: Off",
            MouseLookStatusKind.Ready => "MouseLock: On",
            MouseLookStatusKind.Active => "MouseLock: Active",
            MouseLookStatusKind.Paused => "MouseLock: Paused",
            MouseLookStatusKind.Unavailable => "MouseLock: Unavailable",
            _ => "MouseLock",
        };

    private static string GetCompactText(MouseLookStatus status)
        => status.Kind switch
        {
            MouseLookStatusKind.Off => "ML: Off",
            MouseLookStatusKind.Ready => "ML: On",
            MouseLookStatusKind.Active => "ML: Active",
            MouseLookStatusKind.Paused => "ML: Paused",
            MouseLookStatusKind.Unavailable => "ML: Unavailable",
            _ => "ML",
        };
}
