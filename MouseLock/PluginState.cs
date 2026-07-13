using Dalamud.Interface.Windowing;
using MouseLock.Windows;
using MouseLock.Configuration;
using MouseLock.Commands;
using MouseLock.Ipc;
using MouseLock.MouseLook;
using MouseLock.Services;

namespace MouseLock;

public static class PluginState
{
    public static SystemConfiguration Config { get; set; } = null!;

    public static WindowSystem WindowSystem { get; set; } = null!;

    public static ConfigWindow ConfigWindow { get; set; } = null!;

    public static CommandHandler? CommandHandler { get; set; }

    public static MouseLookService? MouseLookService { get; set; }

    public static ToggleKeybindService? ToggleKeybindService { get; set; }

    public static DtrService? DtrService { get; set; }

    internal static MouseLockIpcProvider? IpcProvider { get; set; }

    public static void Reset()
    {
        Config = null!;
        WindowSystem = null!;
        ConfigWindow = null!;
        CommandHandler = null;
        MouseLookService = null;
        ToggleKeybindService = null;
        DtrService = null;
        IpcProvider = null;
    }
}
