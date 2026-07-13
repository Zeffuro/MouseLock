using Dalamud.Bindings.ImGui;
using MouseLock.Compatibility;
using MouseLock.Game;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private string _diagnosticsMessage = string.Empty;

    private unsafe void DrawDiagnosticsSettings()
    {
        var debugEnabled = _config.General.DebugEnabled;
        if (ImGui.Checkbox("Debug enabled", ref debugEnabled))
        {
            _config.General.DebugEnabled = debugEnabled;
            Save();
        }

        var service = PluginState.MouseLookService;
        var status = service?.Status ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
        var lastDecision = service?.LastDecision ?? MouseLookDecision.Pause(MouseLookPauseReason.HookUnavailable);

        ImGui.TextUnformatted($"Status: {status.Summary}");
        ImGui.TextUnformatted($"Last decision: {(lastDecision.ShouldLock ? "Allow" : "Pause")} ({lastDecision.Reason})");
        ImGui.TextUnformatted($"Atk input hook: {FormatReady(service?.IsAtkModuleHandleInputHookReady == true)}");
        ImGui.TextUnformatted($"Camera input hook: {FormatReady(service?.IsCameraInputSourceHookReady == true)}");

        var focusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName)
            ? focusedAddonName
            : "None";
        var mousePosition = ImGui.GetIO().MousePos;
        var hoveredAddon = NativeUiState.TryGetHoveredBlockingAddonName(
            (short)mousePosition.X,
            (short)mousePosition.Y,
            out var hoveredAddonName)
            ? DisplayAddonName(hoveredAddonName)
            : "None";

        ImGui.TextUnformatted($"Focused native addon: {focusedAddon}");
        ImGui.TextUnformatted($"Hovered native addon: {hoveredAddon}");
        ImGui.TextUnformatted($"External suspensions: {DisplayExternalSuspensions()}");

        ImGui.Spacing();
        if (ImGui.Button("Force release cursor"))
        {
            service?.ForceReleaseCursor();
            _diagnosticsMessage = "Cursor release requested.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Retry hooks"))
        {
            service?.RetryHooks();
            _diagnosticsMessage = "Hook retry requested.";
        }

        if (!string.IsNullOrEmpty(_diagnosticsMessage))
        {
            ImGui.TextDisabled(_diagnosticsMessage);
        }
    }

    private static string FormatReady(bool ready)
        => ready ? "Ready" : "Unavailable";

    private static string DisplayExternalSuspensions()
        => ExternalSuspensionState.IsSuspended ? ExternalSuspensionState.SourcesSummary : "None";
}
