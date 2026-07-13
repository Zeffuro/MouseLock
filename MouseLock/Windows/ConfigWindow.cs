using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;

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
        DrawSection("MouseLook");

        var enabled = _config.General.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            _config.General.Enabled = enabled;
            Save();
        }

        var releaseModifierIndex = FindOptionIndex(ReleaseModifierOptions, _config.General.ReleaseModifier);
        if (ImGui.Combo("Temporary release modifier", ref releaseModifierIndex, ReleaseModifierLabels, ReleaseModifierLabels.Length))
        {
            _config.General.ReleaseModifier = ReleaseModifierOptions[releaseModifierIndex].Value;
            Save();
        }

        DrawSection("Activation conditions");

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

        var countCountdownAsCombat = conditions.CountCountdownAsCombat;
        if (DrawNestedCheckbox("Treat countdown as combat", ref countCountdownAsCombat))
        {
            conditions.CountCountdownAsCombat = countCountdownAsCombat;
            Save();
        }

        DrawSection("Toggle keybind");
        DrawToggleKeybindSettings();

        DrawSection("Mouse actions");
        DrawMouseActionBinding("LMB", _config.General.MouseActions.LeftButton);
        DrawMouseActionBinding("RMB", _config.General.MouseActions.RightButton);

        DrawSection("Compatibility");

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

        DrawSection("Server Info Bar");
        DrawDtrSettings();

        DrawSection("Diagnostics");

        var debugEnabled = _config.General.DebugEnabled;
        if (ImGui.Checkbox("Debug enabled", ref debugEnabled))
        {
            _config.General.DebugEnabled = debugEnabled;
            Save();
        }
    }

    public void Dispose()
    {
    }

    private void Save() => ConfigRepository.Save(_config);
}
