using Dalamud.Bindings.ImGui;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private void DrawToggleKeybindSettings()
    {
        var settings = _config.General.ToggleKeybind;

        var enabled = settings.Enabled;
        if (ImGui.Checkbox("Enable toggle keybind", ref enabled))
        {
            settings.Enabled = enabled;
            ConfigRepository.Save(_config);
        }

        DrawNestIndicator(1);
        var keyIndex = FindOptionIndex(ToggleKeyOptions, settings.Key);
        if (ImGui.Combo("Toggle key", ref keyIndex, ToggleKeyLabels, ToggleKeyLabels.Length))
        {
            settings.Key = ToggleKeyOptions[keyIndex].Value;
            ConfigRepository.Save(_config);
        }

        DrawNestIndicator(1);
        var ignoreTextInput = settings.IgnoreWhileTextInputActive;
        if (ImGui.Checkbox("Ignore while text input is active", ref ignoreTextInput))
        {
            settings.IgnoreWhileTextInputActive = ignoreTextInput;
            ConfigRepository.Save(_config);
        }
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

        ImGui.TextWrapped("Left-click the Server Info Bar entry to toggle MouseLock on/off. Right-click it to toggle this config window.");
    }
}
