using System;
using Dalamud.Configuration;

namespace MouseLock.Configuration;

[Serializable]
public sealed class SystemConfiguration : IPluginConfiguration
{
    public const int CurrentVersion = 2;

    public int Version { get; set; }

    private GeneralSettings _general = new();
    private MouseLookActivationSettings _activation = new();
    private MouseActionSettings _mouseActions = new();
    private MouseLookCompatibilitySettings _compatibility = new();
    private ToggleKeybindSettings _toggleKeybind = new();
    private DtrSettings _dtr = new();

    public GeneralSettings General
    {
        get => _general;
        set => _general = value ?? new GeneralSettings();
    }

    public MouseLookActivationSettings Activation
    {
        get => _activation;
        set => _activation = value ?? new MouseLookActivationSettings();
    }

    public MouseActionSettings MouseActions
    {
        get => _mouseActions;
        set => _mouseActions = value ?? new MouseActionSettings();
    }

    public MouseLookCompatibilitySettings Compatibility
    {
        get => _compatibility;
        set => _compatibility = value ?? new MouseLookCompatibilitySettings();
    }

    public ToggleKeybindSettings ToggleKeybind
    {
        get => _toggleKeybind;
        set => _toggleKeybind = value ?? new ToggleKeybindSettings();
    }

    public DtrSettings Dtr
    {
        get => _dtr;
        set => _dtr = value ?? new DtrSettings();
    }

    public void EnsureInitialized()
    {
        _general ??= new GeneralSettings();
        _activation ??= new MouseLookActivationSettings();
        _activation.EnsureInitialized();
        _mouseActions ??= new MouseActionSettings();
        _mouseActions.EnsureInitialized();
        _compatibility ??= new MouseLookCompatibilitySettings();
        _toggleKeybind ??= new ToggleKeybindSettings();
        _toggleKeybind.EnsureInitialized();
        _dtr ??= new DtrSettings();

        if (Version < CurrentVersion)
        {
            Migrate();
        }
    }

    public void CopyFrom(SystemConfiguration source)
    {
        Version = source.Version;
        General = source.General;
        Activation = source.Activation;
        MouseActions = source.MouseActions;
        Compatibility = source.Compatibility;
        ToggleKeybind = source.ToggleKeybind;
        Dtr = source.Dtr;
        EnsureInitialized();
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
