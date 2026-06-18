using System.IO;
using System.Text.Json;
using Eikones.Models;

namespace Eikones.Services;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public JsonSettingsRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "Eikones");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        SanitizeWindowCoordinates(settings);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static void SanitizeWindowCoordinates(AppSettings settings)
    {
        if (settings.WindowLeft is double left && !IsValidCoordinate(left))
        {
            settings.WindowLeft = null;
        }

        if (settings.WindowTop is double top && !IsValidCoordinate(top))
        {
            settings.WindowTop = null;
        }
    }

    private static bool IsValidCoordinate(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}
