namespace Eikones.Services;

public interface IFileTransferService
{
    Task<bool> MoveAsync(string sourcePath, string destinationDirectory, CancellationToken cancellationToken = default);
}
