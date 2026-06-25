using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eikones.Models;
using Eikones.Services;
using System.IO;
using System.Windows.Media.Imaging;

namespace Eikones.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    private const int PreviewMaxEdge = 1920;

    private readonly IThumbnailService _thumbnailService;
    private readonly IPreviewLoader _previewLoader;
    private readonly IFileTransferService _fileTransferService;
    private readonly IFileDeleteService _fileDeleteService;
    private readonly IImageMetadataReader _metadataReader;
    private readonly IMoveHistoryRepository _moveHistory;
    private CancellationTokenSource? _previewCts;

    public PreviewViewModel(
        IThumbnailService thumbnailService,
        IPreviewLoader previewLoader,
        IFileTransferService fileTransferService,
        IFileDeleteService fileDeleteService,
        IImageMetadataReader metadataReader,
        IMoveHistoryRepository moveHistory)
    {
        _thumbnailService = thumbnailService;
        _previewLoader = previewLoader;
        _fileTransferService = fileTransferService;
        _fileDeleteService = fileDeleteService;
        _metadataReader = metadataReader;
        _moveHistory = moveHistory;
    }

    [ObservableProperty]
    private ImageItemViewModel? _currentImage;

    [ObservableProperty]
    private BitmapSource? _placeholderSource;

    [ObservableProperty]
    private BitmapSource? _fullPreviewSource;

    [ObservableProperty]
    private bool _isLoadingFullPreview;

    [ObservableProperty]
    private string _statusMessage = "Select an image to preview.";

    [ObservableProperty]
    private string? _destinationFolderPath;

    [ObservableProperty]
    private string? _dateTakenDisplay;

    public event EventHandler<ImageItemViewModel>? ImageDeleted;
    public event EventHandler<ImageItemViewModel>? ImageMoved;

    public async Task LoadAsync(ImageItemViewModel? image)
    {
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        CurrentImage = image;
        FullPreviewSource = null;

        if (image is null)
        {
            PlaceholderSource = null;
            IsLoadingFullPreview = false;
            DateTakenDisplay = null;
            StatusMessage = "Select an image to preview.";
            return;
        }

        StatusMessage = image.FileName;
        DateTakenDisplay = "Reading metadata...";
        IsLoadingFullPreview = true;

        PlaceholderSource = image.ThumbnailSource;
        if (PlaceholderSource is null)
        {
            var thumb = await _thumbnailService.GetThumbnailAsync(image.FilePath, 120, token);
            if (token.IsCancellationRequested || CurrentImage != image)
            {
                return;
            }

            PlaceholderSource = thumb;
            if (thumb is not null)
            {
                image.ThumbnailSource = thumb;
            }
        }

        try
        {
            var metadataTask = _metadataReader.GetDateTakenDisplayAsync(image.FilePath, token);
            var previewTask = _previewLoader.LoadPreviewAsync(image.FilePath, PreviewMaxEdge, token);

            await Task.WhenAll(metadataTask, previewTask);

            if (token.IsCancellationRequested || CurrentImage != image)
            {
                return;
            }

            FullPreviewSource = await previewTask;
            DateTakenDisplay = FormatDateTaken(await metadataTask);
        }
        finally
        {
            if (CurrentImage == image)
            {
                IsLoadingFullPreview = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnImage))]
    private async Task DeleteAsync()
    {
        if (CurrentImage is null)
        {
            return;
        }

        var image = CurrentImage;
        var success = await _fileDeleteService.DeleteToRecycleBinAsync(image.FilePath);
        if (success)
        {
            ImageDeleted?.Invoke(this, image);
        }
        else
        {
            StatusMessage = $"Failed to delete {image.FileName}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveImage))]
    private async Task MoveAsync()
    {
        if (CurrentImage is null || string.IsNullOrWhiteSpace(DestinationFolderPath))
        {
            return;
        }

        var image = CurrentImage;
        var success = await _fileTransferService.MoveAsync(image.FilePath, DestinationFolderPath);
        if (success)
        {
            var destinationPath = Path.Combine(DestinationFolderPath, image.FileName);
            _moveHistory.RecordMove(image.FilePath, destinationPath);
            ImageMoved?.Invoke(this, image);
        }
        else
        {
            StatusMessage = $"Failed to move {image.FileName}";
        }
    }

    private bool CanActOnImage() => CurrentImage is not null;

    private bool CanMoveImage() =>
        CurrentImage is not null && !string.IsNullOrWhiteSpace(DestinationFolderPath);

    partial void OnCurrentImageChanged(ImageItemViewModel? value)
    {
        DeleteCommand.NotifyCanExecuteChanged();
        MoveCommand.NotifyCanExecuteChanged();
    }

    partial void OnDestinationFolderPathChanged(string? value) =>
        MoveCommand.NotifyCanExecuteChanged();

    private static string FormatDateTaken(string? dateTaken) =>
        string.IsNullOrWhiteSpace(dateTaken)
            ? "Date taken: unavailable"
            : $"Date taken: {dateTaken}";
}
