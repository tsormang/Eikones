namespace Eikones.Models;

public sealed class ImageFileEntry
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public long SizeBytes { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
