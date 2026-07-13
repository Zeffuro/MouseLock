using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;

namespace MouseLock.Input;

internal static class VirtualKeyExtensions
{
    public static readonly VirtualKey[] ModifierKeys =
    [
        VirtualKey.CONTROL,
        VirtualKey.SHIFT,
        VirtualKey.MENU,
    ];

    public static bool IsModifier(this VirtualKey key)
        => key is VirtualKey.CONTROL
            or VirtualKey.SHIFT
            or VirtualKey.MENU
            or VirtualKey.LCONTROL
            or VirtualKey.RCONTROL
            or VirtualKey.LSHIFT
            or VirtualKey.RSHIFT
            or VirtualKey.LMENU
            or VirtualKey.RMENU;

    public static VirtualKey NormalizeModifier(this VirtualKey key)
        => key switch
        {
            VirtualKey.LCONTROL or VirtualKey.RCONTROL => VirtualKey.CONTROL,
            VirtualKey.LSHIFT or VirtualKey.RSHIFT => VirtualKey.SHIFT,
            VirtualKey.LMENU or VirtualKey.RMENU => VirtualKey.MENU,
            _ => key,
        };

    public static bool IsMouseButton(this VirtualKey key)
        => key is VirtualKey.MBUTTON or VirtualKey.XBUTTON1 or VirtualKey.XBUTTON2;

    public static bool IsPrimaryBindable(this VirtualKey key)
        => key != VirtualKey.NO_KEY && !key.IsModifier() && (key.IsMouseButton() || !IsExcludedMouseButton(key));

    public static string GetDisplayName(this VirtualKey key) => key switch
    {
        VirtualKey.CONTROL => "Ctrl",
        VirtualKey.SHIFT => "Shift",
        VirtualKey.MENU => "Alt",
        VirtualKey.MBUTTON => "Middle Mouse",
        VirtualKey.XBUTTON1 => "Mouse 4",
        VirtualKey.XBUTTON2 => "Mouse 5",
        VirtualKey.NO_KEY => "None",
        _ => key.ToString(),
    };

    public static IOrderedEnumerable<VirtualKey> OrderByModifier(this IEnumerable<VirtualKey> keys)
        => keys.OrderBy(key => key switch
        {
            VirtualKey.CONTROL => 0,
            VirtualKey.SHIFT => 1,
            VirtualKey.MENU => 2,
            _ => 3,
        });

    private static bool IsExcludedMouseButton(VirtualKey key)
        => key is VirtualKey.LBUTTON or VirtualKey.RBUTTON;
}
