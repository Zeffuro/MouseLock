using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private static void DrawSection(string title)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted(title);
    }

    private static bool DrawNestedCheckbox(string label, ref bool value, int depth = 1)
    {
        DrawNestIndicator(depth);
        return ImGui.Checkbox(label, ref value);
    }

    private static void DrawTooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }

    private static void DrawDisabled(bool disabled, Action draw)
    {
        using var disabledScope = ImRaii.Disabled(disabled);
        draw();
    }

    private static void DrawNestIndicator(int depth)
    {
        var frameHeight = ImGui.GetFrameHeight();
        var width = frameHeight * Math.Max(depth, 1);
        var start = ImGui.GetCursorScreenPos();
        var midpointY = start.Y + (frameHeight * 0.5f);
        var verticalX = start.X + width - (frameHeight * 0.5f);
        var color = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddLine(
            new Vector2(verticalX, start.Y + 2.0f),
            new Vector2(verticalX, midpointY),
            color);
        drawList.AddLine(
            new Vector2(verticalX, midpointY),
            new Vector2(start.X + width, midpointY),
            color);

        ImGui.Dummy(new Vector2(width + ImGui.GetStyle().ItemInnerSpacing.X, frameHeight));
        ImGui.SameLine();
    }
}
