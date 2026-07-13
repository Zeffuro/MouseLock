using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace MouseLock;

public sealed class DtrService : IDisposable
{
    private const string EntryTitle = "MouseLock";

    private IDtrBarEntry? dtrEntry;
    private DateTime lastUpdate = DateTime.MinValue;

    public DtrService()
    {
        Services.Framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Services.Framework.Update -= this.OnFrameworkUpdate;
        this.RemoveEntry();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if ((DateTime.Now - this.lastUpdate).TotalMilliseconds < 500)
        {
            return;
        }

        this.lastUpdate = DateTime.Now;
        this.UpdateBar();
    }

    private void UpdateBar()
    {
        var settings = System.Config.Dtr;
        if (!settings.Enabled)
        {
            this.RemoveEntry();
            return;
        }

        this.EnsureEntry();
        if (this.dtrEntry is null)
        {
            return;
        }

        if (!Services.ClientState.IsLoggedIn && !settings.ShowWhenLoggedOut)
        {
            this.dtrEntry.Shown = false;
            return;
        }

        this.dtrEntry.Text = new SeStringBuilder().AddText(settings.Text).Build();
        this.dtrEntry.Tooltip = new SeStringBuilder().AddText(settings.Tooltip).Build();
        this.dtrEntry.Shown = true;
    }

    private void EnsureEntry()
    {
        if (this.dtrEntry is not null)
        {
            return;
        }

        this.dtrEntry = Services.DtrBar.Get(EntryTitle);
        this.dtrEntry.Shown = true;
        this.dtrEntry.OnClick += OnDtrClick;
    }

    private void RemoveEntry()
    {
        if (this.dtrEntry is null)
        {
            return;
        }

        this.dtrEntry.OnClick -= OnDtrClick;
        this.dtrEntry.Remove();
        this.dtrEntry = null;
    }

    private static void OnDtrClick(DtrInteractionEvent interaction)
    {
        if (System.Config.Dtr.ClickToOpenConfig)
        {
            System.ConfigWindow.Toggle();
            return;
        }

        System.MainWindow.Toggle();
    }
}