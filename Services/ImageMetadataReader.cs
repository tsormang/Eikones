using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

    private static readonly Regex DayMonthYearPattern = new(
        @"^\d{1,2}/\d{1,2}/\d{4}$",
        RegexOptions.CultureInvariant);

    public Task<string?> GetDateTakenDisplayAsync(string path, CancellationToken cancellationToken = default) =>
        Task.Run(() => ReadDateTakenDisplay(path), cancellationToken);

    private static string? ReadDateTakenDisplay(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var fromBitmap = ReadDateTakenDisplayFromBitmapMetadata(path);
            if (!string.IsNullOrWhiteSpace(fromBitmap))
            {
                return fromBitmap;
            }
        }
        catch
        {
            // Fall through to shell properties.
        }

        return WindowsShellProperties.TryGetDateTakenDisplay(path);
    }

    private static string? ReadDateTakenDisplayFromBitmapMetadata(string path)
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
            && TryFormatRawDateString(metadata.DateTaken, out var fromProperty))
        {
            return fromProperty;
        }

        foreach (var query in DateTakenQueries)
        {
            if (metadata.GetQuery(query) is { } rawValue
                && TryFormatQueryValue(rawValue, out var formatted))
            {
                return formatted;
            }
        }

        return null;
    }

    private static bool TryFormatQueryValue(object rawValue, out string? formatted)
    {
        formatted = null;

        var rawDate = rawValue switch
        {
            string text => text,
            byte[] bytes => Encoding.ASCII.GetString(bytes).TrimEnd('\0'),
            DateTime dateTime => dateTime.ToString("yyyy:MM:dd HH:mm:ss"),
            _ => rawValue.ToString()
        };

        return !string.IsNullOrWhiteSpace(rawDate) && TryFormatRawDateString(rawDate, out formatted);
    }

    private static bool TryFormatRawDateString(string rawDate, out string? formatted)
    {
        formatted = null;
        var trimmed = rawDate.Trim();
        var datePart = trimmed.Split([' ', 'T'], StringSplitOptions.RemoveEmptyEntries)[0];

        if (DayMonthYearPattern.IsMatch(datePart))
        {
            formatted = datePart;
            return true;
        }

        if (TryReorderDateParts(datePart, ':', out formatted)
            || TryReorderDateParts(datePart, '-', out formatted))
        {
            return true;
        }

        return false;
    }

    private static bool TryReorderDateParts(string datePart, char separator, out string? formatted)
    {
        formatted = null;
        var parts = datePart.Split(separator);
        if (parts.Length < 3
            || parts[0].Length != 4
            || !int.TryParse(parts[0], out _)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var day))
        {
            return false;
        }

        formatted = $"{day:D2}/{month:D2}/{parts[0]}";
        return true;
    }
}
