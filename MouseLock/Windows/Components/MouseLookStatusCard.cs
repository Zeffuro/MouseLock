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
        if (SuspensionRegistry.IsSuspended)
        {
            ImGui.TextDisabled($"External suspensions: {SuspensionRegistry.SourcesSummary}");
        }

        ImGui.Spacing();
    }
}
