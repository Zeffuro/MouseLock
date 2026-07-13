using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using MouseLock.Configuration;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private static readonly ComboOption<ReleaseModifierKey>[] ReleaseModifierOptions =
    [
        new(ReleaseModifierKey.Alt, "Alt"),
        new(ReleaseModifierKey.Control, "Control"),
        new(ReleaseModifierKey.Shift, "Shift"),
        new(ReleaseModifierKey.None, "None"),
    ];

    private static readonly string[] ReleaseModifierLabels = CreateLabels(ReleaseModifierOptions);

    private static readonly ComboOption<MouseButtonBindingKind>[] BindingKindOptions =
    [
        new(MouseButtonBindingKind.None, "None"),
        new(MouseButtonBindingKind.GameInput, "Game input"),
        new(MouseButtonBindingKind.HotbarSlot, "Hotbar slot"),
    ];

    private static readonly string[] BindingKindLabels = CreateLabels(BindingKindOptions);

    private static readonly ComboOption<VirtualKey>[] ToggleKeyOptions =
    [
        new(VirtualKey.NO_KEY, "None"),
        new(VirtualKey.F1, "F1"),
        new(VirtualKey.F2, "F2"),
        new(VirtualKey.F3, "F3"),
        new(VirtualKey.F4, "F4"),
        new(VirtualKey.F5, "F5"),
        new(VirtualKey.F6, "F6"),
        new(VirtualKey.F7, "F7"),
        new(VirtualKey.F8, "F8"),
        new(VirtualKey.F9, "F9"),
        new(VirtualKey.F10, "F10"),
        new(VirtualKey.F11, "F11"),
        new(VirtualKey.F12, "F12"),
        new(VirtualKey.INSERT, "Insert"),
        new(VirtualKey.DELETE, "Delete"),
        new(VirtualKey.HOME, "Home"),
        new(VirtualKey.END, "End"),
        new(VirtualKey.PRIOR, "Page Up"),
        new(VirtualKey.NEXT, "Page Down"),
    ];

    private static readonly string[] ToggleKeyLabels = CreateLabels(ToggleKeyOptions);

    private static readonly ComboOption<CuratedGameInput>[] GameInputOptions =
    [
        new(CuratedGameInput.TabTargetNext, "Tab target next"),
        new(CuratedGameInput.TabTargetPrevious, "Tab target previous"),
        new(CuratedGameInput.TargetNearestEnemy, "Target nearest enemy"),
        new(CuratedGameInput.MoveForward, "Move forward"),
        new(CuratedGameInput.MoveBack, "Move back"),
        new(CuratedGameInput.MoveLeft, "Move left"),
        new(CuratedGameInput.MoveRight, "Move right"),
        new(CuratedGameInput.StrafeLeft, "Strafe left"),
        new(CuratedGameInput.StrafeRight, "Strafe right"),
        new(CuratedGameInput.Jump, "Jump"),
        new(CuratedGameInput.Autorun, "Autorun"),
    ];

    private static readonly string[] GameInputLabels = CreateLabels(GameInputOptions);

    private readonly record struct ComboOption<T>(T Value, string Label);

    private static string[] CreateLabels<T>(IReadOnlyList<ComboOption<T>> options)
    {
        var labels = new string[options.Count];
        for (var index = 0; index < options.Count; index++)
        {
            labels[index] = options[index].Label;
        }

        return labels;
    }

    private static int FindOptionIndex<T>(IReadOnlyList<ComboOption<T>> options, T value)
    {
        for (var index = 0; index < options.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(options[index].Value, value))
            {
                return index;
            }
        }

        return 0;
    }
}
