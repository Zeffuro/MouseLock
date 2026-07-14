using System;

namespace MouseLock.Configuration;

public sealed class MouseLookActivationSettings
{
    private MouseLookConditionSettings _conditions = new();

    public MouseLookResumePolicy ResumePolicy { get; set; } = MouseLookResumePolicy.Immediate;
    public int ResumeDelayMilliseconds { get; set; } = 300;

    public MouseLookConditionSettings Conditions
    {
        get => _conditions;
        set => _conditions = value ?? new MouseLookConditionSettings();
    }

    public void EnsureInitialized()
    {
        _conditions ??= new MouseLookConditionSettings();
        _conditions.EnsureInitialized();
        ResumeDelayMilliseconds = Math.Clamp(ResumeDelayMilliseconds, 100, 2_000);
    }
}
