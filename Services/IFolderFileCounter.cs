namespace Eikones.Services;

public interface IFolderFileCounter
{
    Task<IReadOnlyDictionary<string, int>> CountByExtensionAsync(
        string folderPath,
        CancellationToken cancellationToken = default);
}
