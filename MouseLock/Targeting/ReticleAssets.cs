using System.IO;

namespace MouseLock.Targeting;

internal static class ReticleAssets
{
    internal static readonly uint[] IconIds = [60401, 60402, 60403, 60422, 60423, 60424, 60444, 61708];

    internal static string PresetFileName(int design, int variant)
    {
        var suffix = variant switch
        {
            1 => "open",
            2 => "ring",
            3 when design != 4 => "alt",
            3 => "ring",
            _ => "base",
        };
        return $"{design}-{suffix}.png";
    }

    internal static string PathFor(string fileName)
        => Path.Combine(Service.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "Reticles", fileName);
}
