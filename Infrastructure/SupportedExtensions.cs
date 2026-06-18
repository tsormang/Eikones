using System.IO;

namespace Eikones.Infrastructure;

public static class SupportedExtensions
{
    public static readonly string[] Default =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    ];

    public static bool IsSupported(string filePath, IEnumerable<string> extensions)
    {
        var ext = Path.GetExtension(filePath);
        return extensions.Any(supported =>
            ext.Equals(supported, StringComparison.OrdinalIgnoreCase));
    }
}
