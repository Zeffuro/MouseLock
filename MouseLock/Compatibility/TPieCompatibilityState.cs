using Dalamud.Plugin.Ipc;

namespace MouseLock.Compatibility;

internal static class TPieCompatibilityState
{
    private static readonly ICallGateSubscriber<bool> IsRingActiveSubscriber =
        Service.PluginInterface.GetIpcSubscriber<bool>("TPie.IsRingActive");

    public static bool IsRingActive()
    {
        try
        {
            return IsRingActiveSubscriber.InvokeFunc();
        }
        catch
        {
            return false;
        }
    }
}
