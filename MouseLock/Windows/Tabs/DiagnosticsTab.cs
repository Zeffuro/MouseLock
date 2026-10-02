using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Game;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows.Tabs;

internal sealed class DiagnosticsTab
{
    private string _message = string.Empty;

    public unsafe void Draw()
    {
        using var tab = ImRaii.TabItem("Diagnostics");
        if (!tab) return;

        var service = PluginState.MouseLookService;
        var status = service?.Status ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
        ImGui.TextUnformatted($"Status: {MouseLookStatusFormatter.GetSummary(status)}");
        var lastDecision = service?.LastDecision;
        if (!string.IsNullOrEmpty(lastDecision?.WindowName))
        {
            ImGui.TextWrapped($"Window: {lastDecision.Value.WindowName}");
        }

        ConfigWindow.DrawSection("Input trace");
        ImGui.TextDisabled("Record, close config, and move the mouse for 10 seconds.");
        ImGui.TextUnformatted(service?.InputTrace.Summary ?? "Unavailable");
        if (ImGui.Button("Record 10s"))
        {
            _message = service?.InputTrace.Start("config") ?? "Unavailable";
        }
        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
            _message = service?.InputTrace.Stop() ?? "Unavailable";
        }
        ImGui.SameLine();
        if (ImGui.Button("Save"))
        {
            _message = service?.InputTrace.Save() ?? "Unavailable";
        }

        if (ImGui.CollapsingHeader("Input details"))
        {
            ImGui.TextUnformatted($"Input hook: {FormatReady(service?.IsAtkModuleHandleInputHookReady == true)}");
            ImGui.TextUnformatted($"Camera hook: {FormatReady(service?.IsCameraInputSourceHookReady == true)}");
            ImGui.TextUnformatted($"Mouse drag: {FormatReady(service?.IsMouseDragAvailable == true)}");
            ImGui.TextUnformatted($"Cursor recenter: {FormatReady(service?.IsCursorRecenterAvailable == true)}");
            var focusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName)
                ? focusedAddonName : "None";
            var inputData = UIInputData.Instance();
            var hoveredAddon = inputData is not null &&
                               NativeUiState.TryGetHoveredBlockingAddonName(inputData, out var hoveredAddonName)
                ? ConfigWindow.DisplayAddonName(hoveredAddonName) : "None";
            ImGui.TextUnformatted($"Focused addon: {focusedAddon}");
            ImGui.TextWrapped($"Last addon: {ConfigWindow.DisplayAddonName(NativeUiState.LastFocusedAddonName)}");
            ImGui.TextUnformatted($"Hovered addon: {hoveredAddon}");
            ImGui.TextWrapped($"Suspended by: {(SuspensionRegistry.IsSuspended ? SuspensionRegistry.SourcesSummary : "None")}");
        }

        if (ImGui.CollapsingHeader("Recent pauses"))
        {
            if (service is null || service.PauseHistory.Entries.Count == 0)
            {
                ImGui.TextDisabled("None");
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
        }

        ImGui.Spacing();
        if (ImGui.Button("Release cursor"))
        {
            service?.ForceReleaseCursor();
            _message = "Cursor released.";
        }
        if (!string.IsNullOrEmpty(_message)) ImGui.TextWrapped(_message);
    }

    private static string FormatReady(bool ready) => ready ? "Ready" : "Unavailable";
}
