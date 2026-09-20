using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using KamiToolKit;
using KamiToolKit.UiOverlay;
using MouseLock.Windows;
using MouseLock.Configuration.Persistence;
using MouseLock.Commands;
using MouseLock.Input;
using MouseLock.Input.Keybinds;
using MouseLock.MouseLook;
using MouseLock.UI;
using MouseLock.Targeting;

namespace MouseLock;

public sealed class Plugin : IAsyncDalamudPlugin
{
    private bool _nativeInitializationStarted;
    private OverlayController? _reticleOverlay;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        PluginState.Reset();
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        PluginState.Config = ConfigRepository.LoadOrDefault();
        ConfigBackup.DoConfigBackup(Service.PluginInterface);

        PluginState.WindowSystem = new WindowSystem("MouseLock");
        PluginState.ConfigWindow = new ConfigWindow(PluginState.Config);
        PluginState.FirstRunWindow = new FirstRunWindow(PluginState.Config);
        PluginState.WindowSystem.AddWindow(PluginState.ConfigWindow);
        PluginState.WindowSystem.AddWindow(PluginState.FirstRunWindow);

        _nativeInitializationStarted = true;
        await KamiToolKitLibrary.InitializeAsync(Service.PluginInterface, "MouseLock");
        cancellationToken.ThrowIfCancellationRequested();
        await Service.Framework.RunOnFrameworkThread(() =>
        {
            _reticleOverlay = new OverlayController();
            _reticleOverlay.AddNode(new ReticleNode());
        });

        Service.PluginInterface.UiBuilder.Draw += DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleUi;

        PluginState.CommandHandler = new CommandHandler();
        PluginState.TextInputMonitor = new TextInputMonitor();
        PluginState.MouseLookService = new MouseLookService(PluginState.TextInputMonitor);
        PluginState.TargetingService = new TargetingService();
        PluginState.ToggleKeybindListener = new ToggleKeybindListener(PluginState.TextInputMonitor);
        PluginState.DtrStatusService = new DtrStatusService();
        PluginState.IpcProvider = new();

        ConfigRepository.SaveImmediate(PluginState.Config);
    }

    public async ValueTask DisposeAsync()
    {
        PluginState.IpcProvider?.Dispose();
        PluginState.CommandHandler?.Dispose();
        PluginState.TargetingService?.Dispose();
        PluginState.MouseLookService?.Dispose();
        PluginState.ToggleKeybindListener?.Dispose();
        PluginState.TextInputMonitor?.Dispose();
        PluginState.DtrStatusService?.Dispose();

        Service.PluginInterface.UiBuilder.Draw -= DrawUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= ToggleUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= ToggleUi;

        PluginState.WindowSystem?.RemoveAllWindows();

        if (!Service.Framework.IsFrameworkUnloading)
        {
            await Service.Framework.RunOnFrameworkThread(() =>
            {
                _reticleOverlay?.Dispose();
                _reticleOverlay = null;
            });
        }
        if (_nativeInitializationStarted)
        {
            await Service.Framework.RunOnFrameworkThread(KamiToolKitLibrary.Dispose);
            _nativeInitializationStarted = false;
        }

        ConfigRepository.SaveImmediate(PluginState.Config);

        PluginState.Reset();
    }

    private static void DrawUi() => PluginState.WindowSystem.Draw();

    private static void ToggleUi() => PluginState.ConfigWindow.Toggle();
}
