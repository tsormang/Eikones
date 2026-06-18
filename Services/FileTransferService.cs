using System.IO;

namespace Eikones.Services;

public sealed class FileTransferService : IFileTransferService
{
    public async Task<bool> MoveAsync(string sourcePath, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(destinationDirectory);
            var destPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));

            await Task.Run(() => MoveFile(sourcePath, destinationDirectory, destPath), cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void MoveFile(string sourcePath, string destinationDirectory, string destPath)
    {
        var sourceRoot = Path.GetPathRoot(sourcePath);
        var destRoot = Path.GetPathRoot(destinationDirectory);

        if (sourceRoot is not null && destRoot is not null &&
            sourceRoot.Equals(destRoot, StringComparison.OrdinalIgnoreCase))
        {
            File.Move(sourcePath, destPath, overwrite: true);
            return;
        }

        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            sourceStream.CopyTo(destStream);
        }

        File.Delete(sourcePath);
    }
}
