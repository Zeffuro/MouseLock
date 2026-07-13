namespace MouseLock.Configuration.Persistence;

public static class ConfigRepository
{
    public static SystemConfiguration LoadOrDefault()
    {
        var config = Service.PluginInterface.GetPluginConfig() as SystemConfiguration ?? new SystemConfiguration();

        if (config.Version > SystemConfiguration.CurrentVersion)
        {
            Service.Logger.Warning(
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
            Service.Logger.Error("Refusing to save null configuration.");
            return;
        }

        config.EnsureInitialized();
        Service.PluginInterface.SavePluginConfig(config);
    }
}
