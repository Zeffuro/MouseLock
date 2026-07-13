using MouseLock.Configuration.Persistence;

namespace MouseLock.Configuration;

public static class MouseLockSettingsActions
{
    public static void SetEnabled(bool enabled, bool save = true)
    {
        if (PluginState.Config.General.Enabled == enabled)
        {
            return;
        }

        PluginState.Config.General.Enabled = enabled;

        if (save)
        {
            ConfigRepository.Save(PluginState.Config);
        }
    }

    public static void ToggleEnabled(bool save = true)
    {
        SetEnabled(!PluginState.Config.General.Enabled, save);
    }
}
