using System.Collections.Generic;
using System.Linq;

namespace MouseLock.Configuration;

public sealed class MouseLookConditionSettings
{
    private static readonly string[] DefaultIgnoredDalamudWindowNames =
    [
        "Chat 2###chat2", // Allows the mouse to lock again after typing in Chat 2
    ];

    private List<string> _ignoredFocusedAddonNames = [];
    private List<string> _ignoredHoveredAddonNames = [];
    private List<string> _ignoredDalamudWindowSystemNamespaces = [];
    private List<string> _ignoredDalamudWindowNames = [.. DefaultIgnoredDalamudWindowNames];

    public bool DisableWhileTextInputActive { get; set; } = true;
    public bool DisableWhenTalkAddonVisible { get; set; } = true;
    public bool DisableWhenDalamudWindowFocused { get; set; } = true;
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
        set => _ignoredFocusedAddonNames = NormalizeNames(value);
    }

    public List<string> IgnoredHoveredAddonNames
    {
        get => _ignoredHoveredAddonNames;
        set => _ignoredHoveredAddonNames = NormalizeNames(value);
    }

    public List<string> IgnoredDalamudWindowSystemNamespaces
    {
        get => _ignoredDalamudWindowSystemNamespaces;
        set => _ignoredDalamudWindowSystemNamespaces = NormalizeNames(value);
    }

    public List<string> IgnoredDalamudWindowNames
    {
        get => _ignoredDalamudWindowNames;
        set => _ignoredDalamudWindowNames = NormalizeNames(value);
    }

    public void EnsureInitialized()
    {
        _ignoredFocusedAddonNames = NormalizeNames(_ignoredFocusedAddonNames);
        _ignoredHoveredAddonNames = NormalizeNames(_ignoredHoveredAddonNames);
        _ignoredDalamudWindowSystemNamespaces = NormalizeNames(_ignoredDalamudWindowSystemNamespaces);
        _ignoredDalamudWindowNames = NormalizeNames(_ignoredDalamudWindowNames);
    }

    public bool IsFocusedAddonIgnored(string addonName)
        => ContainsName(_ignoredFocusedAddonNames, addonName);

    public bool IsHoveredAddonIgnored(string addonName)
        => ContainsName(_ignoredHoveredAddonNames, addonName);

    public bool IsDalamudWindowSystemIgnored(string windowSystemNamespace)
        => ContainsName(_ignoredDalamudWindowSystemNamespaces, windowSystemNamespace);

    public bool IsDalamudWindowIgnored(string windowName)
        => ContainsNamePattern(_ignoredDalamudWindowNames, windowName);

    private static bool ContainsName(IEnumerable<string> names, string name)
        => !string.IsNullOrWhiteSpace(name) &&
           names.Any(existing => string.Equals(existing, name, System.StringComparison.OrdinalIgnoreCase));

    private static bool ContainsNamePattern(IEnumerable<string> patterns, string name)
        => !string.IsNullOrWhiteSpace(name) &&
           patterns.Any(pattern => IsNamePatternMatch(pattern, name));

    private static bool IsNamePatternMatch(string pattern, string name)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return pattern.EndsWith('*')
            ? name.StartsWith(pattern[..^1], System.StringComparison.OrdinalIgnoreCase)
            : string.Equals(pattern, name, System.StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> NormalizeNames(IEnumerable<string>? names)
        => names?
               .Select(name => name.Trim())
               .Where(name => !string.IsNullOrWhiteSpace(name))
               .Distinct(System.StringComparer.OrdinalIgnoreCase)
               .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
               .ToList()
           ?? [];
}
