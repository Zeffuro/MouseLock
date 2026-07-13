namespace MouseLock.Configuration;

public sealed class GeneralSettings
{
    private MouseLookConditionSettings _conditions = new();
    private MouseActionSettings _mouseActions = new();
    private MouseLookCompatibilitySettings _compatibility = new();
    private ToggleKeybindSettings _toggleKeybind = new();

    public bool Enabled { get; set; } = true;
    public bool DebugEnabled { get; set; }
    public ReleaseModifierKey ReleaseModifier { get; set; } = ReleaseModifierKey.Alt;

    public MouseLookConditionSettings Conditions
    {
        get => _conditions;
        set => _conditions = value ?? new MouseLookConditionSettings();
    }

    public MouseActionSettings MouseActions
    {
        get => _mouseActions;
        set => _mouseActions = value ?? new MouseActionSettings();
    }

    public MouseLookCompatibilitySettings Compatibility
    {
        get => _compatibility;
        set => _compatibility = value ?? new MouseLookCompatibilitySettings();
    }

    public ToggleKeybindSettings ToggleKeybind
    {
        get => _toggleKeybind;
        set => _toggleKeybind = value ?? new ToggleKeybindSettings();
    }

    public void EnsureInitialized()
    {
        _conditions ??= new MouseLookConditionSettings();
        _mouseActions ??= new MouseActionSettings();
        _mouseActions.EnsureInitialized();
        _compatibility ??= new MouseLookCompatibilitySettings();
        _toggleKeybind ??= new ToggleKeybindSettings();
    }
}
