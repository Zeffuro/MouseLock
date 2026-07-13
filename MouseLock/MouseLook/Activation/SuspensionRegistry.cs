using System;
using System.Collections.Generic;
using System.Linq;

namespace MouseLock.MouseLook.Activation;

internal static class SuspensionRegistry
{
    private const string DefaultSource = "External";

    private static readonly object SyncRoot = new();
    private static readonly HashSet<string> Sources = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsSuspended
    {
        get
        {
            lock (SyncRoot)
            {
                return Sources.Count > 0;
            }
        }
    }

    public static string SourcesSummary
    {
        get
        {
            lock (SyncRoot)
            {
                return Sources.Count == 0
                    ? string.Empty
                    : string.Join(", ", Sources.OrderBy(source => source, StringComparer.OrdinalIgnoreCase));
            }
        }
    }

    public static bool IsSuspendedBy(string source)
    {
        var normalizedSource = NormalizeSource(source);
        lock (SyncRoot)
        {
            return Sources.Contains(normalizedSource);
        }
    }

    public static bool SetSuspended(string source, bool suspended)
    {
        var normalizedSource = NormalizeSource(source);
        lock (SyncRoot)
        {
            if (suspended)
            {
                Sources.Add(normalizedSource);
            }
            else
            {
                Sources.Remove(normalizedSource);
            }

            return Sources.Count > 0;
        }
    }

    public static int Clear()
    {
        lock (SyncRoot)
        {
            var count = Sources.Count;
            Sources.Clear();
            return count;
        }
    }

    private static string NormalizeSource(string source)
        => string.IsNullOrWhiteSpace(source) ? DefaultSource : source.Trim();
}
