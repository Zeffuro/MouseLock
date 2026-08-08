using System;
using System.Text.Json.Serialization;

namespace MouseLock.Configuration;

public sealed class GeneralSettings
{
    private bool? _legacyStickyReleaseEnabled;

    public bool Enabled { get; set; } = true;
    public bool FirstRunIntroCompleted { get; set; }
    public bool DebugEnabled { get; set; }
    public ReleaseModifierKey ReleaseModifier { get; set; } = ReleaseModifierKey.Alt;
    public bool RestoreCursorPositionOnRelease { get; set; } = true;
    public ReleaseModifierTapBehavior ReleaseModifierTapBehavior { get; set; } = ReleaseModifierTapBehavior.UntilWorldClick;

    [Obsolete("Use ReleaseModifierTapBehavior.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StickyReleaseEnabled
    {
        get => null;
        set => _legacyStickyReleaseEnabled = value;
    }

    internal void MigrateStickyReleaseEnabled()
    {
        if (!_legacyStickyReleaseEnabled.HasValue)
        {
            return;
        }

        ReleaseModifierTapBehavior = _legacyStickyReleaseEnabled.Value
            ? ReleaseModifierTapBehavior.UntilWorldClick
            : ReleaseModifierTapBehavior.None;
        _legacyStickyReleaseEnabled = null;
    }
}
