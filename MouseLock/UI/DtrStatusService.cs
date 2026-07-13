using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using MouseLock.Configuration;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.UI;

internal sealed class DtrStatusService : IDisposable
{
    private const string EntryTitle = "MouseLock";

    private IDtrBarEntry? _dtrEntry;

    public DtrStatusService()
    {
        if (PluginState.MouseLookService is not null)
        {
            PluginState.MouseLookService.StatusChanged += OnStatusChanged;
        }

        Refresh();
    }

    public void Dispose()
    {
        if (PluginState.MouseLookService is not null)
        {
            PluginState.MouseLookService.StatusChanged -= OnStatusChanged;
        }

        RemoveEntry();
    }

    internal void Refresh()
        => UpdateBar();

    private void OnStatusChanged(MouseLookStatus status)
        => UpdateBar(status);

    private void UpdateBar(MouseLookStatus? statusOverride = null)
    {
        var settings = PluginState.Config.Dtr;
        if (!settings.Enabled)
        {
            RemoveEntry();
            return;
        }

        EnsureEntry();
        if (_dtrEntry is null)
        {
            return;
        }

        if (!Service.ClientState.IsLoggedIn)
        {
            _dtrEntry.Shown = false;
            return;
        }

        var status = statusOverride ?? GetStatus();
        _dtrEntry.Text = new SeStringBuilder().AddText(DtrStatusFormatter.GetText(status, settings.TextMode)).Build();
        _dtrEntry.Tooltip = new SeStringBuilder()
            .AddText(MouseLookStatusFormatter.GetDetail(status))
            .AddText("\nLeft-click to toggle MouseLock. Right-click to toggle config.")
            .Build();
        _dtrEntry.Shown = true;
    }

    private void EnsureEntry()
    {
        if (_dtrEntry is not null)
        {
            return;
        }

        _dtrEntry = Service.DtrBar.Get(EntryTitle);
        _dtrEntry.Shown = true;
        _dtrEntry.OnClick += OnDtrClick;
    }

    private void RemoveEntry()
    {
        if (_dtrEntry is null)
        {
            return;
        }

        _dtrEntry.OnClick -= OnDtrClick;
        _dtrEntry.Remove();
        _dtrEntry = null;
    }

    private static void OnDtrClick(DtrInteractionEvent interaction)
    {
        switch (interaction.ClickType)
        {
            case MouseClickType.Left:
                MouseLockStateController.ToggleEnabled();
                break;

            case MouseClickType.Right:
                PluginState.ConfigWindow.Toggle();
                break;
        }
    }

    private static MouseLookStatus GetStatus()
        => PluginState.MouseLookService?.Status
           ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
}
