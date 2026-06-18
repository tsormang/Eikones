using Eikones.Infrastructure;
using Eikones.Models;
using System.IO;

namespace Eikones.Services;

public sealed class FileScanner : IFileScanner
{
    public Task<IReadOnlyList<ImageFileEntry>> EnumerateAsync(
        string folderPath,
        IEnumerable<string> extensions,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Enumerate(folderPath, extensions, cancellationToken), cancellationToken);
    }

    private static IReadOnlyList<ImageFileEntry> Enumerate(
        string folderPath,
        IEnumerable<string> extensions,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folderPath))
        {
            return Array.Empty<ImageFileEntry>();
        }

        var extensionSet = extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = new List<ImageFileEntry>();

        foreach (var filePath in Directory.EnumerateFiles(folderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ext = Path.GetExtension(filePath);
            if (!extensionSet.Contains(ext))
            {
                continue;
            }

            try
            {
                var info = new FileInfo(filePath);
                results.Add(new ImageFileEntry
                {
                    FilePath = info.FullName,
                    FileName = info.Name,
                    SizeBytes = info.Length,
                    ModifiedUtc = info.LastWriteTimeUtc
                });
            }
            catch
            {
                // Skip inaccessible files.
            }
        }

        results.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));
        return results;
    }
}
