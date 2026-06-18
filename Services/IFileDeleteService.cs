namespace Eikones.Services;

public interface IFileDeleteService
{
    Task<bool> DeleteToRecycleBinAsync(string filePath, CancellationToken cancellationToken = default);
}
