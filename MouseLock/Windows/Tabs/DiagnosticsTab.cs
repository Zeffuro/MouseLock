using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Game;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows.Tabs;

internal sealed class DiagnosticsTab(SystemConfiguration config, Action save)
{
    private string _message = string.Empty;

    public unsafe void Draw()
    {
        using var tab = ImRaii.TabItem("Diagnostics");
        if (!tab)
        {
            return;
        }

        var debugEnabled = config.General.DebugEnabled;
        if (ImGui.Checkbox("Debug enabled", ref debugEnabled))
        {
            config.General.DebugEnabled = debugEnabled;
            save();
        }

        var service = PluginState.MouseLookService;
        var status = service?.Status ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
        var lastDecision = service?.LastDecision ?? MouseLookDecision.Pause(MouseLookPauseReason.HookUnavailable);

        ImGui.TextUnformatted($"Status: {MouseLookStatusFormatter.GetSummary(status)}");
        ImGui.TextUnformatted($"Last decision: {(lastDecision.ShouldLock ? "Allow" : "Pause")} ({lastDecision.Reason})");
        if (!string.IsNullOrEmpty(lastDecision.WindowName))
        {
            ImGui.TextWrapped($"Responsible window: {lastDecision.WindowName}");
        }
        ImGui.TextUnformatted($"Overall availability: {FormatReady(service?.IsMouseLookAvailable == true)}");
        ImGui.TextUnformatted($"Atk input detour: {FormatReady(service?.IsAtkModuleHandleInputHookReady == true)}");
        ImGui.TextUnformatted($"Camera input detour: {FormatReady(service?.IsCameraInputSourceHookReady == true)}");
        ImGui.TextUnformatted($"Native mouse drag: {FormatReady(service?.IsMouseDragAvailable == true)}");
        ImGui.TextUnformatted($"Cursor recenter scheduler: {FormatReady(service?.IsCursorRecenterAvailable == true)}");

        var focusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName)
            ? focusedAddonName
            : "None";
        var inputData = UIInputData.Instance();
        var hoveredAddon = inputData is not null &&
                           NativeUiState.TryGetHoveredBlockingAddonName(inputData, out var hoveredAddonName)
            ? ConfigWindow.DisplayAddonName(hoveredAddonName)
            : inputData is null
                ? "Input unavailable"
                : "None";

        ImGui.TextUnformatted($"Focused native addon: {focusedAddon}");
        ImGui.TextWrapped($"Last focused native addon: {ConfigWindow.DisplayAddonName(NativeUiState.LastFocusedAddonName)}");
        ImGui.TextUnformatted($"Hovered native addon: {hoveredAddon}");
        ImGui.TextUnformatted($"External suspensions: {DisplayExternalSuspensions()}");

        ConfigWindow.DrawSection("Recent pauses (newest first)");
        ImGui.TextWrapped("Keeps the last eight pause transitions, including window names. Opening config preserves the earlier entries.");
        if (service is null || service.PauseHistory.Entries.Count == 0)
        {
            ImGui.TextDisabled("No pauses recorded yet.");
        }
        else
        {
            var entries = service.PauseHistory.Entries;
            for (var index = entries.Count - 1; index >= 0; index--)
            {
                var entry = entries[index];
                ImGui.TextWrapped($"{entry.Timestamp.ToLocalTime():HH:mm:ss} — {entry.Description}");
            }
        }

        ImGui.Spacing();
        if (ImGui.Button("Force release cursor"))
        {
            service?.ForceReleaseCursor();
            _message = "Cursor release requested.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Retry hooks"))
        {
            service?.RetryHooks();
            _message = "Hook retry requested.";
        }

        if (!string.IsNullOrEmpty(_message))
        {
            ImGui.TextDisabled(_message);
        }
    }

    private static string FormatReady(bool ready)
        => ready ? "Ready" : "Unavailable";

    private static string DisplayExternalSuspensions()
        => SuspensionRegistry.IsSuspended ? SuspensionRegistry.SourcesSummary : "None";
}
