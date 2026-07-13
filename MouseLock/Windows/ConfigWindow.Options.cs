using System.Collections.Generic;
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

    private static readonly ComboOption<MouseLookResumePolicy>[] ResumePolicyOptions =
    [
        new(MouseLookResumePolicy.Immediate, "Immediately"),
        new(MouseLookResumePolicy.Delay, "After a short delay"),
        new(MouseLookResumePolicy.WorldClick, "On next world click"),
    ];

    private static readonly string[] ResumePolicyLabels = CreateLabels(ResumePolicyOptions);

    private static readonly ComboOption<MouseButtonBindingKind>[] BindingKindOptions =
    [
        new(MouseButtonBindingKind.None, "None"),
        new(MouseButtonBindingKind.GameInput, "Game input"),
        new(MouseButtonBindingKind.HotbarSlot, "Hotbar slot"),
        new(MouseButtonBindingKind.TemporaryRelease, "Temporary release"),
        new(MouseButtonBindingKind.ToggleMouseLock, "Toggle MouseLock"),
        new(MouseButtonBindingKind.OpenConfig, "Open config"),
    ];

    private static readonly string[] BindingKindLabels = CreateLabels(BindingKindOptions);

    private static readonly ComboOption<DtrTextMode>[] DtrTextModeOptions =
    [
        new(DtrTextMode.Simple, "Simple: MouseLock: Paused"),
        new(DtrTextMode.Detailed, "Detailed: MouseLock: Paused: Reason"),
        new(DtrTextMode.Compact, "Compact: ML: Paused"),
    ];

    private static readonly string[] DtrTextModeLabels = CreateLabels(DtrTextModeOptions);

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

    private static string GetReleaseModifierLabel(ReleaseModifierKey modifier)
        => ReleaseModifierOptions[FindOptionIndex(ReleaseModifierOptions, modifier)].Label;
}
