using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class WindowExceptionsTab(
    SystemConfiguration config,
    NativeAddonExceptionEditor nativeAddonExceptionEditor,
    DalamudWindowExceptionEditor dalamudWindowExceptionEditor)
{
    public void Draw()
    {
        using var tab = ImRaii.TabItem("Windows");
        if (!tab) return;

        dalamudWindowExceptionEditor.Draw(config.Activation.Conditions);
        nativeAddonExceptionEditor.Draw(config.Activation.Conditions);
        DrawPauseHistory();
    }

    private static void DrawPauseHistory()
    {
        var history = PluginState.MouseLookService?.PauseHistory;
        var open = ImGui.CollapsingHeader("Pause history");
        ConfigWindow.DrawTooltip("Last 8 pauses, newest first.");
        if (!open) return;
        if (history is null || history.Entries.Count == 0)
        {
            ImGui.TextDisabled("No pauses recorded yet.");
            return;
        }

        for (var index = history.Entries.Count - 1; index >= 0; index--)
        {
            var entry = history.Entries[index];
            ImGui.TextWrapped($"{entry.Timestamp.ToLocalTime():HH:mm:ss} — {entry.Description}");
        }
    }
}
