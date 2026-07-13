using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;

namespace MouseLock.Windows.Components;

internal sealed class DtrSettingsEditor(Action save)
{
    private static readonly ComboOption<DtrTextMode>[] TextModeOptions =
    [
        new(DtrTextMode.Simple, "Simple: MouseLock: Paused"),
        new(DtrTextMode.Detailed, "Detailed: MouseLock: Paused: Reason"),
        new(DtrTextMode.Compact, "Compact: ML: Paused"),
    ];

    private static readonly string[] TextModeLabels = CreateLabels(TextModeOptions);

    public void Draw(DtrSettings settings)
    {
        var enabled = settings.Enabled;
        if (ImGui.Checkbox("Show in Server Info Bar", ref enabled))
        {
            settings.Enabled = enabled;
            save();
        }

        using (ImRaii.Disabled(!settings.Enabled))
        {
            ConfigWindow.DrawNestIndicator(1);
            var textModeIndex = FindOptionIndex(TextModeOptions, settings.TextMode);
            if (ImGui.Combo("Status text", ref textModeIndex, TextModeLabels, TextModeLabels.Length))
            {
                settings.TextMode = TextModeOptions[textModeIndex].Value;
                save();
            }
        }

        ImGui.TextWrapped("Left-click the Server Info Bar entry to toggle MouseLock on/off. Right-click it to toggle this config window.");
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
