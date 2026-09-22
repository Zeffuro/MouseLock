using System;
using System.Numerics;

namespace MouseLock.Configuration;

public enum AimTargetMode
{
    Soft = 0,
    Focus = 2,
}

public enum ReticleStyle
{
    Crosshair = 1,
    GameIcon,
    Preset,
    AirForceOne,
}

[Flags]
public enum AimTargetKinds
{
    None = 0,
    Enemies = 1,
    FriendlyPlayers = 2,
    FriendlyNpcs = 4,
    OtherObjects = 8,
    All = Enemies | FriendlyPlayers | FriendlyNpcs | OtherObjects,
}

public sealed class TargetingSettings
{
    public bool Enabled { get; set; }
    public bool OnlyInCombat { get; set; }
    public AimTargetMode Mode { get; set; } = AimTargetMode.Soft;
    public AimTargetKinds TargetKinds { get; set; } = AimTargetKinds.Enemies;
    public bool IncludeDeadTargets { get; set; }
    public float AimTolerance { get; set; } = 20;
    public bool KeepSoftTargetOnLookAway { get; set; } = true;
    public bool KeepSoftTargetAfterAction { get; set; } = true;
    public bool ShowReticle { get; set; }
    public bool ReticleDepthEnabled { get; set; } = true;
    public float ReticleDistance { get; set; } = 10;
    public ReticleStyle ReticleStyle { get; set; } = ReticleStyle.Preset;
    public float VerticalOffset { get; set; } = 10;
    public float ReticleSize { get; set; } = 30;
    public uint ReticleIconId { get; set; } = 60401;
    public int ReticleDesign { get; set; } = 1;
    public int NormalVariant { get; set; } = 2;
    public int TargetVariant { get; set; } = 3;
    public Vector4 ReticleColor { get; set; } = Vector4.One;
    public Vector4 TargetReticleColor { get; set; } = new(1, 0.45f, 0.35f, 1);
    public float ReticleRotation { get; set; }
    public bool AnimateReticle { get; set; } = true;

    public void EnsureInitialized()
    {
        if (!Enum.IsDefined(Mode)) Mode = AimTargetMode.Soft;
        if (!Enum.IsDefined(ReticleStyle)) ReticleStyle = ReticleStyle.Preset;
        TargetKinds &= AimTargetKinds.All;
        AimTolerance = float.IsFinite(AimTolerance) ? Math.Clamp(AimTolerance, 0, 60) : 20;
        VerticalOffset = float.IsFinite(VerticalOffset) ? Math.Clamp(VerticalOffset, -45, 45) : 10;
        ReticleSize = float.IsFinite(ReticleSize) ? Math.Clamp(ReticleSize, 4, 160) : 30;
        ReticleDistance = float.IsFinite(ReticleDistance) ? Math.Clamp(ReticleDistance, 0.1f, 100) : 10;
        ReticleIconId = Math.Clamp(ReticleIconId, 1u, 999999u);
        ReticleDesign = Math.Clamp(ReticleDesign, 1, 6);
        NormalVariant = Math.Clamp(NormalVariant, 0, 3);
        TargetVariant = Math.Clamp(TargetVariant, 0, 3);
        ReticleColor = ClampColor(ReticleColor, Vector4.One);
        TargetReticleColor = ClampColor(TargetReticleColor, new Vector4(1, 0.45f, 0.35f, 1));
        ReticleRotation = float.IsFinite(ReticleRotation) ? Math.Clamp(ReticleRotation, -180, 180) : 0;
    }

    private static Vector4 ClampColor(Vector4 color, Vector4 fallback)
        => float.IsFinite(color.X) && float.IsFinite(color.Y) && float.IsFinite(color.Z) && float.IsFinite(color.W)
            ? Vector4.Clamp(color, Vector4.Zero, Vector4.One)
            : fallback;
}
