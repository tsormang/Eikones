using System.Windows;
using System.Windows.Controls;
using Eikones.Infrastructure;
using Eikones.ViewModels;

namespace Eikones.Views;

public partial class SourceColumnView : UserControl
{
    public SourceColumnView()
    {
        InitializeComponent();
    }

    private void SourceColumn_OnDragOver(object sender, DragEventArgs e) =>
        FolderDropHelper.HandleDragOver(e);

    private async void SourceColumn_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (!FolderDropHelper.TryGetFolderPath(e.Data, out var folderPath, out var error))
        {
            MessageBox.Show(error, "Invalid drop", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DataContext is MainViewModel mainVm)
        {
            await mainVm.TrySetSourceFolderFromDropAsync(folderPath);
        }
    }

    private async void SourceList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm)
        {
            return;
        }

        if (SourceList.SelectedItem is ImageItemViewModel item)
        {
            await mainVm.SourceBrowser.EnsureThumbnailAsync(item);
        }
    }

    private async void SourceItem_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainVm)
        {
            return;
        }

        if (sender is ListBoxItem { DataContext: ImageItemViewModel item })
        {
            await mainVm.SourceBrowser.EnsureThumbnailAsync(item);
        }
    }
}
