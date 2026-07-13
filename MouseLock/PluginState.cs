using Dalamud.Interface.Windowing;
using MouseLock.Windows;
using MouseLock.Configuration;
using MouseLock.Commands;
using MouseLock.Input;
using MouseLock.Input.Keybinds;
using MouseLock.Ipc;
using MouseLock.MouseLook;
using MouseLock.UI;

namespace MouseLock;

public static class PluginState
{
    public static SystemConfiguration Config { get; set; } = null!;

    public static WindowSystem WindowSystem { get; set; } = null!;

    internal static ConfigWindow ConfigWindow { get; set; } = null!;

    internal static CommandHandler? CommandHandler { get; set; }

    internal static TextInputMonitor? TextInputMonitor { get; set; }

    internal static MouseLookService? MouseLookService { get; set; }

    internal static ToggleKeybindListener? ToggleKeybindListener { get; set; }

    internal static DtrStatusService? DtrStatusService { get; set; }

    internal static MouseLockIpcProvider? IpcProvider { get; set; }

    public static void Reset()
    {
        Config = null!;
        WindowSystem = null!;
        ConfigWindow = null!;
        CommandHandler = null;
        TextInputMonitor = null;
        MouseLookService = null;
        ToggleKeybindListener = null;
        DtrStatusService = null;
        IpcProvider = null;
    }
}
