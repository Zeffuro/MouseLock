using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Hotbars;

namespace MouseLock.Windows.Components;

internal sealed class HotbarSlotPicker(Action save)
{
    private const int PulseIntervalMilliseconds = 400;
    private const int PulseStatusDurationMilliseconds = 3_000;
    private const float SlotValueWidthInFrames = 3.0f;
    private const float PickerSizeInFrames = 1.10f;
    private const float PickerSpacing = 2.0f;

    private readonly Stopwatch _pulseTimer = new();
    private readonly Stopwatch _statusTimer = new();
    private string? _status;

    public void Draw(MouseButtonGameInputBinding binding)
    {
        DrawWrappedNumberInput(
            "Hotbar",
            binding.Hotbar,
            1,
            10,
            value => binding.Hotbar = value,
            () => DrawHighlightButton(binding));

        DrawWrappedNumberInput(
            "Slot",
            binding.Slot,
            1,
            12,
            value => binding.Slot = value);

        DrawSlotPicker(binding);
        DrawPulseStatus();
    }

    private void DrawWrappedNumberInput(
        string label,
        int value,
        int min,
        int max,
        Action<int> setValue,
        Action? drawTrailingAction = null)
    {
        ConfigWindow.DrawNestIndicator(1);
        using var id = ImRaii.PushId(label);

        var style = ImGui.GetStyle();
        var rowStartX = ImGui.GetCursorPosX();
        var labelWidth = GetControlLabelWidth();
        var frameHeight = ImGui.GetFrameHeight();
        var arrowWidth = frameHeight;
        var valueWidth = frameHeight * SlotValueWidthInFrames;
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
            save();
        }

        SameLineAt(valueX);
        var inputValue = value;
        ImGui.SetNextItemWidth(valueWidth);
        if (ImGui.InputInt("##Value", ref inputValue, 0, 0))
        {
            setValue(Math.Clamp(inputValue, min, max));
            save();
        }

        SameLineAt(nextButtonX);
        if (ImGui.ArrowButton("##Next", ImGuiDir.Right))
        {
            setValue(WrapValue(value, min, max, 1));
            save();
        }

        if (drawTrailingAction is not null)
        {
            SameLineAt(trailingActionX);
            drawTrailingAction();
        }
    }

    private void DrawHighlightButton(MouseButtonGameInputBinding binding)
    {
        if (ImGui.Button("Highlight"))
        {
            SetPulseStatus(
                HotbarSlotInterop.TryPulseVisibleSlot(binding.Hotbar, binding.Slot),
                binding.Hotbar);
        }

        if (ImGui.IsItemHovered())
        {
            PulseSlotOnHover(binding.Hotbar, binding.Slot);
            ConfigWindow.DrawTooltip("Pulse the selected slot on your visible in-game hotbar.");
        }
    }

    private void DrawSlotPicker(MouseButtonGameInputBinding binding)
    {
        ConfigWindow.DrawNestIndicator(1);
        using var group = ImRaii.Group();

        var size = new Vector2(ImGui.GetFrameHeight() * PickerSizeInFrames);
        var spacing = PickerSpacing;
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

            DrawSlotPickerButton(binding, slot, size);
        }
    }

    private void DrawSlotPickerButton(MouseButtonGameInputBinding binding, int slot, Vector2 size)
    {
        using var id = ImRaii.PushId(slot);

        var selected = binding.Slot == slot;
        var selectionColor = GetSelectionColor();
        using var buttonColor = ImRaii.PushColor(ImGuiCol.Button, selectionColor with { W = 0.28f }, selected);
        using var hoveredColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, selectionColor with { W = 0.38f }, selected);
        using var activeColor = ImRaii.PushColor(ImGuiCol.ButtonActive, selectionColor with { W = 0.48f }, selected);

        var clicked = DrawSlotButtonContent(binding.Hotbar, slot, size);

        if (selected)
        {
            DrawSelectedOverlay(selectionColor);
        }

        if (clicked)
        {
            binding.Slot = slot;
            save();
        }

        if (ImGui.IsItemHovered())
        {
            PulseSlotOnHover(binding.Hotbar, slot);
            ConfigWindow.DrawTooltip(GetTooltip(binding.Hotbar, slot));
        }
    }

    private static bool DrawSlotButtonContent(int hotbar, int slot, Vector2 size)
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

    private void PulseSlotOnHover(int hotbar, int slot)
    {
        if (_pulseTimer.IsRunning &&
            _pulseTimer.ElapsedMilliseconds < PulseIntervalMilliseconds)
        {
            return;
        }

        HotbarSlotInterop.TryPulseVisibleSlot(hotbar, slot);
        _pulseTimer.Restart();
    }

    private void SetPulseStatus(bool success, int hotbar)
    {
        if (success)
        {
            _status = null;
            _statusTimer.Reset();
            return;
        }

        _status = $"Hotbar {hotbar} is not currently visible.";
        _statusTimer.Restart();
    }

    private void DrawPulseStatus()
    {
        if (_status is null ||
            !_statusTimer.IsRunning ||
            _statusTimer.ElapsedMilliseconds >= PulseStatusDurationMilliseconds)
        {
            return;
        }

        ConfigWindow.DrawNestIndicator(1);
        ImGui.TextDisabled(_status);
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

    private static float GetControlLabelWidth()
        => MathF.Max(ImGui.CalcTextSize("Hotbar").X, ImGui.CalcTextSize("Slot").X);

    private static Vector4 GetSelectionColor()
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

    private static void DrawSelectedOverlay(Vector4 selectionColor)
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

    private static string GetTooltip(int hotbar, int slot)
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
