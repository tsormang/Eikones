using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;

namespace Eikones.Services;

public sealed class PreviewLoader : IPreviewLoader
{
    private readonly IImageCache _cache;
    private readonly SemaphoreSlim _decodeGate = new(2);
    private readonly ConcurrentDictionary<string, Task<BitmapSource?>> _inflight = new();

    public PreviewLoader(IImageCache cache)
    {
        _cache = cache;
    }

    public Task<BitmapSource?> LoadPreviewAsync(string path, int maxEdge, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"preview_{maxEdge}_{path}";
        if (_cache.TryGet(cacheKey, out var cached))
        {
            return Task.FromResult(cached);
        }

        return _inflight.GetOrAdd(cacheKey, _ => LoadInternalAsync(path, maxEdge, cacheKey, cancellationToken))
            .ContinueWith(t =>
            {
                _inflight.TryRemove(cacheKey, out _);
                return t.Result;
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task<BitmapSource?> LoadInternalAsync(
        string path,
        int maxEdge,
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

            var bitmap = await Task.Run(() => DecodePreview(path, maxEdge), cancellationToken);
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

    private static BitmapSource? DecodePreview(string path, int maxEdge)
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
            var pixelWidth = frame.PixelWidth;
            var pixelHeight = frame.PixelHeight;

            stream.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;

            if (pixelWidth >= pixelHeight)
            {
                bitmap.DecodePixelWidth = maxEdge;
            }
            else
            {
                bitmap.DecodePixelHeight = maxEdge;
            }

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
