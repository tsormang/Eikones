using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace Eikones.Services;

public sealed class ImageMetadataReader : IImageMetadataReader
{
    private static readonly string[] DateTakenQueries =
    [
        "/app1/ifd/exif/{ushort=36867}", // DateTimeOriginal
        "/app1/ifd/exif/{ushort=36868}", // DateTimeDigitized
        "/app1/ifd/{ushort=306}",        // DateTime
        "/app1/exif/{ushort=36867}",
        "/app1/exif/{ushort=36868}",
        "/app1/exif/{ushort=306}",
        "/xmp/exif:DateTimeOriginal",
        "/xmp/xmp:CreateDate"
    ];

    public Task<DateTime?> GetDateTakenAsync(string path, CancellationToken cancellationToken = default) =>
        Task.Run(() => ReadDateTaken(path), cancellationToken);

    private static DateTime? ReadDateTaken(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var fromBitmap = ReadDateTakenFromBitmapMetadata(path);
            if (fromBitmap.HasValue)
            {
                return fromBitmap;
            }
        }
        catch
        {
            // Fall through to shell properties.
        }

        return WindowsShellProperties.TryGetDateTaken(path);
    }

    private static DateTime? ReadDateTakenFromBitmapMetadata(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
        if (decoder.Frames.Count == 0)
        {
            return null;
        }

        if (decoder.Frames[0].Metadata is not BitmapMetadata metadata)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(metadata.DateTaken)
            && TryParseExifDate(metadata.DateTaken, out var fromProperty))
        {
            return fromProperty;
        }

        foreach (var query in DateTakenQueries)
        {
            if (metadata.GetQuery(query) is { } rawValue
                && TryParseQueryValue(rawValue, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryParseQueryValue(object rawValue, out DateTime result)
    {
        result = default;

        var rawDate = rawValue switch
        {
            string text => text,
            byte[] bytes => Encoding.ASCII.GetString(bytes).TrimEnd('\0'),
            DateTime dateTime => dateTime.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture),
            _ => rawValue.ToString()
        };

        return !string.IsNullOrWhiteSpace(rawDate) && TryParseExifDate(rawDate, out result);
    }

    private static bool TryParseExifDate(string rawDate, out DateTime result)
    {
        var formats = new[]
        {
            "yyyy:MM:dd HH:mm:ss",
            "yyyy:MM:dd HH:mm:ssK",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffK"
        };

        return DateTime.TryParseExact(
                   rawDate.Trim(),
                   formats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                   out result)
               || DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result);
    }
}
