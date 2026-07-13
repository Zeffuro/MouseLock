using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Windows.Components;

namespace MouseLock.Windows.Tabs;

internal sealed class MouseActionsTab(
    SystemConfiguration config,
    Action save,
    HotbarSlotPicker hotbarSlotPicker)
{
    private const float LayerColumnWidthInFrames = 5.0f;
    private const float ComboWidthInFrames = 11.0f;

    private static readonly ComboOption<MouseButtonBindingKind>[] BindingKindOptions =
    [
        new(MouseButtonBindingKind.None, "None"),
        new(MouseButtonBindingKind.GameInput, "Game input"),
        new(MouseButtonBindingKind.HotbarSlot, "Hotbar slot"),
        new(MouseButtonBindingKind.TemporaryRelease, "Temporary release"),
        new(MouseButtonBindingKind.ToggleMouseLock, "Toggle MouseLock"),
        new(MouseButtonBindingKind.OpenConfig, "Open config"),
    ];

    private static readonly string[] BindingKindLabels = CreateLabels(BindingKindOptions);

    private static readonly ComboOption<CuratedGameInput>[] GameInputOptions =
    [
        new(CuratedGameInput.TabTargetNext, "Tab target next"),
        new(CuratedGameInput.TabTargetPrevious, "Tab target previous"),
        new(CuratedGameInput.TargetNearestEnemy, "Target nearest enemy"),
        new(CuratedGameInput.MoveForward, "Move forward"),
        new(CuratedGameInput.MoveBack, "Move back"),
        new(CuratedGameInput.MoveLeft, "Move left"),
        new(CuratedGameInput.MoveRight, "Move right"),
        new(CuratedGameInput.StrafeLeft, "Strafe left"),
        new(CuratedGameInput.StrafeRight, "Strafe right"),
        new(CuratedGameInput.Jump, "Jump"),
        new(CuratedGameInput.Autorun, "Autorun"),
    ];

    private static readonly string[] GameInputLabels = CreateLabels(GameInputOptions);

    public void Draw()
    {
        using var tab = ImRaii.TabItem("Mouse Actions");
        if (!tab)
        {
            return;
        }

        var actions = config.General.MouseActions;

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

    private void DrawMouseButtonActionGroup(
        string label,
        MouseButtonGameInputBinding defaultBinding,
        MouseButtonGameInputBinding altBinding,
        MouseButtonGameInputBinding controlBinding,
        MouseButtonGameInputBinding shiftBinding)
    {
        ConfigWindow.DrawSection(label);

        using var table = ImRaii.Table(
            $"##{label}ActionLayers",
            2,
            ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.SizingStretchProp);
        if (!table)
        {
            return;
        }

        ImGui.TableSetupColumn("##Modifier", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight() * LayerColumnWidthInFrames);
        ImGui.TableSetupColumn("##Binding", ImGuiTableColumnFlags.WidthStretch);

        DrawMouseActionLayerRow($"{label}Default", "Default", ReleaseModifierKey.None, defaultBinding);
        DrawMouseActionLayerRow($"{label}Alt", "Alt", ReleaseModifierKey.Alt, altBinding);
        DrawMouseActionLayerRow($"{label}Control", "Control", ReleaseModifierKey.Control, controlBinding);
        DrawMouseActionLayerRow($"{label}Shift", "Shift", ReleaseModifierKey.Shift, shiftBinding);
    }

    private void DrawMouseActionLayerRow(
        string id,
        string layerLabel,
        ReleaseModifierKey layerModifier,
        MouseButtonGameInputBinding binding)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        if (layerModifier != ReleaseModifierKey.None && binding.Kind == MouseButtonBindingKind.None)
        {
            ImGui.TextDisabled(layerLabel);
        }
        else
        {
            ImGui.TextUnformatted(layerLabel);
        }

        ImGui.TableNextColumn();
        DrawMouseActionBinding(id, binding);
        DrawMouseActionLayerConflictWarning(layerModifier, binding);
    }

    private void DrawMouseActionBinding(string label, MouseButtonGameInputBinding binding)
    {
        using var id = ImRaii.PushId(label);

        var kindIndex = FindOptionIndex(BindingKindOptions, binding.Kind);
        ImGui.SetNextItemWidth(GetControlWidth());
        if (ImGui.Combo("##Binding", ref kindIndex, BindingKindLabels, BindingKindLabels.Length))
        {
            binding.Kind = BindingKindOptions[kindIndex].Value;
            binding.Clamp();
            save();
        }

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.GameInput:
                DrawGameInputBinding(binding);
                break;
            case MouseButtonBindingKind.HotbarSlot:
                hotbarSlotPicker.Draw(binding);
                break;
        }
    }

    private void DrawGameInputBinding(MouseButtonGameInputBinding binding)
    {
        ConfigWindow.DrawNestIndicator(1);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Game input");
        ImGui.SameLine();

        var gameInputIndex = FindOptionIndex(GameInputOptions, binding.GameInput);
        ImGui.SetNextItemWidth(GetControlWidth());
        if (ImGui.Combo("##GameInput", ref gameInputIndex, GameInputLabels, GameInputLabels.Length))
        {
            binding.GameInput = GameInputOptions[gameInputIndex].Value;
            save();
        }
    }

    private void DrawMouseActionLayerConflictWarning(ReleaseModifierKey layerModifier, MouseButtonGameInputBinding binding)
    {
        if (binding.Kind == MouseButtonBindingKind.None ||
            config.General.ReleaseModifier != layerModifier)
        {
            return;
        }

        ConfigWindow.DrawNestIndicator(1);
        using var warningColor = ImRaii.PushColor(ImGuiCol.Text, GetWarningTextColor());
        ImGui.TextWrapped($"{GetReleaseModifierLabel(layerModifier)} is also the temporary release modifier, so this layer pauses MouseLock before the action can run.");
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

    private static string GetReleaseModifierLabel(ReleaseModifierKey modifier)
        => modifier switch
        {
            ReleaseModifierKey.Alt => "Alt",
            ReleaseModifierKey.Control => "Control",
            ReleaseModifierKey.Shift => "Shift",
            _ => "None",
        };

    private static float GetControlWidth()
        => MathF.Min(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * ComboWidthInFrames);

    private static Vector4 GetWarningTextColor()
        => ImGuiColors.WarningForeground;
}
