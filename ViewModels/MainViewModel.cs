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
    private AppSettings _settings;

    public MainViewModel(
        ISettingsRepository settingsRepository,
        SourceBrowserViewModel sourceBrowser,
        PreviewViewModel preview,
        DestinationListViewModel destinationList)
    {
        _settingsRepository = settingsRepository;
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
    }

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
        SourceBrowser.RemoveImage(image);

        if (refreshDestination)
        {
            await DestinationList.LoadFolderAsync(_settings.DestinationFolderPath);
        }

        if (SourceBrowser.Images.Count == 0)
        {
            SelectedImage = null;
            return;
        }

        var nextIndex = index >= SourceBrowser.Images.Count ? SourceBrowser.Images.Count - 1 : index;
        SelectedImage = SourceBrowser.Images[nextIndex];
    }
}
