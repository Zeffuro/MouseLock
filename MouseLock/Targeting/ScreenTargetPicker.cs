using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using MouseLock.Configuration;

namespace MouseLock.Targeting;

internal static unsafe class ScreenTargetPicker
{
    internal static GameObject* Pick(TargetSystem* targets, Vector2 point, TargetingSettings settings)
    {
        var control = Control.Instance();
        var player = Control.GetLocalPlayer();
        if (control == null || control->CameraManager.Camera == null || player == null)
        {
            return null;
        }

        var candidates = new GameObjectArray();
        var source = &targets->ObjectFilterArray1;
        var count = Math.Clamp(source->Length, 0, source->Objects.Length);
        for (var i = 0; i < count; i++)
        {
            var candidate = source->Objects[i].Value;
            if (candidate == player || !IsEligible(candidate, settings))
            {
                continue;
            }

            candidates.Objects[candidates.Length++] = candidate;
        }

        if (candidates.Length == 0) return null;

        var camera = control->CameraManager.Camera;
        var exact = targets->GetMouseOverObject((int)point.X, (int)point.Y, &candidates, camera);
        if (exact != null || settings.AimTolerance <= 0) return exact;
        if (!TargetingPosition.TryGet(0, out _, out var screenSize)) return null;

        Span<Vector2> samples = stackalloc Vector2[AimTolerance.SampleCount];
        AimTolerance.GetSamples(point, settings.AimTolerance, samples);
        foreach (var sample in samples)
        {
            if (sample == point) continue;
            if (sample.X < 0 || sample.Y < 0 || sample.X >= screenSize.X || sample.Y >= screenSize.Y)
                continue;

            var target = targets->GetMouseOverObject((int)sample.X, (int)sample.Y, &candidates, camera);
            if (target != null) return target;
        }
        return null;
    }

    internal static bool IsEligible(GameObject* candidate, TargetingSettings settings)
        => candidate != null && candidate->GetIsTargetable() &&
           (settings.IncludeDeadTargets || !candidate->IsDead()) && Matches(candidate, settings.TargetKinds);

    private static bool Matches(GameObject* candidate, AimTargetKinds kinds)
    {
        if (kinds == AimTargetKinds.All) return true;
        if (kinds == AimTargetKinds.None) return false;

        // Use a random action like Stone to get if it's targetable
        var kind = ActionManager.CanUseActionOnTarget(142, candidate) ? AimTargetKinds.Enemies : candidate->ObjectKind switch
        {
            ObjectKind.Pc => AimTargetKinds.FriendlyPlayers,
            ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion => AimTargetKinds.FriendlyNpcs,
            _ => AimTargetKinds.OtherObjects,
        };
        return (kinds & kind) != 0;
    }
}
