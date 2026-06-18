using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;

namespace Eikones.Services;

public sealed class ThumbnailService : IThumbnailService
{
    private readonly IImageCache _cache;
    private readonly SemaphoreSlim _decodeGate = new(4);
    private readonly ConcurrentDictionary<string, Task<BitmapSource?>> _inflight = new();

    public ThumbnailService(IImageCache cache)
    {
        _cache = cache;
    }

    public Task<BitmapSource?> GetThumbnailAsync(string path, int width, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"thumb_{width}_{path}";
        if (_cache.TryGet(cacheKey, out var cached))
        {
            return Task.FromResult(cached);
        }

        return _inflight.GetOrAdd(cacheKey, _ => LoadInternalAsync(path, width, cacheKey, cancellationToken))
            .ContinueWith(t =>
            {
                _inflight.TryRemove(cacheKey, out _);
                return t.Result;
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task<BitmapSource?> LoadInternalAsync(
        string path,
        int width,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        await _decodeGate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGet(cacheKey, out var cached))
            {
                return cached;
            }

            var bitmap = await Task.Run(() => DecodeThumbnail(path, width), cancellationToken);
            if (bitmap is not null)
            {
                _cache.Set(cacheKey, bitmap);
            }

            return bitmap;
        }
        finally
        {
            _decodeGate.Release();
        }
    }

    private static BitmapSource? DecodeThumbnail(string path, int width)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            var frame = decoder.Frames[0];

            if (frame.Thumbnail is { } embedded)
            {
                if (embedded.CanFreeze)
                {
                    embedded.Freeze();
                }

                return embedded;
            }

            stream.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.DecodePixelWidth = width;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
