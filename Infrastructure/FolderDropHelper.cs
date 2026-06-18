using System.IO;
using System.Windows;

namespace Eikones.Infrastructure;

public static class FolderDropHelper
{
    public static void HandleDragOver(DragEventArgs e)
    {
        e.Effects = CanAcceptDrop(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    public static bool CanAcceptDrop(IDataObject data)
    {
        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length != 1)
        {
            return false;
        }

        var path = paths[0];
        if (File.Exists(path))
        {
            return false;
        }

        return Directory.Exists(path);
    }

    public static bool TryGetFolderPath(IDataObject data, out string folderPath, out string errorMessage)
    {
        folderPath = string.Empty;
        errorMessage = string.Empty;

        if (!data.GetDataPresent(DataFormats.FileDrop))
        {
            errorMessage = "Only folders can be dropped here.";
            return false;
        }

        if (data.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            errorMessage = "Invalid drop data.";
            return false;
        }

        if (paths.Any(File.Exists))
        {
            errorMessage = "File drops are not allowed. Drop a single folder instead.";
            return false;
        }

        if (paths.Length > 1)
        {
            errorMessage = "Drop only one folder at a time.";
            return false;
        }

        folderPath = paths[0];
        if (!Directory.Exists(folderPath))
        {
            errorMessage = $"The folder does not exist or is not accessible:\n{folderPath}";
            return false;
        }

        folderPath = Path.GetFullPath(folderPath);
        return true;
    }
}
