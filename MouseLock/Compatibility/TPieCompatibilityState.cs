using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Ipc;

namespace MouseLock.Compatibility;

internal static unsafe class TPieCompatibilityState
{
    // private static ReadOnlySpan<byte> RingWindowNamePrefix => "TPie_ring_"u8;

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
        /*
        var context = ImGui.GetCurrentContext();
        if (context.IsNull)
        {
            return false;
        }

        var currentFrame = context.FrameCount;
        var windows = context.Windows;

        for (var index = 0; index < windows.Size; index++)
        {
            var window = windows.Ref(index);

            if (window.IsNull ||
                !WasActiveRecently(window, currentFrame) ||
                !StartsWith(window.Name, RingWindowNamePrefix))
            {
                continue;
            }

            return true;
        }

        return false;
        */
    }

    /*
    private static bool WasActiveRecently(ImGuiWindowPtr window, int currentFrame)
        => window.Active ||
           window.WasActive ||
           window.LastFrameActive >= currentFrame - 1;

    private static bool StartsWith(byte* value, ReadOnlySpan<byte> prefix)
    {
        if (value is null)
        {
            return false;
        }

        for (var index = 0; index < prefix.Length; index++)
        {
            if (value[index] != prefix[index])
            {
                return false;
            }
        }

        return true;
    }
    */
}
