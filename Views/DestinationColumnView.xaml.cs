using System.Windows;
using System.Windows.Controls;
using Eikones.Infrastructure;
using Eikones.ViewModels;

namespace Eikones.Views;

public partial class DestinationColumnView : UserControl
{
    public DestinationColumnView()
    {
        InitializeComponent();
    }

    private void DestinationColumn_OnDragOver(object sender, DragEventArgs e) =>
        FolderDropHelper.HandleDragOver(e);

    private async void DestinationColumn_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (!FolderDropHelper.TryGetFolderPath(e.Data, out var folderPath, out var error))
        {
            MessageBox.Show(error, "Invalid drop", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DataContext is MainViewModel mainVm)
        {
            await mainVm.TrySetDestinationFolderFromDropAsync(folderPath);
        }
    }
}
