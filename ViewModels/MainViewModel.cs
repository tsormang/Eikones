using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eikones.Models;
using Eikones.Services;
using Eikones.Views;

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

        _settings.SourceFolderPath = vm.SourceFolderPath;
        _settings.DestinationFolderPath = vm.DestinationFolderPath;
        _settingsRepository.Save(_settings);

        Preview.DestinationFolderPath = _settings.DestinationFolderPath;
        DestinationList.SourceFolderPath = _settings.SourceFolderPath;
        await SourceBrowser.LoadFolderAsync(_settings.SourceFolderPath);
        await DestinationList.LoadFolderAsync(_settings.DestinationFolderPath);

        if (SelectedImage is not null && !SourceBrowser.Images.Contains(SelectedImage))
        {
            SelectedImage = SourceBrowser.Images.FirstOrDefault();
        }
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
