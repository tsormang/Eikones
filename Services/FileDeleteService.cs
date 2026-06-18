using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace Eikones.Services;

public sealed class FileDeleteService : IFileDeleteService
{
    public Task<bool> DeleteToRecycleBinAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                FileSystem.DeleteFile(
                    filePath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }
}
