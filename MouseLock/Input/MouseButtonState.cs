namespace MouseLock.Input;

internal readonly record struct MouseButtonState(
    bool Pressed,
    bool Held,
    bool Released,
    bool AllowNewActions);
