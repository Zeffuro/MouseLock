using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using MouseLock.Windows;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.Commands;

namespace MouseLock;

public sealed class Plugin : IAsyncDalamudPlugin
{
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        System.Reset();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        System.Config = ConfigRepository.LoadOrDefault();
        ConfigBackup.DoConfigBackup(Services.PluginInterface);

        System.WindowSystem = new WindowSystem("MouseLock");
        System.MainWindow = new MainWindow(System.Config);
        System.WindowSystem.AddWindow(System.MainWindow);

        System.ConfigWindow = new ConfigWindow(System.Config);
        System.WindowSystem.AddWindow(System.ConfigWindow);

        Services.PluginInterface.UiBuilder.Draw += DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;


        System.CommandHandler = new CommandHandler();

        System.DtrService = new DtrService();


        ConfigRepository.SaveImmediate(System.Config);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        System.CommandHandler?.Dispose();
        System.DtrService?.Dispose();

        Services.PluginInterface.UiBuilder.Draw -= DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        System.WindowSystem?.RemoveAllWindows();
        System.ConfigWindow?.Dispose();
        System.MainWindow?.Dispose();


        ConfigRepository.SaveImmediate(System.Config);

        System.Reset();

        return ValueTask.CompletedTask;
    }

    private static void DrawUi() => System.WindowSystem?.Draw();


    private static void ToggleMainUi()
    {
        System.MainWindow?.Toggle();
    }

    private static void ToggleConfigUi()
    {
        System.ConfigWindow?.Toggle();
    }
}