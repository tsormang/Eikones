using Eikones.Infrastructure;

namespace Eikones.Models;

public sealed class AppSettings
{
    public string? SourceFolderPath { get; set; }
    public string? DestinationFolderPath { get; set; }
    public string[] SupportedExtensions { get; set; } = Eikones.Infrastructure.SupportedExtensions.Default;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
}
