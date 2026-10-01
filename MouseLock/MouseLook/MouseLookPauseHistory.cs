using System;
using System.Collections.Generic;
using MouseLock.MouseLook.Activation;

namespace MouseLock.MouseLook;

internal sealed class MouseLookPauseHistory
{
    private const int Capacity = 8;
    private readonly List<MouseLookPauseSnapshot> _entries = [];
    private MouseLookDecision _previousDecision = MouseLookDecision.Allow();
    private bool _configWasOpen;

    public IReadOnlyList<MouseLookPauseSnapshot> Entries => _entries;
    public MouseLookPauseSnapshot? LastPauseBeforeConfig { get; private set; }

    public void Observe(MouseLookDecision decision, bool configIsOpen)
    {
        if (configIsOpen && !_configWasOpen)
        {
            LastPauseBeforeConfig = _entries.Count > 0 ? _entries[^1] : null;
        }
        _configWasOpen = configIsOpen;

        var changed = decision != _previousDecision;
        _previousDecision = decision;
        if (!changed || decision.ShouldLock || decision.Reason is
            MouseLookPauseReason.ConfigWindowOpen or MouseLookPauseReason.PluginDisabled or MouseLookPauseReason.LoggedOut)
        {
            return;
        }

        if (_entries.Count == Capacity)
        {
            _entries.RemoveAt(0);
        }
        _entries.Add(new MouseLookPauseSnapshot(DateTimeOffset.UtcNow, decision.Reason, decision.WindowName));
    }
}

internal readonly record struct MouseLookPauseSnapshot(DateTimeOffset Timestamp, MouseLookPauseReason Reason, string WindowName)
{
    public string Description => string.IsNullOrEmpty(WindowName)
        ? MouseLookStatusFormatter.GetReasonLabel(Reason)
        : $"{MouseLookStatusFormatter.GetReasonLabel(Reason)}: {WindowName}";
}
