using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eikones.Infrastructure;
using Eikones.Models;
using Eikones.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Eikones.ViewModels;

public partial class SourceBrowserViewModel : ObservableObject
{
    private readonly IFileScanner _fileScanner;
    private readonly IThumbnailService _thumbnailService;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _scanCts;
    private string[] _extensions = SupportedExtensions.Default;

    public SourceBrowserViewModel(IFileScanner fileScanner, IThumbnailService thumbnailService)
    {
        _fileScanner = fileScanner;
        _thumbnailService = thumbnailService;
        _dispatcher = Application.Current.Dispatcher;
    }

    public ObservableCollection<ImageItemViewModel> Images { get; } = [];

    [ObservableProperty]
    private string? _folderPath;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "No source folder selected.";

    public void Configure(string[] extensions) => _extensions = extensions;

    public async Task LoadFolderAsync(string? folderPath)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        FolderPath = folderPath;
        Images.Clear();

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            StatusMessage = "No source folder selected.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Scanning folder...";

        try
        {
            var entries = await _fileScanner.EnumerateAsync(folderPath, _extensions, token);
            token.ThrowIfCancellationRequested();

            await _dispatcher.InvokeAsync(() =>
            {
                foreach (var entry in entries)
                {
                    Images.Add(new ImageItemViewModel(
                        entry.FilePath,
                        entry.FileName,
                        entry.SizeBytes,
                        entry.ModifiedUtc));
                }

                StatusMessage = $"{Images.Count} image(s)";
            }, DispatcherPriority.Background, token);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation from folder changes.
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task EnsureThumbnailAsync(ImageItemViewModel item, CancellationToken cancellationToken = default)
    {
        if (item.ThumbnailSource is not null || item.IsThumbnailLoading)
        {
            return;
        }

        item.IsThumbnailLoading = true;
        try
        {
            var thumbnail = await _thumbnailService.GetThumbnailAsync(item.FilePath, 120, cancellationToken);
            if (thumbnail is not null)
            {
                await _dispatcher.InvokeAsync(() => item.ThumbnailSource = thumbnail);
            }
        }
        finally
        {
            item.IsThumbnailLoading = false;
        }
    }

    public bool RemoveImage(ImageItemViewModel item) => Images.Remove(item);
}
