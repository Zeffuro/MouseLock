using Dalamud.Plugin.Ipc;

namespace MouseLock.Integrations;

internal static class TPieIntegration
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
