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

    private static readonly ComboOption<ReleaseModifierTapBehavior>[] ReleaseModifierTapBehaviorOptions =
    [
        new(ReleaseModifierTapBehavior.None, "Do nothing"),
        new(ReleaseModifierTapBehavior.UntilWorldClick, "Release until world click or next tap"),
        new(ReleaseModifierTapBehavior.UntilNextTap, "Release until next tap"),
    ];

    private static readonly string[] ReleaseModifierTapBehaviorLabels = CreateLabels(ReleaseModifierTapBehaviorOptions);

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

        var releaseModifierTapBehaviorIndex = FindOptionIndex(
            ReleaseModifierTapBehaviorOptions,
            config.General.ReleaseModifierTapBehavior);
        if (ImGui.Combo(
                "Tap release modifier",
                ref releaseModifierTapBehaviorIndex,
                ReleaseModifierTapBehaviorLabels,
                ReleaseModifierTapBehaviorLabels.Length))
        {
            config.General.ReleaseModifierTapBehavior = ReleaseModifierTapBehaviorOptions[releaseModifierTapBehaviorIndex].Value;
            save();
        }
        ConfigWindow.DrawTooltip(GetReleaseModifierTapBehaviorTooltip());

        ConfigWindow.DrawSection("Toggle keybind");
        toggleKeybindEditor.Draw(config.ToggleKeybind);

        ConfigWindow.DrawSection("Server Info Bar");
        dtrSettingsEditor.Draw(config.Dtr);

        ConfigWindow.DrawSection("Configuration");
        configurationTransferPanel.Draw();
    }

    private readonly record struct ComboOption<T>(T Value, string Label);

    private string GetReleaseModifierTapBehaviorTooltip()
    {
        if (config.General.ReleaseModifier == ReleaseModifierKey.None)
        {
            return "No temporary release modifier is configured. Choose Alt, Control, or Shift to use tap release.";
        }

        var modifier = config.General.ReleaseModifier;
        return config.General.ReleaseModifierTapBehavior switch
        {
            ReleaseModifierTapBehavior.UntilWorldClick =>
                $"Hold {modifier} to temporarily release the cursor.\n\n" +
                $"Selected mode: release until world click or next tap. Tap {modifier} to keep the cursor free until you tap {modifier} again or press LMB/RMB over the game world.\n\n" +
                "Clicking native game or Dalamud UI should not relock it.",
            ReleaseModifierTapBehavior.UntilNextTap =>
                $"Hold {modifier} to temporarily release the cursor.\n\n" +
                $"Selected mode: release until next tap. Tap {modifier} to toggle cursor release. MouseLock relocks only when you tap {modifier} again.\n\n" +
                "World clicks and native LMB + RMB movement will not relock it.",
            _ =>
                $"Hold {modifier} to temporarily release the cursor.\n\n" +
                $"Selected mode: do nothing. Tapping {modifier} has no lasting release behavior.",
        };
    }

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
