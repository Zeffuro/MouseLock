using System;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.Configuration;
using MouseLock.Game;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows.Components;

internal sealed class DiagnosticsPanel(SystemConfiguration config, Action save)
{
    private string _message = string.Empty;

    public unsafe void Draw()
    {
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
        ImGui.TextUnformatted($"Hovered native addon: {hoveredAddon}");
        ImGui.TextUnformatted($"External suspensions: {DisplayExternalSuspensions()}");

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
