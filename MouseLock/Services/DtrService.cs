using System;
using System.Diagnostics;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using MouseLock.Configuration;

namespace MouseLock.Services;

public sealed class DtrService : IDisposable
{
    private const string EntryTitle = "MouseLock";

    private IDtrBarEntry? _dtrEntry;
    private readonly Stopwatch _updateTimer = Stopwatch.StartNew();

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
        if (_updateTimer.ElapsedMilliseconds < 500)
        {
            return;
        }

        _updateTimer.Restart();
        UpdateBar();
    }

    private void UpdateBar()
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

        _dtrEntry.Text = new SeStringBuilder().AddText(GetStatusText()).Build();
        _dtrEntry.Tooltip = new SeStringBuilder().AddText("Left-click to toggle MouseLock. Right-click to toggle config.").Build();
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
                MouseLockSettingsActions.ToggleEnabled();
                break;

            case MouseClickType.Right:
                PluginState.ConfigWindow.Toggle();
                break;
        }
    }

    private static string GetStatusText()
    {
        if (!PluginState.Config.General.Enabled)
        {
            return "MouseLock: Off";
        }

        return PluginState.MouseLookService?.IsActive == true
            ? "MouseLock: Active"
            : "MouseLock: On";
    }
}
