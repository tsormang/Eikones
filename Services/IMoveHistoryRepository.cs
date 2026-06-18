namespace Eikones.Services;

public interface IMoveHistoryRepository
{
    void RecordMove(string sourcePath, string destinationPath);
    string? GetOriginalFolder(string destinationPath);
    void Remove(string destinationPath);
}
