using Eikones.Infrastructure;
using Eikones.Services;
using Eikones.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Eikones;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
                services.AddSingleton<IImageCache, ImageCache>();
                services.AddSingleton<IFileScanner, FileScanner>();
                services.AddSingleton<IThumbnailService, ThumbnailService>();
                services.AddSingleton<IPreviewLoader, PreviewLoader>();
                services.AddSingleton<IFileTransferService, FileTransferService>();
                services.AddSingleton<IFileDeleteService, FileDeleteService>();
                services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
                services.AddSingleton<IMoveHistoryRepository, JsonMoveHistoryRepository>();

                services.AddSingleton<SourceBrowserViewModel>();
                services.AddSingleton<PreviewViewModel>();
                services.AddSingleton<DestinationListViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
