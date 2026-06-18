using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;

namespace Eikones.ViewModels;

public partial class ImageItemViewModel : ObservableObject
{
    public ImageItemViewModel(string filePath, string fileName, long sizeBytes, DateTime modifiedUtc)
    {
        FilePath = filePath;
        FileName = fileName;
        SizeBytes = sizeBytes;
        ModifiedUtc = modifiedUtc;
    }

    public string FilePath { get; }
    public string FileName { get; }
    public long SizeBytes { get; }
    public DateTime ModifiedUtc { get; }

    [ObservableProperty]
    private BitmapSource? _thumbnailSource;

    [ObservableProperty]
    private bool _isThumbnailLoading;
}
