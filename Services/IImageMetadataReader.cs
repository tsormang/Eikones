namespace Eikones.Services;

public interface IImageMetadataReader
{
    Task<DateTime?> GetDateTakenAsync(string path, CancellationToken cancellationToken = default);
}
