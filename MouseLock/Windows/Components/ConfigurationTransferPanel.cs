using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Configuration.ImportExport;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Windows.Components;

internal sealed class ConfigurationTransferPanel(SystemConfiguration config)
{
    private const string ConfirmationPopupName = "Confirm configuration change";
    private const float ConfirmationPopupWidth = 420.0f;
    private const float ConfirmationButtonWidth = 96.0f;

    private string _message = string.Empty;
    private bool _messageIsError;
    private ConfirmationAction _pendingConfirmation;

    public void Draw()
    {
        if (ImGui.Button("Export to clipboard"))
        {
            SetMessage(ConfigPorter.TryExportConfigToClipboard(config, out var message), message);
        }

        ImGui.SameLine();
        if (ImGui.Button("Hold Shift to import"))
        {
            if (!Service.KeyState[VirtualKey.SHIFT])
            {
                SetMessage(false, "Hold Shift while clicking import.");
            }
            else
            {
                OpenConfirmation(ConfirmationAction.Import);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset to defaults"))
        {
            OpenConfirmation(ConfirmationAction.Reset);
        }

        ImGui.Spacing();
        if (ImGui.Button("Restore latest backup"))
        {
            OpenConfirmation(ConfirmationAction.RestoreLatestBackup);
        }

        ImGui.SameLine();
        if (ImGui.Button("Open backup folder"))
        {
            SetMessage(TryOpenBackupFolder(out var message), message);
        }

        if (!string.IsNullOrEmpty(_message))
        {
            ImGui.TextColored(
                _messageIsError ? ImGuiColors.ErrorForeground : ImGuiColors.SuccessForeground,
                _message);
        }

        DrawConfirmationPopup();
    }

    private void SetMessage(bool success, string message)
    {
        _messageIsError = !success;
        _message = message;
    }

    private void OpenConfirmation(ConfirmationAction action)
    {
        _pendingConfirmation = action;
        ImGui.OpenPopup(ConfirmationPopupName);
    }

    private void DrawConfirmationPopup()
    {
        var open = _pendingConfirmation != ConfirmationAction.None;
        var popupWidth = ConfirmationPopupWidth * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSize(new Vector2(popupWidth, 0.0f), ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(popupWidth, 0.0f),
            new Vector2(popupWidth, float.MaxValue));

        using var popup = ImRaii.PopupModal(ConfirmationPopupName, ref open, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
        {
            if (!open)
            {
                _pendingConfirmation = ConfirmationAction.None;
            }

            return;
        }

        ImGui.TextWrapped(GetConfirmationText());
        ImGui.Spacing();

        var buttonSize = new Vector2(ConfirmationButtonWidth * ImGuiHelpers.GlobalScale, 0.0f);
        if (ImGui.Button(GetConfirmLabel(), buttonSize))
        {
            ConfirmPendingAction();
            ImGui.CloseCurrentPopup();
            _pendingConfirmation = ConfirmationAction.None;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", buttonSize))
        {
            ImGui.CloseCurrentPopup();
            _pendingConfirmation = ConfirmationAction.None;
        }
    }

    private string GetConfirmLabel()
        => _pendingConfirmation switch
        {
            ConfirmationAction.Reset => "Reset",
            ConfirmationAction.RestoreLatestBackup => "Restore",
            _ => "Import",
        };

    private string GetConfirmationText()
        => _pendingConfirmation switch
        {
            ConfirmationAction.Import => "Replace current MouseLock settings with the config from your clipboard?",
            ConfirmationAction.Reset => "Reset all MouseLock settings to defaults?",
            ConfirmationAction.RestoreLatestBackup => "Replace current MouseLock settings with the latest automatic backup?",
            _ => string.Empty,
        };

    private void ConfirmPendingAction()
    {
        switch (_pendingConfirmation)
        {
            case ConfirmationAction.Import:
                SetMessage(ConfigPorter.TryImportConfigFromClipboard(config, out var importMessage), importMessage);
                break;

            case ConfirmationAction.Reset:
                SetMessage(ConfigPorter.TryResetConfig(config, out var resetMessage), resetMessage);
                break;

            case ConfirmationAction.RestoreLatestBackup:
                SetMessage(ConfigPorter.TryRestoreLatestBackup(config, out var restoreMessage), restoreMessage);
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

    private enum ConfirmationAction
    {
        None,
        Import,
        Reset,
        RestoreLatestBackup,
    }
}
