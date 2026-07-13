namespace MouseLock.Configuration.Persistence;

public static class ConfigRepository
{
    public static SystemConfiguration LoadOrDefault()
    {
        var config = Services.PluginInterface.GetPluginConfig() as SystemConfiguration ?? new SystemConfiguration();

        if (config.Version > SystemConfiguration.CurrentVersion)
        {
            Services.Logger.Warning(
                "Configuration version {ConfigVersion} is newer than the supported version {CurrentVersion}.",
                config.Version,
                SystemConfiguration.CurrentVersion);
        }

        config.EnsureInitialized();
        return config;
    }

    public static SystemConfiguration Reset()
    {
        var config = new SystemConfiguration();
        config.EnsureInitialized();
        SaveImmediate(config);
        return config;
    }

    public static void Save(SystemConfiguration config)
        => SaveImmediate(config);

    public static void SaveImmediate(SystemConfiguration? config)
    {
        if (config is null)
        {
            Services.Logger.Error("Refusing to save null configuration.");
            return;
        }

        config.EnsureInitialized();
        Services.PluginInterface.SavePluginConfig(config);
    }
}