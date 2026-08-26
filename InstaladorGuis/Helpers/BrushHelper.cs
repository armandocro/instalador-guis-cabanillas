using System.Windows.Media;

namespace InstaladorGuis.Helpers;

internal static class BrushHelper
{
    private static readonly Dictionary<string, Brush> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Brush FromHex(string hex)
    {
        if (Cache.TryGetValue(hex, out var cached))
            return cached;

        var brush = (Brush)new BrushConverter().ConvertFrom(hex)!;
        brush.Freeze();
        Cache[hex] = brush;
        return brush;
    }
}
