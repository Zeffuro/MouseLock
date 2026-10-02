using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using MouseLock.MouseLook.Activation;

namespace MouseLock.MouseLook.Diagnostics;

internal sealed unsafe class MouseInputTrace
{
    private readonly InputTraceBuffer _buffer = new();
    private long _frameworkTick;
    private long _inputCall;
    private long _moveId;
    private double _frameworkDeltaMs;
    private bool _completionReported;
    private string _label = "input";
    private DateTimeOffset _startedAt;
    private MouseLookDecision _decision;
    private string _captureError = string.Empty;

    internal bool IsRecording => _buffer.IsRecording;
    internal string Summary => IsRecording
        ? $"Recording: {_buffer.Count} samples"
        : _buffer.Count == 0 && string.IsNullOrEmpty(_captureError)
            ? "No recording"
            : $"{_buffer.Count} samples — {_buffer.StopReason}. {_captureError}".Trim();

    internal string Start(string label)
    {
        if (IsRecording) return "Recording already running.";
        _label = label.Length > 80 ? label[..80] : label;
        _startedAt = DateTimeOffset.UtcNow;
        _frameworkTick = 0;
        _inputCall = 0;
        _moveId = 0;
        _frameworkDeltaMs = 0;
        _completionReported = false;
        _captureError = string.Empty;
        _buffer.Start(Stopwatch.GetTimestamp(), Stopwatch.Frequency * 10);
        return "Recording for 10s. Close chat/config and move the mouse.";
    }

    internal string Stop()
    {
        _buffer.Stop("Stopped manually");
        _completionReported = true;
        return Summary;
    }

    internal void AdvanceFramework(double deltaMs)
    {
        if (IsRecording)
        {
            _frameworkTick++;
            _frameworkDeltaMs = deltaMs;
            _buffer.Tick(Stopwatch.GetTimestamp());
        }
        if (!_completionReported && !IsRecording && _buffer.Count > 0)
        {
            _completionReported = true;
            try
            {
                Service.ChatGui.Print("MouseLock: recording finished. /mouselock trace save");
            }
            catch (Exception ex)
            {
                _captureError = ex.Message;
            }
        }
    }

    internal void BeginInput(UIInputData* inputData)
    {
        if (!IsRecording) return;
        _inputCall++;
        Record("InputEnter", inputData);
    }

    internal void SetDecision(MouseLookDecision decision) => _decision = decision;

    internal long NextMoveId() => IsRecording ? ++_moveId : 0;

    internal void Record(string phase, UIInputData* inputData, long moveId = 0, int moveX = 0,
        int moveY = 0, int cameraSource = -1)
    {
        if (!IsRecording || inputData is null) return;
        try
        {
            var timestamp = Stopwatch.GetTimestamp();
            var input = &inputData->CursorInputs;
            var filtered = &inputData->UIFilteredCursorInputs;
            var manager = InputManager.Instance();
            var control = Control.Instance();
            var camera = control is null ? null : control->CameraManager.Camera;
            _buffer.Add(new InputTraceSample(
                _buffer.ElapsedMilliseconds(timestamp), _frameworkTick, _inputCall, phase, _frameworkDeltaMs,
                PluginState.Config.General.Enabled, _decision.ShouldLock, (int)_decision.Reason,
                input->IsGameWindowFocused, input->PositionX, input->PositionY, input->DeltaX, input->DeltaY,
                filtered->DeltaX, filtered->DeltaY, (uint)input->MouseButtonHeldFlags,
                manager is not null && manager->MouseDragActive,
                manager is null ? 0 : manager->MouseDragDeltaX,
                manager is null ? 0 : manager->MouseDragDeltaY,
                camera is null ? 0 : camera->DirH, camera is null ? 0 : camera->DirV,
                moveId, moveX, moveY, cameraSource), timestamp);
        }
        catch (Exception ex)
        {
            _buffer.Stop("Capture failed");
            _captureError = ex.Message;
        }
    }

    internal string Save()
    {
        if (IsRecording) return "Stop recording before saving.";
        if (_buffer.Count == 0) return "No recording to save.";
        try
        {
            var directory = Path.Combine(Service.PluginInterface.GetPluginConfigDirectory(), "input-traces");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"input-{_startedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
            var report = new
            {
                SchemaVersion = 1,
                Label = _label,
                StartedAt = _startedAt,
                PluginVersion = typeof(MouseInputTrace).Assembly.GetName().Version?.ToString(),
                _buffer.StopReason,
                CaptureError = _captureError,
                ScheduledMoveCompensationEnabled = false,
                ConfigAtExport = PluginState.Config,
                PauseReasons = Enum.GetValues<MouseLookPauseReason>().ToDictionary(reason => (int)reason,
                    reason => reason.ToString()),
                Samples = _buffer.GetSamples(),
            };
            File.WriteAllText(path, JsonSerializer.Serialize(report,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                }));
            Service.Logger.Information("Saved MouseLock input recording to {Path}.", path);
            return $"Saved: {path}";
        }
        catch (Exception ex)
        {
            Service.Logger.Error(ex, "Could not save MouseLock input recording.");
            return "Save failed. See Dalamud log; recording kept.";
        }
    }
}
