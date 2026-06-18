using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using Eikones.Services;

namespace Eikones.Infrastructure;

public sealed class ImageCache : IImageCache
{
    private const int MaxEntries = 200;
    private readonly ConcurrentDictionary<string, BitmapSource> _cache = new();
    private readonly ConcurrentQueue<string> _order = new();

    public bool TryGet(string key, out BitmapSource? image) => _cache.TryGetValue(key, out image);

    public void Set(string key, BitmapSource image)
    {
        _cache[key] = image;
        _order.Enqueue(key);

        while (_order.Count > MaxEntries && _order.TryDequeue(out var oldest))
        {
            _cache.TryRemove(oldest, out _);
        }
    }

    public void Remove(string key) => _cache.TryRemove(key, out _);
}
