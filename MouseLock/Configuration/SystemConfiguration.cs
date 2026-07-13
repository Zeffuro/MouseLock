using System;
using Dalamud.Configuration;

namespace MouseLock.Configuration;

[Serializable]
public sealed class SystemConfiguration : IPluginConfiguration
{
    public const int CurrentVersion = 1;

    public int Version { get; set; }

    private GeneralSettings general = new();
    private DtrSettings dtr = new();

    public GeneralSettings General
    {
        get => this.general;
        set => this.general = value ?? new GeneralSettings();
    }

    public DtrSettings Dtr
    {
        get => this.dtr;
        set => this.dtr = value ?? new DtrSettings();
    }

    public void EnsureInitialized()
    {
        this.general ??= new GeneralSettings();
        this.dtr ??= new DtrSettings();

        if (this.Version < CurrentVersion)
        {
            this.Migrate();
        }
    }

    private void Migrate()
    {
        while (this.Version < CurrentVersion)
        {
            switch (this.Version)
            {
                case <= 0:
                    this.MigrateFromUnversionedConfig();
                    this.Version = 1;
                    break;
                default:
                    this.Version = CurrentVersion;
                    break;
            }
        }
    }

    private void MigrateFromUnversionedConfig()
    {
    }
}