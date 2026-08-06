using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

namespace MouseLock.Integrations;

internal static class TPieIntegration
{
    private static readonly ICallGateSubscriber<bool> IsRingActiveSubscriber =
        Service.PluginInterface.GetIpcSubscriber<bool>("TPie.IsRingActive");

    public static bool IsRingActive { get; private set; }

    public static void Update()
    {
        if (!IsRingActiveSubscriber.HasFunction)
        {
            IsRingActive = false;
            return;
        }

        try
        {
            IsRingActive = IsRingActiveSubscriber.InvokeFunc();
        }
        catch (IpcNotReadyError)
        {
            IsRingActive = false;
        }
    }
}
