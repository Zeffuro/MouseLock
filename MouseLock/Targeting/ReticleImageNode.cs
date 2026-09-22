using System.Numerics;
using Dalamud.Interface.Textures;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace MouseLock.Targeting;

internal sealed class ReticleImageNode : ImGuiImageNode
{
    private string? _loadedSource;

    internal ReticleImageNode()
    {
        WrapMode = WrapMode.Stretch;
    }

    internal bool ShowFile(string fileName)
    {
        var path = ReticleAssets.PathFor(fileName);
        return Load(path, Service.TextureProvider.GetFromFile(path));
    }

    internal bool ShowIcon(uint iconId)
        => Load($"icon/{iconId}", Service.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)));

    internal bool ShowAirForceRing()
    {
        const string path = "ui/uld/RideShooting.tex";
        if (!Load(path, Service.TextureProvider.GetFromGame(path))) return false;
        TextureCoordinates = new Vector2(252, 264);
        TextureSize = new Vector2(68);
        return true;
    }

    private bool Load(string source, ISharedImmediateTexture texture)
    {
        if (_loadedSource == source) return true;
        var wrap = texture.GetWrapOrDefault();
        if (wrap == null) return false;

        LoadTexture(wrap.CreateWrapSharingLowLevelResource());
        TextureCoordinates = Vector2.Zero;
        TextureSize = wrap.Size;
        _loadedSource = source;
        return true;
    }
}
