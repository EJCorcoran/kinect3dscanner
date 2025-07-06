using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scanner.UI.ViewModels;
using Scanner.UI.Views;
using KinectCore.Services;
using System.Windows;

namespace Scanner.UI
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Core services
                    services.AddSingleton<KinectCameraService>();
                    services.AddSingleton<ScanningService>();
                    
                    // ViewModels
                    services.AddTransient<MainWindowViewModel>();
                    // TODO: Implement missing ViewModels
                    // services.AddTransient<ScanningViewModel>();
                    // services.AddTransient<SettingsViewModel>();
                    // services.AddTransient<PreviewViewModel>();
                    
                    // Views
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
