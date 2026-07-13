using Dalamud.Interface.Windowing;
using MouseLock.Windows;
using MouseLock.Configuration;
using MouseLock.Commands;

namespace MouseLock;

public static class System
{
    public static SystemConfiguration Config { get; set; } = null!;

    public static WindowSystem WindowSystem { get; set; } = null!;
    public static MainWindow MainWindow { get; set; } = null!;


    public static ConfigWindow ConfigWindow { get; set; } = null!;


    public static CommandHandler? CommandHandler { get; set; }

    public static DtrService? DtrService { get; set; }


    public static void Reset()
    {
        Config = null!;
        WindowSystem = null!;
        MainWindow = null!;
        ConfigWindow = null!;
        CommandHandler = null;
        DtrService = null;
    }
}