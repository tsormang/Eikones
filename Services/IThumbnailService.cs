using System.Windows.Media.Imaging;

namespace Eikones.Services;

public interface IThumbnailService
{
    Task<BitmapSource?> GetThumbnailAsync(string path, int width, CancellationToken cancellationToken = default);
}
