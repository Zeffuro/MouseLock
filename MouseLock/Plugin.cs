using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using MouseLock.Windows;
using MouseLock.Configuration.Persistence;
using MouseLock.Commands;
using MouseLock.MouseLook;
using MouseLock.Services;

namespace MouseLock;

public sealed class Plugin : IAsyncDalamudPlugin
{
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        System.Reset();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        System.Config = ConfigRepository.LoadOrDefault();
        ConfigBackup.DoConfigBackup(Service.PluginInterface);

        System.WindowSystem = new WindowSystem("MouseLock");
        System.ConfigWindow = new ConfigWindow(System.Config);
        System.WindowSystem.AddWindow(System.ConfigWindow);

        Service.PluginInterface.UiBuilder.Draw += DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleUi;

        System.CommandHandler = new CommandHandler();
        System.MouseLookService = new MouseLookService();
        System.ToggleKeybindService = new ToggleKeybindService();
        System.DtrService = new DtrService();

        ConfigRepository.SaveImmediate(System.Config);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        System.CommandHandler?.Dispose();
        System.MouseLookService?.Dispose();
        System.ToggleKeybindService?.Dispose();
        System.DtrService?.Dispose();

        Service.PluginInterface.UiBuilder.Draw -= DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleUi;

        System.WindowSystem?.RemoveAllWindows();
        System.ConfigWindow?.Dispose();

        ConfigRepository.SaveImmediate(System.Config);

        System.Reset();

        return ValueTask.CompletedTask;
    }

    private static void DrawUi() => System.WindowSystem.Draw();

    private static void ToggleUi() => System.ConfigWindow.Toggle();
}
