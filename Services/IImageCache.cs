using System.Windows.Media.Imaging;

namespace Eikones.Services;

public interface IImageCache
{
    bool TryGet(string key, out BitmapSource? image);
    void Set(string key, BitmapSource image);
    void Remove(string key);
}
