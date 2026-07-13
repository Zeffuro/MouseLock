namespace MouseLock.Configuration;

public sealed class DtrSettings
{
    public bool Enabled { get; set; } = true;
    public DtrTextMode TextMode { get; set; } = DtrTextMode.Simple;
}
