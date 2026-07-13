using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.Input;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private bool _isRecordingToggleKeybind;
    private bool _skipToggleKeybindCaptureFrame;

    private void DrawToggleKeybindSettings()
    {
        var settings = _config.General.ToggleKeybind;

        var enabled = settings.Enabled;
        if (ImGui.Checkbox("Enable toggle keybind", ref enabled))
        {
            settings.Enabled = enabled;
            ConfigRepository.Save(_config);
        }

        if (!settings.Enabled)
        {
            _isRecordingToggleKeybind = false;
        }

        DrawDisabled(!settings.Enabled, () =>
        {
            DrawNestIndicator(1);
            DrawRecordedToggleKeybind(settings);

            DrawNestIndicator(1);
            var ignoreTextInput = settings.IgnoreWhileTextInputActive;
            if (ImGui.Checkbox("Ignore while text input is active", ref ignoreTextInput))
            {
                settings.IgnoreWhileTextInputActive = ignoreTextInput;
                ConfigRepository.Save(_config);
            }

            DrawNestIndicator(1);
            var showChatFeedback = settings.ShowChatFeedback;
            if (ImGui.Checkbox("Show chat feedback when used", ref showChatFeedback))
            {
                settings.ShowChatFeedback = showChatFeedback;
                ConfigRepository.Save(_config);
            }
        });
    }

    private void DrawRecordedToggleKeybind(ToggleKeybindSettings settings)
    {
        ImGui.TextUnformatted($"Toggle key: {settings.Keybind}");
        ImGui.SameLine();

        if (ImGui.Button(_isRecordingToggleKeybind ? "Recording..." : "Record"))
        {
            _isRecordingToggleKeybind = true;
            _skipToggleKeybindCaptureFrame = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            settings.SetKeybind(new RecordedKeybind());
            ConfigRepository.Save(_config);
        }

        if (_isRecordingToggleKeybind)
        {
            DrawNestIndicator(2);
            ImGui.TextDisabled("Press a key combination. Esc cancels.");

            if (_skipToggleKeybindCaptureFrame)
            {
                _skipToggleKeybindCaptureFrame = false;
            }
            else if (Service.KeyState[VirtualKey.ESCAPE])
            {
                _isRecordingToggleKeybind = false;
            }
            else if (TryCaptureKeybind(out var keybind))
            {
                settings.SetKeybind(keybind);
                _isRecordingToggleKeybind = false;
                ConfigRepository.Save(_config);
            }
        }

        DrawKeybindConflicts(settings.Keybind);
    }

    private static bool TryCaptureKeybind(out RecordedKeybind keybind)
    {
        keybind = new RecordedKeybind();

        var pressedKeys = Service.KeyState.GetValidVirtualKeys()
            .Where(key => key.IsModifier() || key.IsPrimaryBindable())
            .Where(key => Service.KeyState[key])
            .ToArray();

        var primaryKey = pressedKeys.FirstOrDefault(key => key.IsPrimaryBindable(), VirtualKey.NO_KEY);
        if (primaryKey == VirtualKey.NO_KEY)
        {
            return false;
        }

        keybind.Key = primaryKey;
        keybind.Modifiers = pressedKeys
            .Where(key => key.IsModifier())
            .Select(key => key.NormalizeModifier())
            .Distinct()
            .OrderByModifier()
            .ToList();

        return true;
    }

    private static void DrawKeybindConflicts(RecordedKeybind keybind)
    {
        var conflicts = KeybindConflictDetector.GetGameConflicts(keybind);
        if (conflicts.Count == 0)
        {
            return;
        }

        DrawNestIndicator(2);
        var preview = string.Join(", ", conflicts.Take(4));
        if (conflicts.Count > 4)
        {
            preview += $", +{conflicts.Count - 4} more";
        }

        ImGui.TextDisabled($"Conflicts with game keybinds: {preview}");
    }

    private void DrawDtrSettings()
    {
        var settings = _config.Dtr;

        var enabled = settings.Enabled;
        if (ImGui.Checkbox("Show in Server Info Bar", ref enabled))
        {
            settings.Enabled = enabled;
            ConfigRepository.Save(_config);
        }

        DrawDisabled(!settings.Enabled, () =>
        {
            DrawNestIndicator(1);
            var textModeIndex = FindOptionIndex(DtrTextModeOptions, settings.TextMode);
            if (ImGui.Combo("Status text", ref textModeIndex, DtrTextModeLabels, DtrTextModeLabels.Length))
            {
                settings.TextMode = DtrTextModeOptions[textModeIndex].Value;
                ConfigRepository.Save(_config);
            }
        });

        ImGui.TextWrapped("Left-click the Server Info Bar entry to toggle MouseLock on/off. Right-click it to toggle this config window.");
    }
}
