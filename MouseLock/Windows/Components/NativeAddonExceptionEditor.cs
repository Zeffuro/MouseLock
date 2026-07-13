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
    public void Draw(MouseLookConditionSettings conditions)
    {
        var hasFocusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName);
        var currentAddonName = hasFocusedAddon
            ? focusedAddonName
            : string.Empty;
        var canUseExceptions = conditions.DisableWhenNativeAddonFocused || conditions.DisableWhenNativeAddonHovered;

        ConfigWindow.DrawSection("Native window exceptions");
        using (ImRaii.Disabled(!canUseExceptions))
        {
            ImGui.TextDisabled("Allow specific game windows to keep MouseLock active.");
            ImGui.TextUnformatted($"Current game window: {ConfigWindow.DisplayAddonName(currentAddonName)}");

            if (!string.IsNullOrEmpty(currentAddonName))
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Allow this window"))
                {
                    AddAllowedAddonName(conditions, currentAddonName);
                }
            }

            DrawAllowedAddonNameList(conditions);
        }

        if (!canUseExceptions)
        {
            ImGui.TextDisabled("Enable a native-window pause option above to use exceptions.");
        }
    }

    private void AddAllowedAddonName(MouseLookConditionSettings conditions, string addonName)
    {
        var changed = AddAddonName(conditions.IgnoredFocusedAddonNames, addonName);
        changed |= AddAddonName(conditions.IgnoredHoveredAddonNames, addonName);

        if (changed)
        {
            save();
        }
    }

    private static bool AddAddonName(List<string> addonNames, string addonName)
    {
        if (addonNames.Any(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        addonNames.Add(addonName);
        addonNames.Sort(System.StringComparer.OrdinalIgnoreCase);
        return true;
    }

    private void DrawAllowedAddonNameList(MouseLookConditionSettings conditions)
    {
        var addonNames = conditions.IgnoredFocusedAddonNames
            .Concat(conditions.IgnoredHoveredAddonNames)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(addonName => addonName, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        ImGui.TextUnformatted("Allowed windows");
        if (addonNames.Count == 0)
        {
            ImGui.TextDisabled("None");
            return;
        }

        foreach (var addonName in addonNames)
        {
            ImGui.BulletText(addonName);
            ImGui.SameLine();

            if (ImGui.SmallButton($"Remove##AllowedNativeAddon{addonName}"))
            {
                RemoveAllowedAddonName(conditions, addonName);
                return;
            }
        }
    }

    private void RemoveAllowedAddonName(MouseLookConditionSettings conditions, string addonName)
    {
        conditions.IgnoredFocusedAddonNames.RemoveAll(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase));
        conditions.IgnoredHoveredAddonNames.RemoveAll(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase));
        save();
    }
}
