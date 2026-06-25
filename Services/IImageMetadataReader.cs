namespace Eikones.Services;

public interface IImageMetadataReader
{
    Task<string?> GetDateTakenDisplayAsync(string path, CancellationToken cancellationToken = default);
}
