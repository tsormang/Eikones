using System.Windows.Media.Imaging;

namespace Eikones.Services;

public interface IPreviewLoader
{
    Task<BitmapSource?> LoadPreviewAsync(string path, int maxEdge, CancellationToken cancellationToken = default);
}
