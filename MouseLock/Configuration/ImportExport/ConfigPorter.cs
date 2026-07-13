using System;
using Dalamud.Bindings.ImGui;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Configuration.ImportExport;

internal static class ConfigPorter
{
    public static bool TryExportConfigToClipboard(SystemConfiguration config, out string message)
    {
        try
        {
            var exportString = ConfigSerializer.SerializeConfig(config);
            ImGui.SetClipboardText(exportString);

            message = "Configuration exported to clipboard.";
            Service.Logger.Information(message);
            return true;
        }
        catch (Exception exception)
        {
            message = "Configuration export failed.";
            Service.Logger.Error(exception, message);
            return false;
        }
    }

    public static bool TryImportConfigFromClipboard(SystemConfiguration target, out string message)
    {
        var clipboard = ImGui.GetClipboardText();
        if (!ConfigSerializer.TryDeserializeConfig(clipboard, out var imported, out message) || imported is null)
        {
            Service.Logger.Warning(message);
            return false;
        }

        target.CopyFrom(imported);
        ConfigRepository.SaveImmediate(target);
        PluginState.MouseLookService?.RefreshCurrentStatus();

        message = "Configuration imported from clipboard.";
        Service.Logger.Information(message);
        return true;
    }

    public static bool TryResetConfig(SystemConfiguration target, out string message)
    {
        try
        {
            var reset = new SystemConfiguration();
            reset.EnsureInitialized();

            target.CopyFrom(reset);
            ConfigRepository.SaveImmediate(target);
            PluginState.MouseLookService?.RefreshCurrentStatus();

            message = "Configuration reset to defaults.";
            Service.Logger.Information(message);
            return true;
        }
        catch (Exception exception)
        {
            message = "Configuration reset failed.";
            Service.Logger.Error(exception, message);
            return false;
        }
    }

    public static bool TryRestoreLatestBackup(SystemConfiguration target, out string message)
    {
        try
        {
            if (!ConfigBackup.TryLoadLatestBackup(out var backupJson, out message))
            {
                Service.Logger.Warning(message);
                return false;
            }

            if (!ConfigSerializer.TryDeserializeConfig(backupJson, out var backupConfig, out message) || backupConfig is null)
            {
                Service.Logger.Warning(message);
                return false;
            }

            target.CopyFrom(backupConfig);
            ConfigRepository.SaveImmediate(target);
            PluginState.MouseLookService?.RefreshCurrentStatus();

            message = "Latest backup restored.";
            Service.Logger.Information(message);
            return true;
        }
        catch (Exception exception)
        {
            message = "Backup restore failed.";
            Service.Logger.Error(exception, message);
            return false;
        }
    }
}
