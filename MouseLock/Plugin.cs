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
        PluginState.Reset();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        PluginState.Config = ConfigRepository.LoadOrDefault();
        ConfigBackup.DoConfigBackup(Service.PluginInterface);

        PluginState.WindowSystem = new WindowSystem("MouseLock");
        PluginState.ConfigWindow = new ConfigWindow(PluginState.Config);
        PluginState.WindowSystem.AddWindow(PluginState.ConfigWindow);

        Service.PluginInterface.UiBuilder.Draw += DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleUi;

        PluginState.CommandHandler = new CommandHandler();
        PluginState.MouseLookService = new MouseLookService();
        PluginState.ToggleKeybindService = new ToggleKeybindService();
        PluginState.DtrService = new DtrService();
        PluginState.IpcProvider = new();

        ConfigRepository.SaveImmediate(PluginState.Config);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        PluginState.IpcProvider?.Dispose();
        PluginState.CommandHandler?.Dispose();
        PluginState.MouseLookService?.Dispose();
        PluginState.ToggleKeybindService?.Dispose();
        PluginState.DtrService?.Dispose();

        Service.PluginInterface.UiBuilder.Draw -= DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleUi;

        PluginState.WindowSystem?.RemoveAllWindows();
        PluginState.ConfigWindow?.Dispose();

        ConfigRepository.SaveImmediate(PluginState.Config);

        PluginState.Reset();

        return ValueTask.CompletedTask;
    }

    private static void DrawUi() => PluginState.WindowSystem.Draw();

    private static void ToggleUi() => PluginState.ConfigWindow.Toggle();
}
