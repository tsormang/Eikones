using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eikones.Models;
using Eikones.Services;
using Eikones.Views;
using System.IO;
using System.Windows;

namespace Eikones.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFileDeleteService _fileDeleteService;
    private AppSettings _settings;

    public MainViewModel(
        ISettingsRepository settingsRepository,
        IFileDeleteService fileDeleteService,
        SourceBrowserViewModel sourceBrowser,
        PreviewViewModel preview,
        DestinationListViewModel destinationList)
    {
        _settingsRepository = settingsRepository;
        _fileDeleteService = fileDeleteService;
        SourceBrowser = sourceBrowser;
        Preview = preview;
        DestinationList = destinationList;
        _settings = _settingsRepository.Load();

        Preview.ImageDeleted += OnImageDeleted;
        Preview.ImageMoved += OnImageMoved;
        DestinationList.FileRestored += OnDestinationFileRestored;
    }

    public SourceBrowserViewModel SourceBrowser { get; }
    public PreviewViewModel Preview { get; }
    public DestinationListViewModel DestinationList { get; }

    [ObservableProperty]
    private ImageItemViewModel? _selectedImage;

    [ObservableProperty]
    private string _windowTitle = "Eikones";

    public double WindowWidth => _settings.WindowWidth;
    public double WindowHeight => _settings.WindowHeight;
    public double? WindowLeft => _settings.WindowLeft;
    public double? WindowTop => _settings.WindowTop;

    public async Task InitializeAsync()
    {
        SourceBrowser.Configure(_settings.SupportedExtensions);
        DestinationList.Configure(_settings.SupportedExtensions);
        DestinationList.SourceFolderPath = _settings.SourceFolderPath;
        Preview.DestinationFolderPath = _settings.DestinationFolderPath;

        await SourceBrowser.LoadFolderAsync(_settings.SourceFolderPath);
        await DestinationList.LoadFolderAsync(_settings.DestinationFolderPath);

        if (SourceBrowser.Images.Count > 0)
        {
            SelectedImage = SourceBrowser.Images[0];
        }
    }

    public void SaveWindowState(double width, double height, double left, double top)
    {
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        _settings.WindowLeft = IsValidWindowCoordinate(left) ? left : null;
        _settings.WindowTop = IsValidWindowCoordinate(top) ? top : null;
        _settingsRepository.Save(_settings);
    }

    private static bool IsValidWindowCoordinate(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var vm = new SettingsViewModel(_settings);
        var window = new SettingsWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        if (!TryValidateFolder(vm.SourceFolderPath, out var sourceError))
        {
            MessageBox.Show(sourceError, "Invalid source folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryValidateFolder(vm.DestinationFolderPath, out var destinationError))
        {
            MessageBox.Show(destinationError, "Invalid destination folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.SourceFolderPath = Path.GetFullPath(vm.SourceFolderPath!);
        _settings.DestinationFolderPath = Path.GetFullPath(vm.DestinationFolderPath!);
        _settingsRepository.Save(_settings);

        await ApplySourceFolderAsync(_settings.SourceFolderPath);
        await ApplyDestinationFolderAsync(_settings.DestinationFolderPath);
    }

    public async Task<bool> TrySetSourceFolderFromDropAsync(string folderPath)
    {
        if (!TryValidateFolder(folderPath, out var error))
        {
            MessageBox.Show(error, "Invalid source folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var normalized = Path.GetFullPath(folderPath);
        _settings.SourceFolderPath = normalized;
        _settingsRepository.Save(_settings);
        await ApplySourceFolderAsync(normalized);
        return true;
    }

    [RelayCommand]
    private async Task CreateNewDestinationFolderAsync()
    {
        var name = DestinationList.NewFolderName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a folder name.", "New folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show("The folder name contains invalid characters.", "New folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currentFolder = _settings.DestinationFolderPath;
        if (!TryValidateFolder(currentFolder, out _))
        {
            MessageBox.Show("Select a destination folder first.", "New folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var parent = Path.GetDirectoryName(currentFolder);
        if (string.IsNullOrEmpty(parent))
        {
            MessageBox.Show("Cannot create a folder next to the root directory.", "New folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newFolderPath = Path.GetFullPath(Path.Combine(parent, name));
        if (Directory.Exists(newFolderPath))
        {
            MessageBox.Show($"A folder named \"{name}\" already exists.", "New folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(newFolderPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create folder:\n{ex.Message}", "New folder", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        DestinationList.NewFolderName = string.Empty;
        _settings.DestinationFolderPath = newFolderPath;
        _settingsRepository.Save(_settings);
        await ApplyDestinationFolderAsync(newFolderPath);
    }

    public async Task<bool> TrySetDestinationFolderFromDropAsync(string folderPath)
    {
        if (!TryValidateFolder(folderPath, out var error))
        {
            MessageBox.Show(error, "Invalid destination folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var normalized = Path.GetFullPath(folderPath);
        _settings.DestinationFolderPath = normalized;
        _settingsRepository.Save(_settings);
        await ApplyDestinationFolderAsync(normalized);
        return true;
    }

    private async Task ApplySourceFolderAsync(string? folderPath)
    {
        DestinationList.SourceFolderPath = folderPath;
        await SourceBrowser.LoadFolderAsync(folderPath);

        if (SelectedImage is not null && !SourceBrowser.Images.Contains(SelectedImage))
        {
            SelectedImage = SourceBrowser.Images.FirstOrDefault();
        }
        else if (SelectedImage is null && SourceBrowser.Images.Count > 0)
        {
            SelectedImage = SourceBrowser.Images[0];
        }
    }

    private async Task ApplyDestinationFolderAsync(string? folderPath)
    {
        Preview.DestinationFolderPath = folderPath;
        await DestinationList.LoadFolderAsync(folderPath);
    }

    private static bool TryValidateFolder(string? folderPath, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "Folder path is empty.";
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            errorMessage = $"The folder does not exist or is not accessible:\n{folderPath}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    partial void OnSelectedImageChanged(ImageItemViewModel? value)
    {
        WindowTitle = value is null ? "Eikones" : $"Eikones — {value.FileName}";
        _ = Preview.LoadAsync(value);

        if (value is not null)
        {
            _ = SourceBrowser.EnsureThumbnailAsync(value);
        }

        DeleteAllPreviousCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAllPrevious))]
    private async Task DeleteAllPreviousAsync()
    {
        if (SelectedImage is null)
        {
            return;
        }

        var index = SourceBrowser.Images.IndexOf(SelectedImage);
        if (index < 0)
        {
            return;
        }

        var toDelete = SourceBrowser.Images.Take(index + 1).ToList();
        var nextImage = index + 1 < SourceBrowser.Images.Count ? SourceBrowser.Images[index + 1] : null;
        var failedCount = 0;

        foreach (var image in toDelete)
        {
            if (await _fileDeleteService.DeleteToRecycleBinAsync(image.FilePath))
            {
                SourceBrowser.RemoveImage(image);
            }
            else
            {
                failedCount++;
            }
        }

        SourceBrowser.StatusMessage = failedCount == 0
            ? $"{SourceBrowser.Images.Count} image(s)"
            : $"{SourceBrowser.Images.Count} image(s) — failed to delete {failedCount} file(s)";

        if (nextImage is not null && SourceBrowser.Images.Contains(nextImage))
        {
            SelectedImage = nextImage;
        }
        else if (SelectedImage is not null && SourceBrowser.Images.Contains(SelectedImage))
        {
            // Selected image was not deleted (e.g. delete failed).
        }
        else if (SourceBrowser.Images.Count > 0)
        {
            SelectedImage = SourceBrowser.Images[0];
        }
        else
        {
            SelectedImage = null;
        }
    }

    private bool CanDeleteAllPrevious() => SelectedImage is not null;

    private void OnImageDeleted(object? sender, ImageItemViewModel image) =>
        AdvanceAfterRemoval(image);

    private void OnImageMoved(object? sender, ImageItemViewModel image) =>
        AdvanceAfterRemoval(image, refreshDestination: true);

    private async void OnDestinationFileRestored(object? sender, ImageItemViewModel image)
    {
        await SourceBrowser.LoadFolderAsync(_settings.SourceFolderPath);
    }

    private async void AdvanceAfterRemoval(ImageItemViewModel image, bool refreshDestination = false)
    {
        var index = SourceBrowser.Images.IndexOf(image);

        // Pre-select the successor before removing the item from the collection.
        // If the removal happens while the item is still selected, the ListBox's
        // TwoWay binding clears SelectedImage (because the selected item disappeared),
        // which can overwrite the value we set afterwards and break keyboard navigation.
        // By selecting the next item first, the removed item is no longer selected
        // when it leaves the collection, so the binding never fires the null back.
        if (index >= 0)
        {
            SelectedImage = SourceBrowser.Images.Count <= 1
                ? null
                : SourceBrowser.Images[index > 0 ? index - 1 : 1];
        }

        SourceBrowser.RemoveImage(image);

        if (refreshDestination)
        {
            await DestinationList.LoadFolderAsync(_settings.DestinationFolderPath);
        }
    }
}
