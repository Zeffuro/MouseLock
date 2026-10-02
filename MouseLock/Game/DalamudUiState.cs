using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;

namespace MouseLock.Game;

internal static class DalamudUiState
{
    private static readonly TimeSpan RecentFocusGracePeriod = TimeSpan.FromMilliseconds(250);

    private static DalamudWindowFocus _lastExternalFocus;
    private static readonly RecentWindowHistory<DalamudWindowFocus> FocusHistory = new();

    public static IReadOnlyList<DalamudWindowFocus> RecentExternalFocus => FocusHistory.Entries;

    public static DalamudWindowFocus UpdateFocusSnapshot()
    {
        var focus = GetCurrentFocus();
        RefreshFocusSnapshot(focus);
        return focus;
    }

    public static void ClearFocusSnapshot()
    {
        _lastExternalFocus = default;
        FocusHistory.Clear();
    }

    private static string CurrentWindowSystemNamespace
        => WindowSystem.FocusedWindowSystemNamespace;

    public static DalamudWindowFocus LastExternalFocus => _lastExternalFocus;

    public static bool IsBlockingUiActive(MouseLookConditionSettings conditions)
        => TryGetBlockingFocus(conditions, out _);

    public static bool TryGetBlockingFocus(MouseLookConditionSettings conditions, out DalamudWindowFocus focus)
    {
        focus = UpdateFocusSnapshot();
        return IsBlockingFocus(conditions, focus);
    }

    public static bool IsBlockingFocus(MouseLookConditionSettings conditions, DalamudWindowFocus focus)
    {
        if (!conditions.DisableWhileConfigOpen && IsMouseLockWindowSystem(focus.WindowSystemNamespace))
        {
            return false;
        }

        return IsBlockingWindowSystemFocusActive(focus, conditions);
    }

    private static bool IsBlockingWindowSystemFocusActive(
        DalamudWindowFocus focus,
        MouseLookConditionSettings conditions)
    {
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

    private static void RefreshFocusSnapshot(DalamudWindowFocus focus)
    {
        if (string.IsNullOrEmpty(focus.WindowSystemNamespace) ||
            IsMouseLockWindowSystem(focus.WindowSystemNamespace) ||
            IsMouseLockWindowName(focus.WindowName))
        {
            return;
        }

        _lastExternalFocus = focus;
        FocusHistory.Record(focus);
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
