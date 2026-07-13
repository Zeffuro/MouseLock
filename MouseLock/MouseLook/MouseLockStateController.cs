using MouseLock.Configuration.Persistence;

namespace MouseLock.MouseLook;

internal static class MouseLockStateController
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

        PluginState.MouseLookService?.RefreshCurrentStatus();
        PluginState.DtrStatusService?.Refresh();
    }

    public static void ToggleEnabled(bool save = true)
    {
        SetEnabled(!PluginState.Config.General.Enabled, save);
    }
}
