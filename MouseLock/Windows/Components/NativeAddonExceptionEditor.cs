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
        var hasFocusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var name);
        ImGui.TextWrapped($"Focused game window: {ConfigWindow.DisplayAddonName(name)}");
        var alreadyAllowed = conditions.IsFocusedAddonIgnored(name) && conditions.IsHoveredAddonIgnored(name);
        using (ImRaii.Disabled(!hasFocusedAddon || alreadyAllowed))
        {
            if (ImGui.Button(alreadyAllowed ? "Already allowed" : "Add exception"))
                AddAllowedAddonName(conditions, name);
        }
        ConfigWindow.DrawTooltip("Keep MouseLock active when this game window is focused or hovered.");
        ImGui.Spacing();

        _table.Draw(names.Select(addon => new WindowExceptionEntry(addon, GetRule(conditions, addon),
            () => RemoveAllowedAddonName(conditions, addon))).ToList());
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
