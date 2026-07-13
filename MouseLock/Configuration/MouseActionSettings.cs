using System;

namespace MouseLock.Configuration;

public sealed class MouseActionSettings
{
    private MouseButtonGameInputBinding _leftButton = new();
    private MouseButtonGameInputBinding _rightButton = new();

    public MouseButtonGameInputBinding LeftButton
    {
        get => _leftButton;
        set => _leftButton = value ?? new MouseButtonGameInputBinding();
    }

    public MouseButtonGameInputBinding RightButton
    {
        get => _rightButton;
        set => _rightButton = value ?? new MouseButtonGameInputBinding();
    }

    public void EnsureInitialized()
    {
        _leftButton ??= new MouseButtonGameInputBinding();
        _rightButton ??= new MouseButtonGameInputBinding();
        _leftButton.Clamp();
        _rightButton.Clamp();
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
