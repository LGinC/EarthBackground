using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using EarthBackground.Background;
using EarthBackground.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EarthBackground
{
    public partial class App : Application
    {
        private System.IServiceProvider? _serviceProvider;
        private MainWindowViewModel? _mainWindowViewModel;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private static void ShowErrorDialog(string message)
        {
            Dispatcher.UIThread.Post(() =>
                Controls.ModernNotification.Show(message, Controls.ModernNotification.NotificationType.Error));
        }

        public override void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                _logger.Fatal(ex, "Unhandled exception");
                ShowErrorDialog(ex?.Message ?? e.ExceptionObject?.ToString() ?? "Unknown error");
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                _logger.Error(e.Exception, "Unobserved task exception");
                e.SetObserved();
                ShowErrorDialog(e.Exception.Message);
            };

            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                _logger.Error(e.Exception, "UI thread unhandled exception");
                e.Handled = true;
                ShowErrorDialog(e.Exception.Message);
            };

            _serviceProvider = Program.ConfigureServices();

            var wallpaperService = _serviceProvider.GetRequiredService<WallpaperService>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindowViewModel = new MainWindowViewModel(
                    _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MainWindowViewModel>>(),
                    _serviceProvider,
                    wallpaperService,
                    _serviceProvider.GetRequiredService<Localization.ILocalizationService>(),
                    desktop);

                // Set DataContext for TrayIcon commands
                DataContext = _mainWindowViewModel;

                var mainWindow = new Views.MainWindow
                {
                    DataContext = _mainWindowViewModel
                };

                desktop.MainWindow = mainWindow;

                // 注入主窗口访问器，供 SettingsCommand 获取 owner
                _mainWindowViewModel.SetMainWindowAccessor(() => desktop.MainWindow);

                // Start wallpaper service
                _ = Task.Run(() => wallpaperService.StartAsync(CancellationToken.None));
                wallpaperService.Start();

                // 注册应用退出事件，确保资源释放
                desktop.Exit += (_, _) =>
                {
                    wallpaperService.Stop();
                    _mainWindowViewModel?.Dispose();
                };

                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
