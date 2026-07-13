using System;
using Dalamud.Configuration;

namespace MouseLock.Configuration;

[Serializable]
public sealed class SystemConfiguration : IPluginConfiguration
{
    public const int CurrentVersion = 2;

    public int Version { get; set; }

    private GeneralSettings _general = new();
    private DtrSettings _dtr = new();

    public GeneralSettings General
    {
        get => _general;
        set => _general = value ?? new GeneralSettings();
    }

    public DtrSettings Dtr
    {
        get => _dtr;
        set => _dtr = value ?? new DtrSettings();
    }

    public void EnsureInitialized()
    {
        _general ??= new GeneralSettings();
        _general.EnsureInitialized();
        _dtr ??= new DtrSettings();

        if (Version < CurrentVersion)
        {
            Migrate();
        }
    }

    private void Migrate()
    {
        while (Version < CurrentVersion)
        {
            switch (Version)
            {
                case <= 0:
                    MigrateFromUnversionedConfig();
                    Version = 1;
                    break;
                case 1:
                    MigrateToVersion2();
                    Version = 2;
                    break;
                default:
                    Version = CurrentVersion;
                    break;
            }
        }
    }

    private void MigrateFromUnversionedConfig()
    {
    }

    private void MigrateToVersion2()
    {
        _dtr.Enabled = true;
    }
}
