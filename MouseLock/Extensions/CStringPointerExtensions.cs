using InteropGenerator.Runtime;
using Lumina.Text.ReadOnly;

namespace MouseLock.Extensions;

internal static class CStringPointerExtensions
{
    public static string ToPlainText(this CStringPointer text)
        => text.HasValue ? ((ReadOnlySeStringSpan)text.AsSpan()).ToString() : string.Empty;
}
