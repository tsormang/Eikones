namespace Eikones.Infrastructure;

public static class FolderFileCountFormatter
{
    public static string FormatCounts(IReadOnlyDictionary<string, int> counts)
    {
        if (counts.Count == 0)
        {
            return "No files";
        }

        var parts = counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Value} {FormatExtension(pair.Key)}");

        return string.Join(", ", parts);
    }

    public static string FormatSummary(string? folderPath, IReadOnlyDictionary<string, int> counts)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return "No source folder selected.";
        }

        return $"{folderPath} — {FormatCounts(counts)}";
    }

    private static string FormatExtension(string extension) =>
        string.IsNullOrEmpty(extension) ? "(no extension)" : extension.TrimStart('.').ToLowerInvariant();
}
