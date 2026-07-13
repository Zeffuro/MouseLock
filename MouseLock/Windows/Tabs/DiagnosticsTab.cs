using Dalamud.Interface.Utility.Raii;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class DiagnosticsTab(DiagnosticsPanel diagnosticsPanel)
{
    public void Draw()
    {
        using var tab = ImRaii.TabItem("Diagnostics");
        if (!tab)
        {
            return;
        }

        diagnosticsPanel.Draw();
    }
}
