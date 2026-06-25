using System.Windows;
using System.Windows.Input;
using Eikones.ViewModels;

namespace Eikones;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        if (viewModel.WindowLeft is double left && viewModel.WindowTop is double top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.SaveWindowState(ActualWidth, ActualHeight, Left, Top);
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down))
        {
            return;
        }

        // Don't intercept Up/Down while a text-editing control has focus
        // (e.g. the new-folder name TextBox in the destination column).
        if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase)
        {
            return;
        }

        var images = _viewModel.SourceBrowser.Images;
        if (images.Count == 0)
        {
            return;
        }

        var currentIndex = _viewModel.SelectedImage is { } selected
            ? images.IndexOf(selected)
            : -1;

        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextIndex = e.Key == Key.Down
            ? Math.Min(currentIndex + 1, images.Count - 1)
            : Math.Max(currentIndex - 1, 0);

        if (nextIndex != currentIndex || _viewModel.SelectedImage is null)
        {
            _viewModel.SelectedImage = images[nextIndex];
        }

        FocusSourceListAtSelection();
        e.Handled = true;
    }

    private void FocusSourceListAtSelection()
    {
        if (Content is not FrameworkElement root)
        {
            return;
        }

        var sourceView = FindVisualChild<Views.SourceColumnView>(root);
        sourceView?.FocusListAtSelection();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
