using System;

namespace MouseLock.Configuration;

public sealed class MouseActionSettings
{
    private MouseButtonGameInputBinding _leftButton = new();
    private MouseButtonGameInputBinding _leftAltButton = new();
    private MouseButtonGameInputBinding _leftControlButton = new();
    private MouseButtonGameInputBinding _leftShiftButton = new();
    private MouseButtonGameInputBinding _rightButton = new();
    private MouseButtonGameInputBinding _rightAltButton = new();
    private MouseButtonGameInputBinding _rightControlButton = new();
    private MouseButtonGameInputBinding _rightShiftButton = new();

    public MouseButtonGameInputBinding LeftButton
    {
        get => _leftButton;
        set => _leftButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding LeftAltButton
    {
        get => _leftAltButton;
        set => _leftAltButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding LeftControlButton
    {
        get => _leftControlButton;
        set => _leftControlButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding LeftShiftButton
    {
        get => _leftShiftButton;
        set => _leftShiftButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding RightButton
    {
        get => _rightButton;
        set => _rightButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding RightAltButton
    {
        get => _rightAltButton;
        set => _rightAltButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding RightControlButton
    {
        get => _rightControlButton;
        set => _rightControlButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding RightShiftButton
    {
        get => _rightShiftButton;
        set => _rightShiftButton = value ?? new MouseButtonGameInputBinding();
    }

    public void EnsureInitialized()
    {
        _leftButton ??= new MouseButtonGameInputBinding();
        _leftAltButton ??= new MouseButtonGameInputBinding();
        _leftControlButton ??= new MouseButtonGameInputBinding();
        _leftShiftButton ??= new MouseButtonGameInputBinding();
        _rightButton ??= new MouseButtonGameInputBinding();
        _rightAltButton ??= new MouseButtonGameInputBinding();
        _rightControlButton ??= new MouseButtonGameInputBinding();
        _rightShiftButton ??= new MouseButtonGameInputBinding();
        _leftButton.Clamp();
        _leftAltButton.Clamp();
        _leftControlButton.Clamp();
        _leftShiftButton.Clamp();
        _rightButton.Clamp();
        _rightAltButton.Clamp();
        _rightControlButton.Clamp();
        _rightShiftButton.Clamp();
    }
}

public sealed class MouseButtonGameInputBinding
{
    public MouseButtonBindingKind Kind { get; set; }
    public CuratedGameInput GameInput { get; set; } = CuratedGameInput.TabTargetNext;
    public int Hotbar { get; set; } = 1;
    public int Slot { get; set; } = 1;

    public void Clamp()
    {
        Hotbar = Math.Clamp(Hotbar, 1, 10);
        Slot = Math.Clamp(Slot, 1, 12);
    }
}

public enum MouseButtonBindingKind
{
    None = 0,
    GameInput = 1,
    HotbarSlot = 2,
    TemporaryRelease = 3,
    ToggleMouseLock = 4,
    OpenConfig = 5,
}

public enum CuratedGameInput
{
    TabTargetNext = 0,
    TabTargetPrevious = 1,
    TargetNearestEnemy = 2,
    MoveForward = 3,
    MoveBack = 4,
    MoveLeft = 5,
    MoveRight = 6,
    StrafeLeft = 7,
    StrafeRight = 8,
    Jump = 9,
    Autorun = 10,
}
