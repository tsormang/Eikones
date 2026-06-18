using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eikones.Models;
using Microsoft.Win32;

namespace Eikones.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _sourceFolderPath;

    [ObservableProperty]
    private string? _destinationFolderPath;

    public SettingsViewModel(AppSettings settings)
    {
        SourceFolderPath = settings.SourceFolderPath;
        DestinationFolderPath = settings.DestinationFolderPath;
    }

    [RelayCommand]
    private void BrowseSource()
    {
        var path = PickFolder(SourceFolderPath);
        if (path is not null)
        {
            SourceFolderPath = path;
        }
    }

    [RelayCommand]
    private void BrowseDestination()
    {
        var path = PickFolder(DestinationFolderPath);
        if (path is not null)
        {
            DestinationFolderPath = path;
        }
    }

    public AppSettings ToSettings() => new()
    {
        SourceFolderPath = SourceFolderPath,
        DestinationFolderPath = DestinationFolderPath
    };

    private static string? PickFolder(string? initialPath)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder",
            InitialDirectory = string.IsNullOrWhiteSpace(initialPath) ? null : initialPath,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
