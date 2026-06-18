using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;

namespace Eikones.Services;

public sealed class ImageMetadataReader : IImageMetadataReader
{
    private static readonly string[] DateTakenQueries =
    [
        "/app1/exif/{ushort=36867}", // DateTimeOriginal
        "/app1/exif/{ushort=36868}", // DateTimeDigitized
        "/app1/exif/{ushort=306}"    // DateTime
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
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            if (decoder.Frames.Count == 0)
            {
                return null;
            }

            if (decoder.Frames[0].Metadata is not BitmapMetadata metadata)
            {
                return null;
            }

            foreach (var query in DateTakenQueries)
            {
                if (metadata.GetQuery(query) is string rawDate && TryParseExifDate(rawDate, out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool TryParseExifDate(string rawDate, out DateTime result)
    {
        var formats = new[]
        {
            "yyyy:MM:dd HH:mm:ss",
            "yyyy:MM:dd HH:mm:ssK",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK"
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
