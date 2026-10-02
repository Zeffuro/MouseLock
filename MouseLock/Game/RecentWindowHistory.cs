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
        for (var index = 0; index < _entries.Count; index++)
        {
            if (!_comparer.Equals(_entries[index], window)) continue;
            if (index == 0) return;
            _entries.RemoveAt(index);
            break;
        }
        _entries.Insert(0, window);
        if (_entries.Count > Capacity) _entries.RemoveAt(Capacity);
    }

    public void Clear() => _entries.Clear();
}
