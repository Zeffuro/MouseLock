using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace MouseLock.Windows.Components;

internal static class RecentWindowTable
{
    public static void Draw(IReadOnlyList<RecentWindowEntry> entries)
    {
        var open = ImGui.CollapsingHeader($"Recent windows ({entries.Count})###RecentWindows");
        ConfigWindow.DrawTooltip("Last 10 focused windows, newest first. Cleared when you log out.");
        if (!open) return;
        if (entries.Count == 0)
        {
            ImGui.TextDisabled("No windows recorded yet.");
            return;
        }

        using var table = ImRaii.Table("RecentWindows", 2,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(0, ImGui.GetFrameHeightWithSpacing() * Math.Min(entries.Count + 1, 6)));
        if (!table) return;
        ImGui.TableSetupColumn("Window", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed,
            ImGui.CalcTextSize("Add system").X + ImGui.GetStyle().FramePadding.X * 2);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            using var id = ImRaii.PushId(index);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextWrapped(entry.Name);
            ConfigWindow.DrawTooltip(entry.Detail);
            ImGui.TableNextColumn();
            using var disabled = ImRaii.Disabled(entry.AlreadyAllowed);
            if (ImGui.Button(entry.AlreadyAllowed ? "Allowed" : entry.AddLabel))
            {
                entry.Add();
                return;
            }
        }
    }
}

internal readonly record struct RecentWindowEntry(string Name, string Detail, bool AlreadyAllowed, string AddLabel, Action Add);
