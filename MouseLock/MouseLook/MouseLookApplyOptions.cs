namespace MouseLock.MouseLook;

internal readonly record struct MouseLookApplyOptions(
    bool ApplyCursorOverlayCompatibility,
    bool RememberCursorPosition,
    bool HideCursorOverlayPlugins);
