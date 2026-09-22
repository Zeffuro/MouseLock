using System.Collections.Generic;
using System.Numerics;
using KamiToolKit.Nodes;

namespace MouseLock.Targeting;

internal sealed class CrosshairNode : ResNode
{
    private readonly ColorImageNode _horizontalBorder = new();
    private readonly ColorImageNode _verticalBorder = new();
    private readonly ColorImageNode _horizontal = new();
    private readonly ColorImageNode _vertical = new();

    internal IReadOnlyList<ColorImageNode> Parts { get; }

    internal CrosshairNode()
    {
        Parts = [_horizontalBorder, _verticalBorder, _horizontal, _vertical];
        foreach (var part in Parts)
        {
            part.AttachNode(this);
        }
    }

    public override Vector4 Color
    {
        get => _horizontal.Color;
        set
        {
            _horizontal.Color = value;
            _vertical.Color = value;
            _horizontalBorder.Color = new Vector4(0, 0, 0, value.W * 0.85f);
            _verticalBorder.Color = _horizontalBorder.Color;
        }
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        _horizontal.Size = new Vector2(Width, 2);
        _vertical.Size = new Vector2(2, Height);
        _horizontalBorder.Size = new Vector2(Width, 4);
        _verticalBorder.Size = new Vector2(4, Height);
        _horizontal.Position = (Size - _horizontal.Size) / 2;
        _vertical.Position = (Size - _vertical.Size) / 2;
        _horizontalBorder.Position = (Size - _horizontalBorder.Size) / 2;
        _verticalBorder.Position = (Size - _verticalBorder.Size) / 2;
    }
}
