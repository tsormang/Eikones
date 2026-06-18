using Eikones.Models;

namespace Eikones.Services;

public interface IFileScanner
{
    Task<IReadOnlyList<ImageFileEntry>> EnumerateAsync(
        string folderPath,
        IEnumerable<string> extensions,
        CancellationToken cancellationToken = default);
}
