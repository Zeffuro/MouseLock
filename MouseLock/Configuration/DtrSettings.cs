namespace MouseLock.Configuration;

public sealed class DtrSettings
{
    public bool Enabled { get; set; }
    public string Text { get; set; } = "MouseLock";
    public string Tooltip { get; set; } = "Click to toggle MouseLock.";
    public bool ShowWhenLoggedOut { get; set; }
    public bool ClickToOpenConfig { get; set; } = true;
}