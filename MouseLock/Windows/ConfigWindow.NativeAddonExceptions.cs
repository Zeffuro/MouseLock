using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using MouseLock.Game;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private unsafe void DrawNativeAddonExceptionSettings()
    {
        var conditions = _config.General.Conditions;
        var hasFocusedAddon = NativeUiState.TryGetFocusedBlockingAddonName(out var focusedAddonName);
        var currentAddonName = hasFocusedAddon
            ? focusedAddonName
            : string.Empty;
        var canUseExceptions = conditions.DisableWhenNativeAddonFocused || conditions.DisableWhenNativeAddonHovered;

        DrawSection("Native window exceptions");
        DrawDisabled(!canUseExceptions, () =>
        {
            ImGui.TextDisabled("Allow specific game windows to keep MouseLock active.");
            ImGui.TextUnformatted($"Current game window: {DisplayAddonName(currentAddonName)}");

            if (!string.IsNullOrEmpty(currentAddonName))
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Allow this window"))
                {
                    AddAllowedAddonName(currentAddonName);
                }
            }

            DrawAllowedAddonNameList();
        });

        if (!canUseExceptions)
        {
            ImGui.TextDisabled("Enable a native-window pause option above to use exceptions.");
        }
    }

    private void AddAllowedAddonName(string addonName)
    {
        var conditions = _config.General.Conditions;
        var changed = AddAddonName(conditions.IgnoredFocusedAddonNames, addonName);
        changed |= AddAddonName(conditions.IgnoredHoveredAddonNames, addonName);

        if (changed)
        {
            Save();
        }
    }

    private bool AddAddonName(List<string> addonNames, string addonName)
    {
        if (addonNames.Any(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        addonNames.Add(addonName);
        addonNames.Sort(System.StringComparer.OrdinalIgnoreCase);
        return true;
    }

    private void DrawAllowedAddonNameList()
    {
        var conditions = _config.General.Conditions;
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

        for (var index = 0; index < addonNames.Count; index++)
        {
            var addonName = addonNames[index];
            ImGui.BulletText(addonName);
            ImGui.SameLine();

            if (ImGui.SmallButton($"Remove##AllowedNativeAddon{addonName}"))
            {
                RemoveAllowedAddonName(addonName);
                return;
            }
        }
    }

    private void RemoveAllowedAddonName(string addonName)
    {
        var conditions = _config.General.Conditions;
        conditions.IgnoredFocusedAddonNames.RemoveAll(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase));
        conditions.IgnoredHoveredAddonNames.RemoveAll(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private static string DisplayAddonName(string addonName)
        => string.IsNullOrEmpty(addonName) ? "None" : addonName;
}
