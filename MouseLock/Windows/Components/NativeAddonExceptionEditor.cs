using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Game;

namespace MouseLock.Windows.Components;

internal sealed class NativeAddonExceptionEditor(Action save)
{
    private readonly WindowExceptionTable _table = new();
    private string _manualName = string.Empty;

    public void Draw(MouseLookConditionSettings conditions)
    {
        using var id = ImRaii.PushId("NativeExceptions");
        var names = conditions.IgnoredFocusedAddonNames
            .Concat(conditions.IgnoredHoveredAddonNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!ImGui.CollapsingHeader($"Game windows ({names.Count})###Header", ImGuiTreeNodeFlags.DefaultOpen)) return;

        var canUseExceptions = conditions.DisableWhenNativeAddonFocused || conditions.DisableWhenNativeAddonHovered;
        if (!canUseExceptions)
            ImGui.TextWrapped("Enable a game window pause option above to use these exceptions.");

        using var disabled = ImRaii.Disabled(!canUseExceptions);
        var isFocused = NativeUiState.TryGetFocusedBlockingAddonName(out var name);
        if (!isFocused) name = NativeUiState.LastFocusedAddonName;
        ImGui.TextWrapped($"{(isFocused ? "Focused" : "Last focused")}: {ConfigWindow.DisplayAddonName(name)}");
        ConfigWindow.DrawTooltip("Remembers the last game window you focused, even after it closes.");
        if (ImGui.Button("Add exception..."))
        {
            ImGui.OpenPopup("AddException");
        }
        ConfigWindow.DrawTooltip("Allow this window or enter a window name. Other pause settings still apply.");
        DrawAddMenu(conditions, name);
        RecentWindowTable.Draw(NativeUiState.RecentFocusedAddonNames.Select(addon => new RecentWindowEntry(
            addon, "Add an exception for this game window.",
            conditions.IsFocusedAddonIgnored(addon) && conditions.IsHoveredAddonIgnored(addon), "Add",
            () => AddAllowedAddonName(conditions, addon))).ToList());
        ImGui.Spacing();
        ImGui.Separator();

        _table.Draw(names.Select(addon => new WindowExceptionEntry(addon, GetRule(conditions, addon),
            () => RemoveAllowedAddonName(conditions, addon))).ToList());
    }

    private void DrawAddMenu(MouseLookConditionSettings conditions, string name)
    {
        using var popup = ImRaii.Popup("AddException");
        if (!popup) return;

        ImGui.TextWrapped($"Game window: {ConfigWindow.DisplayAddonName(name)}");
        var alreadyAllowed = conditions.IsFocusedAddonIgnored(name) && conditions.IsHoveredAddonIgnored(name);
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(name) || alreadyAllowed))
        {
            if (ImGui.Selectable(alreadyAllowed ? "This window (already allowed)" : "This window"))
                AddAllowedAddonName(conditions, name);
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Add by name");
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 22);
        ImGui.InputTextWithHint("##AddonName", "Game window name (e.g. Character)", ref _manualName, 128);
        ConfigWindow.DrawTooltip("Use the game's window name, such as FateReward or RetainerList.");
        var manualName = _manualName.Trim();
        var manualAlreadyAllowed = conditions.IsFocusedAddonIgnored(manualName) && conditions.IsHoveredAddonIgnored(manualName);
        using (ImRaii.Disabled(manualName.Length == 0 || manualAlreadyAllowed))
        {
            if (ImGui.Button(manualAlreadyAllowed ? "Already allowed" : "Add"))
            {
                AddAllowedAddonName(conditions, manualName);
                _manualName = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static string GetRule(MouseLookConditionSettings conditions, string name)
        => conditions.IsFocusedAddonIgnored(name)
            ? conditions.IsHoveredAddonIgnored(name) ? "Focus + hover" : "Focus"
            : "Hover";

    private void AddAllowedAddonName(MouseLookConditionSettings conditions, string addonName)
    {
        var changed = AddAddonName(conditions.IgnoredFocusedAddonNames, addonName);
        changed |= AddAddonName(conditions.IgnoredHoveredAddonNames, addonName);
        if (changed) save();
    }

    private static bool AddAddonName(List<string> names, string name)
    {
        if (names.Contains(name, StringComparer.OrdinalIgnoreCase)) return false;

        names.Add(name);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return true;
    }

    private void RemoveAllowedAddonName(MouseLookConditionSettings conditions, string addonName)
    {
        conditions.IgnoredFocusedAddonNames.RemoveAll(existing => string.Equals(existing, addonName, StringComparison.OrdinalIgnoreCase));
        conditions.IgnoredHoveredAddonNames.RemoveAll(existing => string.Equals(existing, addonName, StringComparison.OrdinalIgnoreCase));
        save();
    }
}
