using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class ActivationTab(
    SystemConfiguration config,
    Action save,
    NativeAddonExceptionEditor nativeAddonExceptionEditor)
{
    private static readonly ComboOption<MouseLookResumePolicy>[] ResumePolicyOptions =
    [
        new(MouseLookResumePolicy.Immediate, "Immediately"),
        new(MouseLookResumePolicy.Delay, "After a short delay"),
        new(MouseLookResumePolicy.WorldClick, "On next world click"),
    ];

    private static readonly string[] ResumePolicyLabels = CreateLabels(ResumePolicyOptions);

    public void Draw()
    {
        using var tab = ImRaii.TabItem("Activation");
        if (!tab)
        {
            return;
        }

        var conditions = config.General.Conditions;

        var disableWhileTextInputActive = conditions.DisableWhileTextInputActive;
        if (ImGui.Checkbox("Pause while chat/text input is active", ref disableWhileTextInputActive))
        {
            conditions.DisableWhileTextInputActive = disableWhileTextInputActive;
            save();
        }

        var disableWhenTalkAddonVisible = conditions.DisableWhenTalkAddonVisible;
        if (ImGui.Checkbox("Pause while talking to NPCs", ref disableWhenTalkAddonVisible))
        {
            conditions.DisableWhenTalkAddonVisible = disableWhenTalkAddonVisible;
            save();
        }

        var disableWhenNativeAddonFocused = conditions.DisableWhenNativeAddonFocused;
        if (ImGui.Checkbox("Pause while a native game window is focused", ref disableWhenNativeAddonFocused))
        {
            conditions.DisableWhenNativeAddonFocused = disableWhenNativeAddonFocused;
            save();
        }

        var disableWhenNativeAddonHovered = conditions.DisableWhenNativeAddonHovered;
        if (ConfigWindow.DrawNestedCheckbox("Also pause while hovering native game windows", ref disableWhenNativeAddonHovered))
        {
            conditions.DisableWhenNativeAddonHovered = disableWhenNativeAddonHovered;
            save();
        }
        ConfigWindow.DrawTooltip("Not recommended, can cause weird behaviour if an addon is in the direct center");

        var requireCombat = conditions.RequireCombat;
        if (ImGui.Checkbox("Only lock in combat", ref requireCombat))
        {
            conditions.RequireCombat = requireCombat;
            save();
        }

        ConfigWindow.DrawDisabled(!conditions.RequireCombat, () =>
        {
            var countCountdownAsCombat = conditions.CountCountdownAsCombat;
            if (ConfigWindow.DrawNestedCheckbox("Treat countdown as combat", ref countCountdownAsCombat))
            {
                conditions.CountCountdownAsCombat = countCountdownAsCombat;
                save();
            }
        });

        ConfigWindow.DrawSection("Resume behavior");
        var resumePolicyIndex = FindOptionIndex(ResumePolicyOptions, config.General.ResumePolicy);
        if (ImGui.Combo("After a pause ends", ref resumePolicyIndex, ResumePolicyLabels, ResumePolicyLabels.Length))
        {
            config.General.ResumePolicy = ResumePolicyOptions[resumePolicyIndex].Value;
            save();
        }

        ConfigWindow.DrawDisabled(config.General.ResumePolicy != MouseLookResumePolicy.Delay, () =>
        {
            ConfigWindow.DrawNestIndicator(1);
            var resumeDelay = config.General.ResumeDelayMilliseconds;
            ImGui.SetNextItemWidth(ImGui.GetFrameHeight() * 4.0f);
            if (ImGui.InputInt("Delay (ms)", ref resumeDelay, 0, 0))
            {
                config.General.ResumeDelayMilliseconds = Math.Clamp(resumeDelay, 100, 2_000);
                save();
            }
        });

        if (config.General.ResumePolicy == MouseLookResumePolicy.WorldClick)
        {
            ImGui.TextDisabled("MouseLock resumes when you click back into the world, not while a native game window is focused or hovered.");
        }

        ConfigWindow.DrawSection("Game state pauses");
        DrawGameStatePauseSettings(conditions);

        nativeAddonExceptionEditor.Draw(conditions);
    }

    private void DrawGameStatePauseSettings(MouseLookConditionSettings conditions)
    {
        var disableDuringCutscenes = conditions.DisableDuringCutscenes;
        if (ImGui.Checkbox("Pause during cutscenes", ref disableDuringCutscenes))
        {
            conditions.DisableDuringCutscenes = disableDuringCutscenes;
            save();
        }

        var disableDuringGpose = conditions.DisableDuringGpose;
        if (ImGui.Checkbox("Pause during GPose", ref disableDuringGpose))
        {
            conditions.DisableDuringGpose = disableDuringGpose;
            save();
        }

        var disableDuringTerritoryTransitions = conditions.DisableDuringTerritoryTransitions;
        if (ImGui.Checkbox("Pause during territory transitions", ref disableDuringTerritoryTransitions))
        {
            conditions.DisableDuringTerritoryTransitions = disableDuringTerritoryTransitions;
            save();
        }

        var disableDuringCrafting = conditions.DisableDuringCrafting;
        if (ImGui.Checkbox("Pause while crafting", ref disableDuringCrafting))
        {
            conditions.DisableDuringCrafting = disableDuringCrafting;
            save();
        }

        var disableDuringGathering = conditions.DisableDuringGathering;
        if (ImGui.Checkbox("Pause while gathering", ref disableDuringGathering))
        {
            conditions.DisableDuringGathering = disableDuringGathering;
            save();
        }

        var disableDuringGroundTargeting = conditions.DisableDuringGroundTargeting;
        if (ImGui.Checkbox("Pause during ground targeting", ref disableDuringGroundTargeting))
        {
            conditions.DisableDuringGroundTargeting = disableDuringGroundTargeting;
            save();
        }

        var disableDuringHousingPlacement = conditions.DisableDuringHousingPlacement;
        if (ImGui.Checkbox("Pause while using housing placement", ref disableDuringHousingPlacement))
        {
            conditions.DisableDuringHousingPlacement = disableDuringHousingPlacement;
            save();
        }

        var disableWhileMounted = conditions.DisableWhileMounted;
        if (ImGui.Checkbox("Pause while mounted", ref disableWhileMounted))
        {
            conditions.DisableWhileMounted = disableWhileMounted;
            save();
        }

        var disableDuringGamepadMouseMode = conditions.DisableDuringGamepadMouseMode;
        if (ImGui.Checkbox("Pause during gamepad mouse mode", ref disableDuringGamepadMouseMode))
        {
            conditions.DisableDuringGamepadMouseMode = disableDuringGamepadMouseMode;
            save();
        }
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
