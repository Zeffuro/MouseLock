using System;
using System.Diagnostics;

namespace MouseLock.MouseLook.Diagnostics;

internal sealed class InputTraceBuffer(int capacity = 40000)
{
    private InputTraceSample[]? _samples;
    private long _startedTicks;
    private long _durationTicks;

    internal bool IsRecording { get; private set; }
    internal int Count { get; private set; }
    internal string StopReason { get; private set; } = "Not started";

    internal void Start(long timestamp, long durationTicks)
    {
        if (IsRecording) throw new InvalidOperationException("A recording is already running.");
        if (capacity <= 0 || durationTicks <= 0) throw new ArgumentOutOfRangeException(nameof(durationTicks));
        _samples = new InputTraceSample[capacity];
        _startedTicks = timestamp;
        _durationTicks = durationTicks;
        Count = 0;
        StopReason = string.Empty;
        IsRecording = true;
    }

    internal double ElapsedMilliseconds(long timestamp)
        => (timestamp - _startedTicks) * 1000.0 / Stopwatch.Frequency;

    internal void Tick(long timestamp)
    {
        if (IsRecording && timestamp - _startedTicks >= _durationTicks) Stop("Duration reached");
    }

    internal void Add(InputTraceSample sample, long timestamp)
    {
        Tick(timestamp);
        if (!IsRecording) return;
        _samples![Count++] = sample;
        if (Count == capacity) Stop("Buffer full");
    }

    internal void Stop(string reason)
    {
        if (!IsRecording) return;
        IsRecording = false;
        StopReason = reason;
    }

    internal InputTraceSample[] GetSamples()
    {
        if (IsRecording) throw new InvalidOperationException("Stop the recording before exporting.");
        return _samples is null ? [] : _samples.AsSpan(0, Count).ToArray();
    }
}

internal readonly record struct InputTraceSample(
    double TimeMs,
    long FrameworkTick,
    long InputCall,
    string Phase,
    double FrameworkDeltaMs,
    bool Enabled,
    bool ShouldLock,
    int PauseReason,
    bool Focused,
    int CursorX,
    int CursorY,
    int DeltaX,
    int DeltaY,
    int FilteredDeltaX,
    int FilteredDeltaY,
    uint ButtonsHeld,
    bool DragActive,
    float DragDeltaX,
    float DragDeltaY,
    float CameraYaw,
    float CameraPitch,
    long MoveId,
    int MoveX,
    int MoveY,
    int CameraSource);
