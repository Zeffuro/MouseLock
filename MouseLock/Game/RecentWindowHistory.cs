using System.Collections.Generic;

namespace MouseLock.Game;

internal sealed class RecentWindowHistory<T>(IEqualityComparer<T>? comparer = null)
{
    public const int Capacity = 10;
    private readonly List<T> _entries = [];
    private readonly IEqualityComparer<T> _comparer = comparer ?? EqualityComparer<T>.Default;

    public IReadOnlyList<T> Entries => _entries;

    public void Record(T window)
    {
        var existingIndex = _entries.FindIndex(entry => _comparer.Equals(entry, window));
        if (existingIndex == 0) return;
        if (existingIndex > 0) _entries.RemoveAt(existingIndex);
        _entries.Insert(0, window);
        if (_entries.Count > Capacity) _entries.RemoveAt(Capacity);
    }

    public void Clear() => _entries.Clear();
}
