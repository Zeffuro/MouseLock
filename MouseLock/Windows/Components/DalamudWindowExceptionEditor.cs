using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Game;

namespace MouseLock.Windows.Components;

internal sealed class DalamudWindowExceptionEditor(Action save)
{
    private readonly WindowExceptionTable _table = new();

    public void Draw(MouseLookConditionSettings conditions)
    {
        using var id = ImRaii.PushId("DalamudExceptions");
        var count = conditions.IgnoredDalamudWindowNames.Count + conditions.IgnoredDalamudWindowSystemNamespaces.Count;
        if (!ImGui.CollapsingHeader($"Dalamud windows ({count})###Header", ImGuiTreeNodeFlags.DefaultOpen)) return;

        if (!conditions.DisableWhenDalamudWindowFocused)
            ImGui.TextWrapped("Enable the Dalamud window pause option above to use these exceptions.");

        using var disabled = ImRaii.Disabled(!conditions.DisableWhenDalamudWindowFocused);
        var focus = DalamudUiState.LastExternalFocus;
        ImGui.TextWrapped($"Last focused: {ConfigWindow.DisplayAddonName(focus.WindowName)}");
        ConfigWindow.DrawTooltip($"Window system: {ConfigWindow.DisplayAddonName(focus.WindowSystemNamespace)}");
        using (ImRaii.Disabled(focus.IsEmpty))
        {
            if (ImGui.Button("Add exception"))
                ImGui.OpenPopup("AddException");
        }
        ConfigWindow.DrawTooltip("Focus another plugin window, then return here to allow it.");
        DrawAddMenu(conditions, focus);
        ImGui.Spacing();

        var entries = conditions.IgnoredDalamudWindowNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => CreateEntry(conditions.IgnoredDalamudWindowNames, name,
                name.EndsWith('*') ? "Window family" : "Exact window"))
            .Concat(conditions.IgnoredDalamudWindowSystemNamespaces
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => CreateEntry(conditions.IgnoredDalamudWindowSystemNamespaces, name, "Window system")))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Rule)
            .ToList();
        _table.Draw(entries);
    }

    private void DrawAddMenu(MouseLookConditionSettings conditions, DalamudWindowFocus focus)
    {
        using var popup = ImRaii.Popup("AddException");
        if (!popup) return;

        DrawAddOption("This window", conditions.IgnoredDalamudWindowNames, focus.WindowName);
        DrawAddOption("Window family", conditions.IgnoredDalamudWindowNames, GetSuggestedWindowPattern(focus.WindowName));
        DrawAddOption("Entire window system", conditions.IgnoredDalamudWindowSystemNamespaces, focus.WindowSystemNamespace);
    }

    private void DrawAddOption(string label, List<string> names, string name)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        var alreadyAllowed = names.Contains(normalizedName, StringComparer.OrdinalIgnoreCase);
        using var disabled = ImRaii.Disabled(normalizedName.Length == 0 || alreadyAllowed);
        if (ImGui.Selectable(alreadyAllowed ? $"{label} (already allowed)" : label))
        {
            names.Add(normalizedName);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            save();
        }
        ConfigWindow.DrawTooltip(string.IsNullOrEmpty(name) ? "Not available for this window." : name);
    }

    private WindowExceptionEntry CreateEntry(List<string> names, string name, string rule)
        => new(name, rule, () =>
        {
            names.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
            save();
        });

    private static string GetSuggestedWindowPattern(string windowName)
    {
        if (string.IsNullOrWhiteSpace(windowName)) return string.Empty;

        var slashIndex = windowName.IndexOf('/');
        return slashIndex <= 0 ? string.Empty : $"{windowName[..(slashIndex + 1)]}*";
    }
}
