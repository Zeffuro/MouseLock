using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using MouseLock.Configuration;

namespace MouseLock.Services;

public sealed class DtrService : IDisposable
{
    private const string EntryTitle = "MouseLock";

    private IDtrBarEntry? _dtrEntry;
    private DateTime _lastUpdate = DateTime.MinValue;

    public DtrService()
    {
        Service.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        RemoveEntry();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 500)
        {
            return;
        }

        _lastUpdate = DateTime.Now;
        UpdateBar();
    }

    private void UpdateBar()
    {
        var settings = System.Config.Dtr;
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

        if (!Service.ClientState.IsLoggedIn && !settings.ShowWhenLoggedOut)
        {
            _dtrEntry.Shown = false;
            return;
        }

        _dtrEntry.Text = new SeStringBuilder().AddText(GetStatusText()).Build();
        _dtrEntry.Tooltip = new SeStringBuilder().AddText("Click to enable or disable MouseLock.").Build();
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
        MouseLockSettingsActions.ToggleEnabled();
    }

    private static string GetStatusText()
    {
        if (!System.Config.General.Enabled)
        {
            return "MouseLock: Off";
        }

        return System.MouseLookService?.IsActive == true
            ? "MouseLock: Active"
            : "MouseLock: On";
    }
}
