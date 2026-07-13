using MouseLock.Configuration.Persistence;

namespace MouseLock.Configuration;

public static class MouseLockSettingsActions
{
    public static void SetEnabled(bool enabled, bool save = true)
    {
        if (System.Config.General.Enabled == enabled)
        {
            return;
        }

        System.Config.General.Enabled = enabled;

        if (save)
        {
            ConfigRepository.Save(System.Config);
        }
    }

    public static void ToggleEnabled(bool save = true)
    {
        SetEnabled(!System.Config.General.Enabled, save);
    }
}
