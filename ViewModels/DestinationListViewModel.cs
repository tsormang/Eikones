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

public partial class DestinationListViewModel : ObservableObject
{
    private readonly IFileScanner _fileScanner;
    private readonly IFileDeleteService _fileDeleteService;
    private readonly IFileTransferService _fileTransferService;
    private readonly IMoveHistoryRepository _moveHistory;
    private readonly Dispatcher _dispatcher;
    private string[] _extensions = SupportedExtensions.Default;
    private CancellationTokenSource? _scanCts;

    public DestinationListViewModel(
        IFileScanner fileScanner,
        IFileDeleteService fileDeleteService,
        IFileTransferService fileTransferService,
        IMoveHistoryRepository moveHistory)
    {
        _fileScanner = fileScanner;
        _fileDeleteService = fileDeleteService;
        _fileTransferService = fileTransferService;
        _moveHistory = moveHistory;
        _dispatcher = Application.Current.Dispatcher;
    }

    public ObservableCollection<ImageItemViewModel> Files { get; } = [];

    [ObservableProperty]
    private string? _folderPath;

    [ObservableProperty]
    private string? _sourceFolderPath;

    [ObservableProperty]
    private ImageItemViewModel? _selectedFile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "No destination folder selected.";

    [ObservableProperty]
    private string _newFolderName = string.Empty;

    public event EventHandler<ImageItemViewModel>? FileDeleted;
    public event EventHandler<ImageItemViewModel>? FileRestored;

    public void Configure(string[] extensions) => _extensions = extensions;

    public async Task LoadFolderAsync(string? folderPath)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        FolderPath = folderPath;
        Files.Clear();
        SelectedFile = null;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            StatusMessage = "No destination folder selected.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Scanning destination...";

        try
        {
            var entries = await _fileScanner.EnumerateAsync(folderPath, _extensions, token);
            token.ThrowIfCancellationRequested();

            await _dispatcher.InvokeAsync(() =>
            {
                foreach (var entry in entries)
                {
                    Files.Add(new ImageItemViewModel(
                        entry.FilePath,
                        entry.FileName,
                        entry.SizeBytes,
                        entry.ModifiedUtc));
                }

                StatusMessage = $"{Files.Count} file(s)";
            }, DispatcherPriority.Background, token);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation.
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnFile))]
    private async Task DeleteFileAsync(ImageItemViewModel? file)
    {
        file ??= SelectedFile;
        if (file is null)
        {
            return;
        }

        var success = await _fileDeleteService.DeleteToRecycleBinAsync(file.FilePath);
        if (!success)
        {
            StatusMessage = $"Failed to delete {file.FileName}";
            return;
        }

        _moveHistory.Remove(file.FilePath);
        Files.Remove(file);
        if (SelectedFile == file)
        {
            SelectedFile = null;
        }

        StatusMessage = $"{Files.Count} file(s)";
        FileDeleted?.Invoke(this, file);
    }

    [RelayCommand(CanExecute = nameof(CanActOnFile))]
    private async Task RestoreFileAsync(ImageItemViewModel? file)
    {
        file ??= SelectedFile;
        if (file is null)
        {
            return;
        }

        var originalFolder = _moveHistory.GetOriginalFolder(file.FilePath) ?? SourceFolderPath;
        if (string.IsNullOrWhiteSpace(originalFolder) || !Directory.Exists(originalFolder))
        {
            StatusMessage = $"No original folder found for {file.FileName}";
            return;
        }

        var success = await _fileTransferService.MoveAsync(file.FilePath, originalFolder);
        if (!success)
        {
            StatusMessage = $"Failed to restore {file.FileName}";
            return;
        }

        _moveHistory.Remove(file.FilePath);
        Files.Remove(file);
        if (SelectedFile == file)
        {
            SelectedFile = null;
        }

        StatusMessage = $"{Files.Count} file(s)";
        FileRestored?.Invoke(this, file);
    }

    private bool CanActOnFile(ImageItemViewModel? file) => file is not null || SelectedFile is not null;

    partial void OnSelectedFileChanged(ImageItemViewModel? value)
    {
        DeleteFileCommand.NotifyCanExecuteChanged();
        RestoreFileCommand.NotifyCanExecuteChanged();
    }
}
