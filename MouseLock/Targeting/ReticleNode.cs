using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;
using KamiToolKit.UiOverlay;
using MouseLock.Configuration;
using MouseLock.MouseLook;

namespace MouseLock.Targeting;

internal sealed unsafe class ReticleNode : OverlayNode
{
    private readonly ResNode _contentNode = new();
    private readonly ReticleImageNode _imageNode = new();
    private readonly CrosshairNode _crosshairNode = new();
    private readonly NodeBase[] _depthNodes;
    private bool? _depthEnabled;

    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    internal ReticleNode()
    {
        _depthNodes = [_imageNode, .. _crosshairNode.Parts];
        _contentNode.AttachNode(this);
        _imageNode.AttachNode(_contentNode);
        _crosshairNode.AttachNode(_contentNode);
        BuildTimeline();
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        Origin = Size / 2;
        _contentNode.Size = Size;
        _contentNode.Origin = Origin;
        _imageNode.Size = Size;
        _crosshairNode.Size = Size;
    }

    protected override void OnUpdate()
    {
        var settings = PluginState.Config.Targeting;
        var preview = PluginState.ConfigWindow.IsTargetingTabOpen;
        var active = PluginState.MouseLookService?.Status.Kind == MouseLookStatusKind.Active;
        IsVisible = settings.ShowReticle && (preview || active);
        if (!IsVisible) return;
        if (!TargetingPosition.TryGet(settings.VerticalOffset, out var point, out _))
        {
            IsVisible = false;
            return;
        }

        var hasTarget = preview
            ? PluginState.ConfigWindow.PreviewReticleTarget
            : PluginState.TargetingService?.HasCandidate == true;
        Size = new Vector2(settings.ReticleSize * 2);
        Position = point - Size / 2;
        RotationDegrees = settings.ReticleRotation;
        UpdateDepth(settings);
        UpdateAppearance(settings, hasTarget);
        Timeline?.PlayAnimation(!settings.AnimateReticle ? 3 : hasTarget ? 1 : 2);
    }

    private void UpdateDepth(TargetingSettings settings)
    {
        var depth = 0f;
        var enabled = settings.ReticleDepthEnabled && ReticleDepth.TryGet(settings.ReticleDistance, out depth);
        if (_depthEnabled == enabled && Node->Depth == depth) return;

        if (_depthEnabled != enabled)
        {
            foreach (var child in _depthNodes)
            {
                child.NodeFlags = enabled
                    ? child.NodeFlags | NodeFlags.UseDepthBasedPriority
                    : child.NodeFlags & ~NodeFlags.UseDepthBasedPriority;
            }
            _depthEnabled = enabled;
        }

        Node->Depth = depth;
        MarkDirty();
    }

    private void UpdateAppearance(TargetingSettings settings, bool hasTarget)
    {
        var color = hasTarget ? settings.TargetReticleColor : settings.ReticleColor;
        _crosshairNode.Color = color;
        _imageNode.Color = color;
        _crosshairNode.IsVisible = settings.ReticleStyle == ReticleStyle.Crosshair;
        _imageNode.IsVisible = settings.ReticleStyle switch
        {
            ReticleStyle.GameIcon => _imageNode.ShowIcon(settings.ReticleIconId),
            ReticleStyle.AirForceOne => _imageNode.ShowAirForceRing(),
            ReticleStyle.Preset => _imageNode.ShowFile(ReticleAssets.PresetFileName(
                settings.ReticleDesign, hasTarget ? settings.TargetVariant : settings.NormalVariant)),
            _ => false,
        };
    }

    private void BuildTimeline()
    {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 16)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(7, 1, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(8, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(14, 2, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(15, 3, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(16, 3, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet().Build());
        _contentNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 7)
            .AddFrame(1, rotation: 0, scale: Vector2.One)
            .AddFrame(4, rotation: MathF.PI / 8, scale: new Vector2(1.2f))
            .AddFrame(7, rotation: MathF.PI / 4, scale: new Vector2(1.1f))
            .EndFrameSet()
            .BeginFrameSet(8, 14)
            .AddFrame(8, rotation: MathF.PI / 4, scale: new Vector2(1.1f))
            .AddFrame(14, rotation: 0, scale: Vector2.One)
            .EndFrameSet()
            .BeginFrameSet(15, 16)
            .AddFrame(15, rotation: 0, scale: Vector2.One)
            .EndFrameSet().Build());
    }
}
