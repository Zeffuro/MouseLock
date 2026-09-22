using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace MouseLock.Windows.Components;

internal sealed class WindowExceptionTable
{
    private string _filter = string.Empty;

    internal void Draw(IReadOnlyList<WindowExceptionEntry> entries)
    {
        if (entries.Count == 0)
        {
            ImGui.TextDisabled("No exceptions added.");
            return;
        }

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##Filter", "Filter exceptions", ref _filter, 128);
        using var table = ImRaii.Table("Exceptions", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY,
            new Vector2(0, ImGui.GetFrameHeightWithSpacing() * Math.Min(entries.Count + 1, 7)));
        if (!table) return;

        ImGui.TableSetupColumn("Window", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rule", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Window system").X);
        ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("Remove").X + ImGui.GetStyle().FramePadding.X * 2);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var anyVisible = false;
        foreach (var entry in entries)
        {
            if (!entry.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase) &&
                !entry.Rule.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;

            anyVisible = true;
            using var id = ImRaii.PushId($"{entry.Rule}/{entry.Name}");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextWrapped(entry.Name);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(entry.Rule);
            ImGui.TableNextColumn();
            if (ImGui.Button("Remove"))
            {
                entry.Remove();
                return;
            }
        }

        if (!anyVisible)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled("No matching exceptions.");
        }
    }
}

internal readonly record struct WindowExceptionEntry(string Name, string Rule, Action Remove);
