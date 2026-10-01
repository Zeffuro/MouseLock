using Dalamud.Bindings.ImGui;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows.Components;

internal sealed class MouseLookStatusCard
{
    public void Draw()
    {
        var status = PluginState.MouseLookService?.Status
                     ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);

        ImGui.TextUnformatted($"Status: {MouseLookStatusFormatter.GetSummary(status)}");
        ImGui.TextDisabled(MouseLookStatusFormatter.GetDetail(status));
        if (PluginState.MouseLookService?.PauseHistory.LastPauseBeforeConfig is { } previousPause)
        {
            ImGui.TextWrapped($"Last pause before config ({previousPause.Timestamp.ToLocalTime():HH:mm:ss}): {previousPause.Description}");
        }
        if (SuspensionRegistry.IsSuspended)
        {
            ImGui.TextDisabled($"External suspensions: {SuspensionRegistry.SourcesSummary}");
        }

        ImGui.Spacing();
    }
}
