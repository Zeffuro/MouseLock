using System.Collections.Generic;
using System.Linq;

namespace MouseLock.Configuration;

public sealed class MouseLookConditionSettings
{
    private List<string> _ignoredFocusedAddonNames = [];
    private List<string> _ignoredHoveredAddonNames = [];

    public bool DisableWhileTextInputActive { get; set; } = true;
    public bool DisableWhenTalkAddonVisible { get; set; } = true;
    public bool DisableWhenNativeAddonFocused { get; set; } = true;
    public bool DisableWhenNativeAddonHovered { get; set; }
    public bool RequireCombat { get; set; }
    public bool CountCountdownAsCombat { get; set; } = true;
    public bool DisableDuringCutscenes { get; set; } = true;
    public bool DisableDuringGpose { get; set; } = true;
    public bool DisableDuringCrafting { get; set; }
    public bool DisableDuringGathering { get; set; }
    public bool DisableDuringGroundTargeting { get; set; } = true;
    public bool DisableDuringHousingPlacement { get; set; }
    public bool DisableWhileMounted { get; set; }
    public bool DisableDuringTerritoryTransitions { get; set; } = true;
    public bool DisableDuringGamepadMouseMode { get; set; }

    public List<string> IgnoredFocusedAddonNames
    {
        get => _ignoredFocusedAddonNames;
        set => _ignoredFocusedAddonNames = NormalizeAddonNames(value);
    }

    public List<string> IgnoredHoveredAddonNames
    {
        get => _ignoredHoveredAddonNames;
        set => _ignoredHoveredAddonNames = NormalizeAddonNames(value);
    }

    public void EnsureInitialized()
    {
        _ignoredFocusedAddonNames = NormalizeAddonNames(_ignoredFocusedAddonNames);
        _ignoredHoveredAddonNames = NormalizeAddonNames(_ignoredHoveredAddonNames);
    }

    public bool IsFocusedAddonIgnored(string addonName)
        => ContainsAddonName(_ignoredFocusedAddonNames, addonName);

    public bool IsHoveredAddonIgnored(string addonName)
        => ContainsAddonName(_ignoredHoveredAddonNames, addonName);

    private static bool ContainsAddonName(IEnumerable<string> addonNames, string addonName)
        => !string.IsNullOrWhiteSpace(addonName) &&
           addonNames.Any(existing => string.Equals(existing, addonName, System.StringComparison.OrdinalIgnoreCase));

    private static List<string> NormalizeAddonNames(IEnumerable<string>? addonNames)
        => addonNames?
               .Select(addonName => addonName.Trim())
               .Where(addonName => !string.IsNullOrWhiteSpace(addonName))
               .Distinct(System.StringComparer.OrdinalIgnoreCase)
               .OrderBy(addonName => addonName, System.StringComparer.OrdinalIgnoreCase)
               .ToList()
           ?? [];
}
