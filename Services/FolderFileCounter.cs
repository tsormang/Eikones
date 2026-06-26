using System.IO;

namespace Eikones.Services;

public sealed class FolderFileCounter : IFolderFileCounter
{
    public Task<IReadOnlyDictionary<string, int>> CountByExtensionAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => CountByExtension(folderPath, cancellationToken), cancellationToken);
    }

    private static IReadOnlyDictionary<string, int> CountByExtension(
        string folderPath,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folderPath))
        {
            return new Dictionary<string, int>();
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(folderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(filePath);
            counts.TryGetValue(extension, out var count);
            counts[extension] = count + 1;
        }

        return counts;
    }
}
