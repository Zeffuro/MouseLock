using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration.ImportExport;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private const string ConfigConfirmationPopupName = "Confirm configuration change";
    private const float ConfigConfirmationPopupWidth = 420.0f;
    private const float ConfigConfirmationButtonWidth = 96.0f;

    private string _configImportExportMessage = string.Empty;
    private bool _configImportExportIsError;
    private ConfigConfirmationAction _pendingConfigConfirmation;

    private void DrawImportExportSettings()
    {
        if (ImGui.Button("Export to clipboard"))
        {
            SetConfigImportExportMessage(ConfigPorter.TryExportConfigToClipboard(_config, out var message), message);
        }

        ImGui.SameLine();
        if (ImGui.Button("Hold Shift to import"))
        {
            if (!Service.KeyState[VirtualKey.SHIFT])
            {
                SetConfigImportExportMessage(false, "Hold Shift while clicking import.");
            }
            else
            {
                OpenConfigConfirmation(ConfigConfirmationAction.Import);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset to defaults"))
        {
            OpenConfigConfirmation(ConfigConfirmationAction.Reset);
        }

        ImGui.Spacing();
        if (ImGui.Button("Restore latest backup"))
        {
            OpenConfigConfirmation(ConfigConfirmationAction.RestoreLatestBackup);
        }

        ImGui.SameLine();
        if (ImGui.Button("Open backup folder"))
        {
            SetConfigImportExportMessage(TryOpenBackupFolder(out var message), message);
        }

        if (!string.IsNullOrEmpty(_configImportExportMessage))
        {
            var color = _configImportExportIsError
                ? new System.Numerics.Vector4(1.0f, 0.35f, 0.35f, 1.0f)
                : new System.Numerics.Vector4(0.35f, 1.0f, 0.55f, 1.0f);

            ImGui.TextColored(color, _configImportExportMessage);
        }

        DrawConfigConfirmationPopup();
    }

    private void SetConfigImportExportMessage(bool success, string message)
    {
        _configImportExportIsError = !success;
        _configImportExportMessage = message;
    }

    private void OpenConfigConfirmation(ConfigConfirmationAction action)
    {
        _pendingConfigConfirmation = action;
        ImGui.OpenPopup(ConfigConfirmationPopupName);
    }

    private void DrawConfigConfirmationPopup()
    {
        var open = _pendingConfigConfirmation != ConfigConfirmationAction.None;
        var popupWidth = ConfigConfirmationPopupWidth * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0.0f), ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(popupWidth, 0.0f),
            new Vector2(popupWidth, float.MaxValue));
        using var popup = ImRaii.PopupModal(ConfigConfirmationPopupName, ref open, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
        {
            if (!open)
            {
                _pendingConfigConfirmation = ConfigConfirmationAction.None;
            }

            return;
        }

        ImGui.TextWrapped(GetConfigConfirmationText());
        ImGui.Spacing();

        var buttonSize = new Vector2(ConfigConfirmationButtonWidth * ImGuiHelpers.GlobalScale, 0.0f);
        var confirmLabel = _pendingConfigConfirmation == ConfigConfirmationAction.Reset
            ? "Reset"
            : _pendingConfigConfirmation == ConfigConfirmationAction.RestoreLatestBackup
                ? "Restore"
                : "Import";
        if (ImGui.Button(confirmLabel, buttonSize))
        {
            ConfirmPendingConfigAction();
            ImGui.CloseCurrentPopup();
            _pendingConfigConfirmation = ConfigConfirmationAction.None;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", buttonSize))
        {
            ImGui.CloseCurrentPopup();
            _pendingConfigConfirmation = ConfigConfirmationAction.None;
        }
    }

    private string GetConfigConfirmationText()
        => _pendingConfigConfirmation switch
        {
            ConfigConfirmationAction.Import => "Replace current MouseLock settings with the config from your clipboard?",
            ConfigConfirmationAction.Reset => "Reset all MouseLock settings to defaults?",
            ConfigConfirmationAction.RestoreLatestBackup => "Replace current MouseLock settings with the latest automatic backup?",
            _ => string.Empty,
        };

    private void ConfirmPendingConfigAction()
    {
        switch (_pendingConfigConfirmation)
        {
            case ConfigConfirmationAction.Import:
                SetConfigImportExportMessage(ConfigPorter.TryImportConfigFromClipboard(_config, out var importMessage), importMessage);
                break;

            case ConfigConfirmationAction.Reset:
                SetConfigImportExportMessage(ConfigPorter.TryResetConfig(_config, out var resetMessage), resetMessage);
                break;

            case ConfigConfirmationAction.RestoreLatestBackup:
                SetConfigImportExportMessage(ConfigPorter.TryRestoreLatestBackup(_config, out var restoreMessage), restoreMessage);
                break;
        }
    }

    private static bool TryOpenBackupFolder(out string message)
    {
        try
        {
            var backupDirectory = ConfigBackup.GetBackupDirectory(Service.PluginInterface);
            if (backupDirectory is null)
            {
                message = "Backup folder could not be resolved.";
                return false;
            }

            if (!backupDirectory.Exists)
            {
                backupDirectory.Create();
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = backupDirectory.FullName,
                UseShellExecute = true,
            });

            message = "Backup folder opened.";
            return true;
        }
        catch (Exception exception)
        {
            message = "Backup folder could not be opened.";
            Service.Logger.Error(exception, message);
            return false;
        }
    }

    private enum ConfigConfirmationAction
    {
        None,
        Import,
        Reset,
        RestoreLatestBackup,
    }
}
