namespace MouseLock.Configuration;

public sealed class GeneralSettings
{
    public bool Enabled { get; set; } = true;
    public bool DebugEnabled { get; set; }
    public ReleaseModifierKey ReleaseModifier { get; set; } = ReleaseModifierKey.Alt;
    public bool RestoreCursorPositionOnRelease { get; set; } = true;
    public bool StickyReleaseEnabled { get; set; }
}
