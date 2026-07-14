using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;

namespace MouseLock.Windows.Tabs;

internal sealed class CompatibilityTab(SystemConfiguration config, Action save)
{
    public void Draw()
    {
        using var tab = ImRaii.TabItem("Compatibility");
        if (!tab)
        {
            return;
        }

        var hideCursorOverlayPlugins = config.Compatibility.HideCursorOverlayPluginsDuringMouseLook;
        if (ImGui.Checkbox("Hide cursor overlay plugins during mouselook", ref hideCursorOverlayPlugins))
        {
            config.Compatibility.HideCursorOverlayPluginsDuringMouseLook = hideCursorOverlayPlugins;
            save();
        }

        var disableDuringTPieRing = config.Compatibility.DisableDuringTPieRing;
        if (ImGui.Checkbox("Pause while using TPie", ref disableDuringTPieRing))
        {
            config.Compatibility.DisableDuringTPieRing = disableDuringTPieRing;
            save();
        }
    }
}
