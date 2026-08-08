using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.MouseLook;

namespace MouseLock.Windows;

internal sealed class FirstRunWindow : Window
{
    private readonly SystemConfiguration _config;

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

    public FirstRunWindow(SystemConfiguration config)
        : base("MouseLock Quick Start###MouseLockFirstRun")
    {
        _config = config;
        IsOpen = !_config.General.FirstRunIntroCompleted;
        ShowCloseButton = true;
        Size = new Vector2(500.0f, 0.0f);
    }

    public override void Draw()
    {
        ImGui.TextWrapped("MouseLock keeps your camera locked until something needs the cursor.");
        ImGui.Spacing();

        ImGui.TextWrapped("Important notes:");
        ImGui.BulletText("Camera active: Locks look direction to mouse movement.");
        ImGui.BulletText("Cursor position: Recenters to the middle of the screen while locking.");
        ImGui.BulletText("Window placement: Avoid placing plugin windows directly in the center.");

        DrawQuickSettings();

        ImGui.Spacing();
        ImGui.TextWrapped(GetReleaseModifierHelpText());

        ImGui.Spacing();
        if (ImGui.Button("Got it"))
        {
            CompleteIntro();
        }

        ImGui.SameLine();
        if (ImGui.Button("Open full settings"))
        {
            PluginState.ConfigWindow.IsOpen = true;
            CompleteIntro();
        }
    }

    public override void OnClose()
    {
        CompleteIntro();
    }

    private void DrawQuickSettings()
    {
        var enabled = _config.General.Enabled;
        if (ImGui.Checkbox("Enable MouseLock", ref enabled))
        {
            MouseLockStateController.SetEnabled(enabled);
        }

        var releaseModifierIndex = FindOptionIndex(ReleaseModifierOptions, _config.General.ReleaseModifier);
        if (ImGui.Combo("Temporary release modifier", ref releaseModifierIndex, ReleaseModifierLabels, ReleaseModifierLabels.Length))
        {
            _config.General.ReleaseModifier = ReleaseModifierOptions[releaseModifierIndex].Value;
            Save();
        }

        var releaseModifierTapBehaviorIndex = FindOptionIndex(
            ReleaseModifierTapBehaviorOptions,
            _config.General.ReleaseModifierTapBehavior);
        if (ImGui.Combo(
                "Tap release modifier",
                ref releaseModifierTapBehaviorIndex,
                ReleaseModifierTapBehaviorLabels,
                ReleaseModifierTapBehaviorLabels.Length))
        {
            _config.General.ReleaseModifierTapBehavior =
                ReleaseModifierTapBehaviorOptions[releaseModifierTapBehaviorIndex].Value;
            Save();
        }

        var conditions = _config.Activation.Conditions;

        var disableWhileTextInputActive = conditions.DisableWhileTextInputActive;
        if (ImGui.Checkbox("Pause while chat/text input is active", ref disableWhileTextInputActive))
        {
            conditions.DisableWhileTextInputActive = disableWhileTextInputActive;
            Save();
        }

        var disableWhenDalamudWindowFocused = conditions.DisableWhenDalamudWindowFocused;
        if (ImGui.Checkbox("Pause while Dalamud/ImGui windows are focused", ref disableWhenDalamudWindowFocused))
        {
            conditions.DisableWhenDalamudWindowFocused = disableWhenDalamudWindowFocused;
            Save();
        }

        var disableWhenNativeAddonFocused = conditions.DisableWhenNativeAddonFocused;
        if (ImGui.Checkbox("Pause while native game windows are focused", ref disableWhenNativeAddonFocused))
        {
            conditions.DisableWhenNativeAddonFocused = disableWhenNativeAddonFocused;
            Save();
        }
    }

    private string GetReleaseModifierHelpText()
    {
        if (_config.General.ReleaseModifier == ReleaseModifierKey.None)
        {
            return "No temporary release modifier is configured. You can add one later in General settings.";
        }

        var modifier = _config.General.ReleaseModifier;
        return _config.General.ReleaseModifierTapBehavior switch
        {
            ReleaseModifierTapBehavior.UntilWorldClick =>
                $"Hold {modifier} to temporarily release the cursor. Tap {modifier} to keep the cursor free until you tap {modifier} again or press LMB/RMB over the game world.",
            ReleaseModifierTapBehavior.UntilNextTap =>
                $"Hold {modifier} to temporarily release the cursor. Tap {modifier} to toggle cursor release; MouseLock relocks only when you tap {modifier} again.",
            _ => $"Hold {modifier} to temporarily release the cursor.",
        };
    }

    private void CompleteIntro()
    {
        if (_config.General.FirstRunIntroCompleted)
        {
            IsOpen = false;
            return;
        }

        _config.General.FirstRunIntroCompleted = true;
        IsOpen = false;
        Save();
    }

    private void Save()
    {
        ConfigRepository.Save(_config);
        PluginState.MouseLookService?.RefreshCurrentStatus();
        PluginState.DtrStatusService?.Refresh();
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
