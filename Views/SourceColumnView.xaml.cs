using System.Windows;
using System.Windows.Controls;
using Eikones.ViewModels;

namespace Eikones.Views;

public partial class SourceColumnView : UserControl
{
    public SourceColumnView()
    {
        InitializeComponent();
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
