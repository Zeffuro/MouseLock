using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Hotbars;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private const int HotbarSlotPulseIntervalMilliseconds = 400;
    private const int HotbarPulseStatusDurationMilliseconds = 3_000;
    private const float HotbarSlotValueWidthInFrames = 3.0f;
    private const float HotbarSlotPickerSizeInFrames = 1.10f;
    private const float HotbarSlotPickerSpacing = 2.0f;
    private const float MouseActionLayerColumnWidthInFrames = 5.0f;
    private const float MouseActionComboWidthInFrames = 11.0f;

    private readonly Stopwatch _hotbarSlotPulseTimer = new();
    private readonly Stopwatch _hotbarPulseStatusTimer = new();
    private string? _hotbarPulseStatus;

    private void DrawMouseButtonActionGroup(
        string label,
        MouseButtonGameInputBinding defaultBinding,
        MouseButtonGameInputBinding altBinding,
        MouseButtonGameInputBinding controlBinding,
        MouseButtonGameInputBinding shiftBinding)
    {
        DrawSection(label);

        using var table = ImRaii.Table(
            $"##{label}ActionLayers",
            2,
            ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.SizingStretchProp);
        if (!table)
        {
            return;
        }

        ImGui.TableSetupColumn("##Modifier", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight() * MouseActionLayerColumnWidthInFrames);
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
        ImGui.SetNextItemWidth(GetMouseActionControlWidth());
        if (ImGui.Combo("##Binding", ref kindIndex, BindingKindLabels, BindingKindLabels.Length))
        {
            binding.Kind = BindingKindOptions[kindIndex].Value;
            binding.Clamp();
            Save();
        }

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.GameInput:
                DrawGameInputBinding(binding);
                break;
            case MouseButtonBindingKind.HotbarSlot:
                DrawHotbarSlotBinding(binding);
                break;
        }
    }

    private void DrawGameInputBinding(MouseButtonGameInputBinding binding)
    {
        DrawNestIndicator(1);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Game input");
        ImGui.SameLine();

        var gameInputIndex = FindOptionIndex(GameInputOptions, binding.GameInput);
        ImGui.SetNextItemWidth(GetMouseActionControlWidth());
        if (ImGui.Combo("##GameInput", ref gameInputIndex, GameInputLabels, GameInputLabels.Length))
        {
            binding.GameInput = GameInputOptions[gameInputIndex].Value;
            Save();
        }
    }

    private void DrawMouseActionLayerConflictWarning(ReleaseModifierKey layerModifier, MouseButtonGameInputBinding binding)
    {
        if (binding.Kind == MouseButtonBindingKind.None ||
            _config.General.ReleaseModifier != layerModifier)
        {
            return;
        }

        DrawNestIndicator(1);
        using var warningColor = ImRaii.PushColor(ImGuiCol.Text, GetWarningTextColor());
        ImGui.TextWrapped($"{GetReleaseModifierLabel(layerModifier)} is also the temporary release modifier, so this layer pauses MouseLock before the action can run.");
    }

    private void DrawHotbarSlotBinding(MouseButtonGameInputBinding binding)
    {
        DrawWrappedNumberInput(
            "Hotbar",
            binding.Hotbar,
            1,
            10,
            value => binding.Hotbar = value,
            () => DrawHotbarSlotHighlightButton(binding));

        DrawWrappedNumberInput(
            "Slot",
            binding.Slot,
            1,
            12,
            value => binding.Slot = value);

        DrawHotbarSlotPicker(binding);
        DrawHotbarPulseStatus();
    }

    private void DrawWrappedNumberInput(
        string label,
        int value,
        int min,
        int max,
        Action<int> setValue,
        Action? drawTrailingAction = null)
    {
        DrawNestIndicator(1);
        using var id = ImRaii.PushId(label);

        var style = ImGui.GetStyle();
        var rowStartX = ImGui.GetCursorPosX();
        var labelWidth = GetHotbarSlotControlLabelWidth();
        var frameHeight = ImGui.GetFrameHeight();
        var arrowWidth = frameHeight;
        var valueWidth = frameHeight * HotbarSlotValueWidthInFrames;
        var previousButtonX = rowStartX + labelWidth + style.ItemInnerSpacing.X;
        var valueX = previousButtonX + arrowWidth + style.ItemInnerSpacing.X;
        var nextButtonX = valueX + valueWidth + style.ItemInnerSpacing.X;
        var trailingActionX = nextButtonX + arrowWidth + (style.ItemInnerSpacing.X * 2.0f);

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);

        SameLineAt(previousButtonX);
        if (ImGui.ArrowButton("##Previous", ImGuiDir.Left))
        {
            setValue(WrapValue(value, min, max, -1));
            Save();
        }

        SameLineAt(valueX);
        var inputValue = value;
        ImGui.SetNextItemWidth(valueWidth);
        if (ImGui.InputInt("##Value", ref inputValue, 0, 0))
        {
            setValue(Math.Clamp(inputValue, min, max));
            Save();
        }

        SameLineAt(nextButtonX);
        if (ImGui.ArrowButton("##Next", ImGuiDir.Right))
        {
            setValue(WrapValue(value, min, max, 1));
            Save();
        }

        if (drawTrailingAction is not null)
        {
            SameLineAt(trailingActionX);
            drawTrailingAction();
        }
    }

    private void DrawHotbarSlotHighlightButton(MouseButtonGameInputBinding binding)
    {
        if (ImGui.Button("Highlight"))
        {
            SetHotbarPulseStatus(
                HotbarSlotInterop.TryPulseVisibleSlot(binding.Hotbar, binding.Slot),
                binding.Hotbar);
        }

        if (ImGui.IsItemHovered())
        {
            PulseHotbarSlotOnHover(binding.Hotbar, binding.Slot);
            DrawTooltip("Pulse the selected slot on your visible in-game hotbar.");
        }
    }

    private void DrawHotbarSlotPicker(MouseButtonGameInputBinding binding)
    {
        DrawNestIndicator(1);
        using var group = ImRaii.Group();

        var size = new Vector2(ImGui.GetFrameHeight() * HotbarSlotPickerSizeInFrames);
        var spacing = HotbarSlotPickerSpacing;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var maxColumns = Math.Clamp((int)((availableWidth + spacing) / (size.X + spacing)), 1, 12);
        var preferredLayout = HotbarSlotInterop.GetPickerLayout(binding.Hotbar);
        var slotsPerRow = Math.Clamp(Math.Min(preferredLayout.Columns, maxColumns), 1, 12);

        using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(spacing, spacing));

        for (var slot = 1; slot <= 12; slot++)
        {
            if (slot > 1 && (slot - 1) % slotsPerRow != 0)
            {
                ImGui.SameLine();
            }

            DrawHotbarSlotPickerButton(binding, slot, size);
        }
    }

    private void DrawHotbarSlotPickerButton(MouseButtonGameInputBinding binding, int slot, Vector2 size)
    {
        using var id = ImRaii.PushId(slot);

        var selected = binding.Slot == slot;
        var selectionColor = GetHotbarSlotSelectionColor();
        using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, selectionColor with { W = 0.28f }, selected);
        using var hoveredColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, selectionColor with { W = 0.38f }, selected);
        using var activeColor = ImRaii.PushColor(ImGuiCol.ButtonActive, selectionColor with { W = 0.48f }, selected);

        var clicked = DrawHotbarSlotButtonContent(binding.Hotbar, slot, size);

        if (selected)
        {
            DrawHotbarSlotSelectedOverlay(selectionColor);
        }

        if (clicked)
        {
            binding.Slot = slot;
            Save();
        }

        if (ImGui.IsItemHovered())
        {
            PulseHotbarSlotOnHover(binding.Hotbar, slot);
            DrawTooltip(GetHotbarSlotTooltip(binding.Hotbar, slot));
        }
    }

    private static bool DrawHotbarSlotButtonContent(int hotbar, int slot, Vector2 size)
    {
        if (!HotbarSlotInterop.TryGetSlot(hotbar, slot, out var hotbarSlot))
        {
            return ImGui.Button(slot.ToString(), size);
        }

        var iconId = hotbarSlot.IconId;
        if (iconId == 0)
        {
            return ImGui.Button(slot.ToString(), size);
        }

        var lookup = new GameIconLookup(iconId, hotbarSlot.IsHqItem, false);
        if (!Service.TextureProvider.TryGetFromGameIcon(lookup, out var sharedTexture) ||
            !sharedTexture.TryGetWrap(out var texture, out _) ||
            texture is null)
        {
            return ImGui.Button(slot.ToString(), size);
        }

        return ImGui.ImageButton(texture.Handle, size, 0);
    }

    private void PulseHotbarSlotOnHover(int hotbar, int slot)
    {
        if (_hotbarSlotPulseTimer.IsRunning &&
            _hotbarSlotPulseTimer.ElapsedMilliseconds < HotbarSlotPulseIntervalMilliseconds)
        {
            return;
        }

        HotbarSlotInterop.TryPulseVisibleSlot(hotbar, slot);
        _hotbarSlotPulseTimer.Restart();
    }

    private void SetHotbarPulseStatus(bool success, int hotbar)
    {
        if (success)
        {
            _hotbarPulseStatus = null;
            _hotbarPulseStatusTimer.Reset();
            return;
        }

        _hotbarPulseStatus = $"Hotbar {hotbar} is not currently visible.";
        _hotbarPulseStatusTimer.Restart();
    }

    private void DrawHotbarPulseStatus()
    {
        if (_hotbarPulseStatus is null ||
            !_hotbarPulseStatusTimer.IsRunning ||
            _hotbarPulseStatusTimer.ElapsedMilliseconds >= HotbarPulseStatusDurationMilliseconds)
        {
            return;
        }

        DrawNestIndicator(1);
        ImGui.TextDisabled(_hotbarPulseStatus);
    }

    private static int WrapValue(int value, int min, int max, int delta)
    {
        var range = max - min + 1;
        var zeroBased = Math.Clamp(value, min, max) - min;
        return ((zeroBased + delta + range) % range) + min;
    }

    private static void SameLineAt(float cursorPosX)
    {
        ImGui.SameLine();
        ImGui.SetCursorPosX(cursorPosX);
    }

    private static float GetHotbarSlotControlLabelWidth()
        => MathF.Max(ImGui.CalcTextSize("Hotbar").X, ImGui.CalcTextSize("Slot").X);

    private static float GetMouseActionControlWidth()
        => MathF.Min(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * MouseActionComboWidthInFrames);

    private static Vector4 GetHotbarSlotSelectionColor()
    {
        var colors = ImGui.GetStyle().Colors;
        var color = colors[(int)ImGuiCol.CheckMark];
        if (color.W < 0.50f)
        {
            color = colors[(int)ImGuiCol.SliderGrabActive];
        }

        if (color.W < 0.50f)
        {
            color = colors[(int)ImGuiCol.HeaderActive];
        }

        if (color.W < 0.50f)
        {
            color = colors[(int)ImGuiCol.Text];
        }

        color = color with { W = 1.0f };
        var brightness = GetPerceivedBrightness(color);
        if (brightness < 0.45f)
        {
            var lightenAmount = Math.Clamp((0.45f - brightness) * 0.75f, 0.20f, 0.45f);
            color = Vector4.Lerp(color, Vector4.One, lightenAmount) with { W = 1.0f };
        }

        return color;
    }

    private static float GetPerceivedBrightness(Vector4 color)
        => (color.X * 0.299f) + (color.Y * 0.587f) + (color.Z * 0.114f);

    private static Vector4 GetWarningTextColor()
        => new(1.0f, 0.72f, 0.25f, 1.0f);

    private static void DrawHotbarSlotSelectedOverlay(Vector4 selectionColor)
    {
        var drawList = ImGui.GetWindowDrawList();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var rounding = MathF.Max(2.0f, ImGui.GetStyle().FrameRounding);
        var fillColor = ImGui.GetColorU32(selectionColor with { W = 0.18f });
        var borderColor = ImGui.GetColorU32(selectionColor);

        drawList.AddRectFilled(min, max, fillColor, rounding, ImDrawFlags.RoundCornersAll);
        drawList.AddRect(
            min + Vector2.One,
            max - Vector2.One,
            borderColor,
            rounding,
            ImDrawFlags.RoundCornersAll,
            2.0f);
    }

    private static string GetHotbarSlotTooltip(int hotbar, int slot)
    {
        if (HotbarSlotInterop.TryGetSlot(hotbar, slot, out var hotbarSlot))
        {
            var displayName = hotbarSlot.PlainTextDisplayName;
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Hotbar {hotbar}, slot {slot}"
                : $"Hotbar {hotbar}, slot {slot}: {displayName}";
        }

        return $"Hotbar {hotbar}, slot {slot}: empty";
    }
}
