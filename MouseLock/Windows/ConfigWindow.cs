using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using MouseLock.Compatibility;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow : Window, IDisposable
{
    private readonly SystemConfiguration _config;

    public ConfigWindow(SystemConfiguration config) : base("MouseLock Config")
    {
        _config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600.0f, 550.0f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        DrawMouseLookStatusCard();

        using var tabBar = ImRaii.TabBar("MouseLockConfigTabs");
        if (!tabBar)
        {
            return;
        }

        DrawGeneralTab();
        DrawActivationTab();
        DrawMouseActionsTab();
        DrawCompatibilityTab();
        DrawDiagnosticsTab();
    }

    public void Dispose()
    {
    }

    private void DrawGeneralTab()
    {
        using var tab = ImRaii.TabItem("General");
        if (!tab)
        {
            return;
        }

        var enabled = _config.General.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            MouseLockSettingsActions.SetEnabled(enabled);
        }

        var releaseModifierIndex = FindOptionIndex(ReleaseModifierOptions, _config.General.ReleaseModifier);
        if (ImGui.Combo("Temporary release modifier", ref releaseModifierIndex, ReleaseModifierLabels, ReleaseModifierLabels.Length))
        {
            _config.General.ReleaseModifier = ReleaseModifierOptions[releaseModifierIndex].Value;
            Save();
        }

        var restoreCursorPositionOnRelease = _config.General.RestoreCursorPositionOnRelease;
        if (ImGui.Checkbox("Return cursor to previous position when released", ref restoreCursorPositionOnRelease))
        {
            _config.General.RestoreCursorPositionOnRelease = restoreCursorPositionOnRelease;
            Save();
        }
        DrawTooltip("Disable this to leave the cursor centered when MouseLock pauses or releases.");

        var stickyReleaseEnabled = _config.General.StickyReleaseEnabled;
        if (ImGui.Checkbox("Tap release modifier to keep cursor released", ref stickyReleaseEnabled))
        {
            _config.General.StickyReleaseEnabled = stickyReleaseEnabled;
            Save();
        }
        DrawTooltip("When enabled, tap the release modifier to keep the cursor free. Tap it again or click back into the world to relock.");

        DrawSection("Toggle keybind");
        DrawToggleKeybindSettings();

        DrawSection("Server Info Bar");
        DrawDtrSettings();

        DrawSection("Configuration");
        DrawImportExportSettings();
    }

    private void DrawActivationTab()
    {
        using var tab = ImRaii.TabItem("Activation");
        if (!tab)
        {
            return;
        }

        var conditions = _config.General.Conditions;

        var disableWhileTextInputActive = conditions.DisableWhileTextInputActive;
        if (ImGui.Checkbox("Pause while chat/text input is active", ref disableWhileTextInputActive))
        {
            conditions.DisableWhileTextInputActive = disableWhileTextInputActive;
            Save();
        }

        var disableWhenTalkAddonVisible = conditions.DisableWhenTalkAddonVisible;
        if (ImGui.Checkbox("Pause while talking to NPCs", ref disableWhenTalkAddonVisible))
        {
            conditions.DisableWhenTalkAddonVisible = disableWhenTalkAddonVisible;
            Save();
        }

        var disableWhenNativeAddonFocused = conditions.DisableWhenNativeAddonFocused;
        if (ImGui.Checkbox("Pause while a native game window is focused", ref disableWhenNativeAddonFocused))
        {
            conditions.DisableWhenNativeAddonFocused = disableWhenNativeAddonFocused;
            Save();
        }

        var disableWhenNativeAddonHovered = conditions.DisableWhenNativeAddonHovered;
        if (DrawNestedCheckbox("Also pause while hovering native game windows", ref disableWhenNativeAddonHovered))
        {
            conditions.DisableWhenNativeAddonHovered = disableWhenNativeAddonHovered;
            Save();
        }
        DrawTooltip("Not recommended, can cause weird behaviour if an addon is in the direct center");

        var requireCombat = conditions.RequireCombat;
        if (ImGui.Checkbox("Only lock in combat", ref requireCombat))
        {
            conditions.RequireCombat = requireCombat;
            Save();
        }

        DrawDisabled(!conditions.RequireCombat, () =>
        {
            var countCountdownAsCombat = conditions.CountCountdownAsCombat;
            if (DrawNestedCheckbox("Treat countdown as combat", ref countCountdownAsCombat))
            {
                conditions.CountCountdownAsCombat = countCountdownAsCombat;
                Save();
            }
        });

        DrawSection("Resume behavior");
        var resumePolicyIndex = FindOptionIndex(ResumePolicyOptions, _config.General.ResumePolicy);
        if (ImGui.Combo("After a pause ends", ref resumePolicyIndex, ResumePolicyLabels, ResumePolicyLabels.Length))
        {
            _config.General.ResumePolicy = ResumePolicyOptions[resumePolicyIndex].Value;
            Save();
        }

        DrawDisabled(_config.General.ResumePolicy != MouseLookResumePolicy.Delay, () =>
        {
            DrawNestIndicator(1);
            var resumeDelay = _config.General.ResumeDelayMilliseconds;
            ImGui.SetNextItemWidth(ImGui.GetFrameHeight() * 4.0f);
            if (ImGui.InputInt("Delay (ms)", ref resumeDelay, 0, 0))
            {
                _config.General.ResumeDelayMilliseconds = Math.Clamp(resumeDelay, 100, 2_000);
                Save();
            }
        });

        if (_config.General.ResumePolicy == MouseLookResumePolicy.WorldClick)
        {
            ImGui.TextDisabled("MouseLock resumes when you click back into the world, not while a native game window is focused or hovered.");
        }

        DrawSection("Game state pauses");
        DrawGameStatePauseSettings(conditions);

        DrawNativeAddonExceptionSettings();
    }

    private void DrawGameStatePauseSettings(MouseLookConditionSettings conditions)
    {
        var disableDuringCutscenes = conditions.DisableDuringCutscenes;
        if (ImGui.Checkbox("Pause during cutscenes", ref disableDuringCutscenes))
        {
            conditions.DisableDuringCutscenes = disableDuringCutscenes;
            Save();
        }

        var disableDuringGpose = conditions.DisableDuringGpose;
        if (ImGui.Checkbox("Pause during GPose", ref disableDuringGpose))
        {
            conditions.DisableDuringGpose = disableDuringGpose;
            Save();
        }

        var disableDuringTerritoryTransitions = conditions.DisableDuringTerritoryTransitions;
        if (ImGui.Checkbox("Pause during territory transitions", ref disableDuringTerritoryTransitions))
        {
            conditions.DisableDuringTerritoryTransitions = disableDuringTerritoryTransitions;
            Save();
        }

        var disableDuringCrafting = conditions.DisableDuringCrafting;
        if (ImGui.Checkbox("Pause while crafting", ref disableDuringCrafting))
        {
            conditions.DisableDuringCrafting = disableDuringCrafting;
            Save();
        }

        var disableDuringGathering = conditions.DisableDuringGathering;
        if (ImGui.Checkbox("Pause while gathering", ref disableDuringGathering))
        {
            conditions.DisableDuringGathering = disableDuringGathering;
            Save();
        }

        var disableDuringGroundTargeting = conditions.DisableDuringGroundTargeting;
        if (ImGui.Checkbox("Pause during ground targeting", ref disableDuringGroundTargeting))
        {
            conditions.DisableDuringGroundTargeting = disableDuringGroundTargeting;
            Save();
        }

        var disableDuringHousingPlacement = conditions.DisableDuringHousingPlacement;
        if (ImGui.Checkbox("Pause while using housing placement", ref disableDuringHousingPlacement))
        {
            conditions.DisableDuringHousingPlacement = disableDuringHousingPlacement;
            Save();
        }

        var disableWhileMounted = conditions.DisableWhileMounted;
        if (ImGui.Checkbox("Pause while mounted", ref disableWhileMounted))
        {
            conditions.DisableWhileMounted = disableWhileMounted;
            Save();
        }

        var disableDuringGamepadMouseMode = conditions.DisableDuringGamepadMouseMode;
        if (ImGui.Checkbox("Pause during gamepad mouse mode", ref disableDuringGamepadMouseMode))
        {
            conditions.DisableDuringGamepadMouseMode = disableDuringGamepadMouseMode;
            Save();
        }
    }

    private void DrawMouseActionsTab()
    {
        using var tab = ImRaii.TabItem("Mouse Actions");
        if (!tab)
        {
            return;
        }

        var actions = _config.General.MouseActions;

        DrawMouseButtonActionGroup(
            "LMB",
            actions.LeftButton,
            actions.LeftAltButton,
            actions.LeftControlButton,
            actions.LeftShiftButton);

        DrawMouseButtonActionGroup(
            "RMB",
            actions.RightButton,
            actions.RightAltButton,
            actions.RightControlButton,
            actions.RightShiftButton);
    }

    private void DrawCompatibilityTab()
    {
        using var tab = ImRaii.TabItem("Compatibility");
        if (!tab)
        {
            return;
        }

        var hideCursorOverlayPlugins = _config.General.Compatibility.HideCursorOverlayPluginsDuringMouseLook;
        if (ImGui.Checkbox("Hide cursor overlay plugins during mouselook", ref hideCursorOverlayPlugins))
        {
            _config.General.Compatibility.HideCursorOverlayPluginsDuringMouseLook = hideCursorOverlayPlugins;
            Save();
        }

        var disableDuringTPieRing = _config.General.Compatibility.DisableDuringTPieRing;
        if (ImGui.Checkbox("Pause while using TPie", ref disableDuringTPieRing))
        {
            _config.General.Compatibility.DisableDuringTPieRing = disableDuringTPieRing;
            Save();
        }
    }

    private void DrawDiagnosticsTab()
    {
        using var tab = ImRaii.TabItem("Diagnostics");
        if (!tab)
        {
            return;
        }

        DrawDiagnosticsSettings();
    }

    private void Save()
    {
        ConfigRepository.Save(_config);
        PluginState.MouseLookService?.RefreshCurrentStatus();
    }

    private static void DrawMouseLookStatusCard()
    {
        var status = PluginState.MouseLookService?.Status
                     ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);

        ImGui.TextUnformatted($"Status: {status.Summary}");
        ImGui.TextDisabled(status.Detail);
        if (ExternalSuspensionState.IsSuspended)
        {
            ImGui.TextDisabled($"External suspensions: {ExternalSuspensionState.SourcesSummary}");
        }

        ImGui.Spacing();
    }
}
