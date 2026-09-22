using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class TargetingTab(SystemConfiguration config, Action save)
{
    private static readonly string[] TargetModeLabels = ["Soft target", "Focus target"];
    private static readonly AimTargetMode[] TargetModes = [AimTargetMode.Soft, AimTargetMode.Focus];
    private readonly ReticleSettingsEditor _reticleEditor = new(config, save);
    internal bool PreviewTarget => _reticleEditor.PreviewTarget;
    internal bool IsSelected { get; private set; }

    public void Draw()
    {
        using var tab = ImRaii.TabItem("Targeting");
        IsSelected = tab;
        if (!tab) return;

        var settings = config.Targeting;
        var enabled = settings.Enabled;
        if (ImGui.Checkbox("Target while aiming", ref enabled))
        {
            settings.Enabled = enabled;
            save();
        }
        ImGui.TextWrapped("Updates your target when you move the camera during mouselook.");
        ImGui.TextWrapped("For targeting on click, bind 'Target under reticle' in Mouse Actions. Works with automatic targeting off.");

        using (ImRaii.Disabled(!settings.Enabled))
        {
            var onlyInCombat = settings.OnlyInCombat;
            if (ImGui.Checkbox("Only auto-target in combat", ref onlyInCombat))
            {
                settings.OnlyInCombat = onlyInCombat;
                save();
            }
            ConfigWindow.DrawTooltip("Outside combat, automatic targeting stops and its soft target is released. Mouse look, the reticle, and targeting on click still work. Existing pause settings still apply.");
        }

        var tolerance = settings.AimTolerance;
        if (ImGui.SliderFloat("Aim tolerance", ref tolerance, 0, 60, "%.0f px"))
        {
            settings.AimTolerance = tolerance;
            save();
        }
        ConfigWindow.DrawTooltip("Checks near the reticle when nothing is directly under it. Zero requires exact aim.");

        var mode = Array.IndexOf(TargetModes, settings.Mode);
        if (ImGui.Combo("Target mode", ref mode, TargetModeLabels, TargetModeLabels.Length))
        {
            settings.Mode = TargetModes[mode];
            save();
        }
        if (settings.Mode == AimTargetMode.Soft)
        {
            var keepOnLookAway = settings.KeepSoftTargetOnLookAway;
            if (ImGui.Checkbox("Keep soft target when looking away", ref keepOnLookAway))
            {
                settings.KeepSoftTargetOnLookAway = keepOnLookAway;
                save();
            }

            var keepAfterAction = settings.KeepSoftTargetAfterAction;
            if (ImGui.Checkbox("Keep soft target after using actions", ref keepAfterAction))
            {
                settings.KeepSoftTargetAfterAction = keepAfterAction;
                save();
            }
        }
        else
        {
            ImGui.TextWrapped("Keeps your last target until you look at another one.");
        }

        if (PluginState.TargetingService?.Error is { } error)
            ImGui.TextWrapped(error);
        if (PluginState.TargetingService?.SoftTargetError is { } softTargetError)
            ImGui.TextWrapped(softTargetError);

        ConfigWindow.DrawSection("Target filters");
        DrawKind("Enemies (attackable targets)", AimTargetKinds.Enemies);
        DrawKind("Friendly players", AimTargetKinds.FriendlyPlayers);
        DrawKind("Friendly NPCs and companions", AimTargetKinds.FriendlyNpcs);
        DrawKind("Other objects", AimTargetKinds.OtherObjects);
        if (settings.TargetKinds == AimTargetKinds.None)
            ImGui.TextWrapped("Select at least one target type to enable targeting.");

        var includeDead = settings.IncludeDeadTargets;
        if (ImGui.Checkbox("Include dead targets", ref includeDead))
        {
            settings.IncludeDeadTargets = includeDead;
            save();
        }

        _reticleEditor.Draw();
    }

    private void DrawKind(string label, AimTargetKinds kind)
    {
        var selected = (config.Targeting.TargetKinds & kind) != 0;
        if (!ImGui.Checkbox(label, ref selected)) return;

        config.Targeting.TargetKinds = selected
            ? config.Targeting.TargetKinds | kind
            : config.Targeting.TargetKinds & ~kind;
        save();
    }
}
