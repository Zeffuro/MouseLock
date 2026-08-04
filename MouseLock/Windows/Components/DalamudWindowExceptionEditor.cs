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
    public void Draw(MouseLookConditionSettings conditions)
    {
        var focus = DalamudUiState.LastExternalFocus;
        var suggestedWindowPattern = GetSuggestedWindowPattern(focus.WindowName);
        var canUseExceptions = conditions.DisableWhenDalamudWindowFocused;

        ConfigWindow.DrawSection("Dalamud window exceptions");
        using (ImRaii.Disabled(!canUseExceptions))
        {
            ImGui.TextDisabled("Allow specific Dalamud/ImGui windows or child-window prefixes to keep MouseLock active.");
            ImGui.TextWrapped($"Last external window system: {ConfigWindow.DisplayAddonName(focus.WindowSystemNamespace)}");
            ImGui.TextWrapped($"Last external window: {ConfigWindow.DisplayAddonName(focus.WindowName)}");

            if (!string.IsNullOrEmpty(focus.WindowName))
            {
                if (ImGui.SmallButton("Allow exact window"))
                {
                    AddAllowedWindowName(conditions, focus.WindowName);
                }

                if (!string.IsNullOrEmpty(suggestedWindowPattern))
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Allow window family"))
                    {
                        AddAllowedWindowName(conditions, suggestedWindowPattern);
                    }
                }
            }

            if (!string.IsNullOrEmpty(focus.WindowSystemNamespace))
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Allow entire window system"))
                {
                    AddAllowedWindowSystemNamespace(conditions, focus.WindowSystemNamespace);
                }
            }

            if (focus.IsEmpty)
            {
                ImGui.TextDisabled("Focus another plugin window, then return here to allow it.");
            }

            ImGui.TextDisabled("Entries ending in * match by prefix, which is useful for plugin child windows with generated names.");
            DrawAllowedWindowNameList(conditions);
            DrawAllowedWindowSystemNamespaceList(conditions);
        }

        if (!canUseExceptions)
        {
            ImGui.TextDisabled("Enable the Dalamud/ImGui window pause option above to use exceptions.");
        }
    }

    private void AddAllowedWindowSystemNamespace(MouseLookConditionSettings conditions, string windowSystemNamespace)
    {
        if (AddName(conditions.IgnoredDalamudWindowSystemNamespaces, windowSystemNamespace))
        {
            save();
        }
    }

    private void AddAllowedWindowName(MouseLookConditionSettings conditions, string windowName)
    {
        if (AddName(conditions.IgnoredDalamudWindowNames, windowName))
        {
            save();
        }
    }

    private static bool AddName(List<string> names, string name)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrEmpty(normalizedName) ||
            names.Any(existing => string.Equals(existing, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        names.Add(normalizedName);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return true;
    }

    private void DrawAllowedWindowNameList(MouseLookConditionSettings conditions)
    {
        var windowNames = conditions.IgnoredDalamudWindowNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(windowName => windowName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ImGui.TextUnformatted("Allowed Dalamud windows / prefixes");
        if (windowNames.Count == 0)
        {
            ImGui.TextDisabled("None");
            return;
        }

        foreach (var windowName in windowNames)
        {
            ImGui.BulletText(windowName);
            ImGui.SameLine();

            using var id = ImRaii.PushId($"AllowedDalamudWindow{windowName}");
            if (ImGui.SmallButton("Remove"))
            {
                RemoveAllowedWindowName(conditions, windowName);
                return;
            }
        }
    }

    private void DrawAllowedWindowSystemNamespaceList(MouseLookConditionSettings conditions)
    {
        var windowSystemNamespaces = conditions.IgnoredDalamudWindowSystemNamespaces
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(windowSystemNamespace => windowSystemNamespace, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ImGui.TextUnformatted("Allowed entire Dalamud window systems");
        if (windowSystemNamespaces.Count == 0)
        {
            ImGui.TextDisabled("None");
            return;
        }

        foreach (var windowSystemNamespace in windowSystemNamespaces)
        {
            ImGui.BulletText(windowSystemNamespace);
            ImGui.SameLine();

            using var id = ImRaii.PushId($"AllowedDalamudWindowSystem{windowSystemNamespace}");
            if (ImGui.SmallButton("Remove"))
            {
                RemoveAllowedWindowSystemNamespace(conditions, windowSystemNamespace);
                return;
            }
        }
    }

    private void RemoveAllowedWindowSystemNamespace(MouseLookConditionSettings conditions, string windowSystemNamespace)
    {
        conditions.IgnoredDalamudWindowSystemNamespaces.RemoveAll(existing => string.Equals(existing, windowSystemNamespace, StringComparison.OrdinalIgnoreCase));
        save();
    }

    private void RemoveAllowedWindowName(MouseLookConditionSettings conditions, string windowName)
    {
        conditions.IgnoredDalamudWindowNames.RemoveAll(existing => string.Equals(existing, windowName, StringComparison.OrdinalIgnoreCase));
        save();
    }

    private static string GetSuggestedWindowPattern(string windowName)
    {
        if (string.IsNullOrWhiteSpace(windowName))
        {
            return string.Empty;
        }

        var slashIndex = windowName.IndexOf('/');
        return slashIndex <= 0
            ? string.Empty
            : $"{windowName[..(slashIndex + 1)]}*";
    }
}
