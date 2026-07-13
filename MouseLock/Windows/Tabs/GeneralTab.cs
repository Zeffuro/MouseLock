using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.MouseLook;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class GeneralTab(
    SystemConfiguration config,
    Action save,
    ToggleKeybindEditor toggleKeybindEditor,
    DtrSettingsEditor dtrSettingsEditor,
    ConfigurationTransferPanel configurationTransferPanel)
{
    private static readonly ComboOption<ReleaseModifierKey>[] ReleaseModifierOptions =
    [
        new(ReleaseModifierKey.Alt, "Alt"),
        new(ReleaseModifierKey.Control, "Control"),
        new(ReleaseModifierKey.Shift, "Shift"),
        new(ReleaseModifierKey.None, "None"),
    ];

    private static readonly string[] ReleaseModifierLabels = CreateLabels(ReleaseModifierOptions);

    public void Draw()
    {
        using var tab = ImRaii.TabItem("General");
        if (!tab)
        {
            return;
        }

        var enabled = config.General.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            MouseLockStateController.SetEnabled(enabled);
        }

        var releaseModifierIndex = FindOptionIndex(ReleaseModifierOptions, config.General.ReleaseModifier);
        if (ImGui.Combo("Temporary release modifier", ref releaseModifierIndex, ReleaseModifierLabels, ReleaseModifierLabels.Length))
        {
            config.General.ReleaseModifier = ReleaseModifierOptions[releaseModifierIndex].Value;
            save();
        }

        var restoreCursorPositionOnRelease = config.General.RestoreCursorPositionOnRelease;
        if (ImGui.Checkbox("Return cursor to previous position when released", ref restoreCursorPositionOnRelease))
        {
            config.General.RestoreCursorPositionOnRelease = restoreCursorPositionOnRelease;
            save();
        }
        ConfigWindow.DrawTooltip("Disable this to leave the cursor centered when MouseLock pauses or releases.");

        var stickyReleaseEnabled = config.General.StickyReleaseEnabled;
        if (ImGui.Checkbox("Tap release modifier to keep cursor released", ref stickyReleaseEnabled))
        {
            config.General.StickyReleaseEnabled = stickyReleaseEnabled;
            save();
        }
        ConfigWindow.DrawTooltip("When enabled, tap the release modifier to keep the cursor free. Tap it again or click back into the world to relock.");

        ConfigWindow.DrawSection("Toggle keybind");
        toggleKeybindEditor.Draw(config.General.ToggleKeybind);

        ConfigWindow.DrawSection("Server Info Bar");
        dtrSettingsEditor.Draw(config.Dtr);

        ConfigWindow.DrawSection("Configuration");
        configurationTransferPanel.Draw();
    }

    private readonly record struct ComboOption<T>(T Value, string Label);

    private static string[] CreateLabels<T>(IReadOnlyList<ComboOption<T>> options)
    {
        var labels = new string[options.Count];
        for (var index = 0; index < options.Count; index++)
        {
            labels[index] = options[index].Label;
        }

        return labels;
    }

    private static int FindOptionIndex<T>(IReadOnlyList<ComboOption<T>> options, T value)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(options[index].Value, value))
            {
                return index;
            }
        }

        return 0;
    }
}
