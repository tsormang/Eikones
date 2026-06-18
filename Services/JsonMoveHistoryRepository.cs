using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Eikones.Services;

public sealed class JsonMoveHistoryRepository : IMoveHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _historyPath;
    private readonly ConcurrentDictionary<string, string> _entries;
    private readonly object _saveLock = new();

    public JsonMoveHistoryRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "Eikones");
        Directory.CreateDirectory(folder);
        _historyPath = Path.Combine(folder, "move-history.json");
        _entries = new ConcurrentDictionary<string, string>(LoadEntries(), StringComparer.OrdinalIgnoreCase);
    }

    public void RecordMove(string sourcePath, string destinationPath)
    {
        var sourceFolder = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrWhiteSpace(sourceFolder))
        {
            return;
        }

        _entries[NormalizePath(destinationPath)] = NormalizePath(sourceFolder);
        SaveEntries();
    }

    public string? GetOriginalFolder(string destinationPath)
    {
        return _entries.TryGetValue(NormalizePath(destinationPath), out var folder)
            ? folder
            : null;
    }

    public void Remove(string destinationPath)
    {
        if (_entries.TryRemove(NormalizePath(destinationPath), out _))
        {
            SaveEntries();
        }
    }

    private Dictionary<string, string> LoadEntries()
    {
        if (!File.Exists(_historyPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveEntries()
    {
        lock (_saveLock)
        {
            var json = JsonSerializer.Serialize(_entries, JsonOptions);
            File.WriteAllText(_historyPath, json);
        }
    }

    private static string NormalizePath(string path) => Path.GetFullPath(path);
}
