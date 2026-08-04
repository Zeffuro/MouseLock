using System;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;

namespace MouseLock.Game;

internal static class DalamudUiState
{
    private static readonly TimeSpan RecentFocusGracePeriod = TimeSpan.FromMilliseconds(250);

    private static DalamudWindowFocus _lastExternalFocus;

    public static string CurrentWindowSystemNamespace
        => WindowSystem.FocusedWindowSystemNamespace ?? string.Empty;

    public static DalamudWindowFocus LastExternalFocus
    {
        get
        {
            RefreshFocusSnapshot();
            return _lastExternalFocus;
        }
    }

    public static bool IsBlockingUiActive(MouseLookConditionSettings conditions)
    {
        RefreshFocusSnapshot();

        return IsBlockingWindowSystemFocusActive(conditions);
    }

    private static bool IsBlockingWindowSystemFocusActive(MouseLookConditionSettings conditions)
    {
        var focus = GetCurrentFocus();
        if (string.IsNullOrEmpty(focus.WindowSystemNamespace))
        {
            return false;
        }

        if (conditions.IsDalamudWindowSystemIgnored(focus.WindowSystemNamespace) ||
            conditions.IsDalamudWindowIgnored(focus.WindowName))
        {
            return false;
        }

        return WindowSystem.HasAnyWindowSystemFocus ||
               WindowSystem.TimeSinceLastAnyFocus <= RecentFocusGracePeriod;
    }

    private static void RefreshFocusSnapshot()
    {
        var focus = GetCurrentFocus();
        if (string.IsNullOrEmpty(focus.WindowSystemNamespace) ||
            IsMouseLockWindowSystem(focus.WindowSystemNamespace) ||
            IsMouseLockWindowName(focus.WindowName))
        {
            return;
        }

        _lastExternalFocus = focus;
    }

    private static DalamudWindowFocus GetCurrentFocus()
        => new(CurrentWindowSystemNamespace, GetFocusedImGuiWindowName());

    private static bool IsMouseLockWindowSystem(string windowSystemNamespace)
        => string.Equals(windowSystemNamespace, "MouseLock", StringComparison.Ordinal);

    private static bool IsMouseLockWindowName(string windowName)
        => windowName.StartsWith("MouseLock", StringComparison.Ordinal);

    private static unsafe string GetFocusedImGuiWindowName()
    {
        var context = ImGui.GetCurrentContext();
        if (context.IsNull)
        {
            return string.Empty;
        }

        var window = context.NavWindow;
        if (window.IsNull)
        {
            window = context.ActiveIdWindow;
        }

        return window.IsNull
            ? string.Empty
            : Marshal.PtrToStringUTF8((nint)window.Name) ?? string.Empty;
    }
}

internal readonly record struct DalamudWindowFocus(string WindowSystemNamespace, string WindowName)
{
    public bool IsEmpty => string.IsNullOrEmpty(WindowSystemNamespace) && string.IsNullOrEmpty(WindowName);
}
