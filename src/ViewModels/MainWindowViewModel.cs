using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using EarthBackground.Background;
using EarthBackground.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace EarthBackground.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _provider;
        private readonly WallpaperService _wallpaperService;
        private readonly ILocalizationService _loc;
        private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

        // 用于获取当前主窗口（供 SettingsCommand 作为 owner）
        private Func<Window?>? _getMainWindow;

        private string _statusText;
        private string _progressText = string.Empty;
        private double _progressValue = 0;
        private int _progressMax = 100;
        private bool _isRunning = false;
        private float _earthRotationAngle = 0f;
        private bool _disposed = false;

        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => this.RaiseAndSetIfChanged(ref _progressText, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
        }

        public int ProgressMax
        {
            get => _progressMax;
            set => this.RaiseAndSetIfChanged(ref _progressMax, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                this.RaiseAndSetIfChanged(ref _isRunning, value);
                this.RaisePropertyChanged(nameof(CanStart));
                this.RaisePropertyChanged(nameof(CanStop));
            }
        }

        public float EarthRotationAngle
        {
            get => _earthRotationAngle;
            set => this.RaiseAndSetIfChanged(ref _earthRotationAngle, value);
        }

        public bool CanStart => !IsRunning;
        public bool CanStop => IsRunning;

        // Localized button labels
        public string WindowTitle => _loc["MainWindow_Title"];
        public string HeaderTitle => _loc["MainWindow_Header"];
        public string BtnStart => _loc["Btn_Start"];
        public string BtnStop => _loc["Btn_Stop"];
        public string BtnSettings => _loc["Btn_Settings"];
        public string BtnExit => _loc["Btn_Exit"];
        public string NotifyHiddenToTray => _loc["Notify_HiddenToTray"];

        public ReactiveCommand<Unit, Unit> StartCommand { get; }
        public ReactiveCommand<Unit, Unit> StopCommand { get; }
        public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowMainWindowCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }

        private DispatcherTimer? _earthRotationTimer;

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider provider,
            WallpaperService wallpaperService,
            ILocalizationService loc,
            IClassicDesktopStyleApplicationLifetime lifetime)
        {
            _logger = logger;
            _provider = provider;
            _wallpaperService = wallpaperService;
            _loc = loc;
            _lifetime = lifetime;
            _statusText = _loc["Status_WaitForRun"];

            StartCommand = ReactiveCommand.Create(OnStart, outputScheduler: AvaloniaScheduler.Instance);
            StopCommand = ReactiveCommand.Create(OnStop, outputScheduler: AvaloniaScheduler.Instance);
            SettingsCommand = ReactiveCommand.CreateFromTask(OnSettingsAsync, outputScheduler: AvaloniaScheduler.Instance);

            ShowMainWindowCommand = ReactiveCommand.Create(OnShowMainWindow, outputScheduler: AvaloniaScheduler.Instance);
            ExitCommand = ReactiveCommand.Create(OnExit, outputScheduler: AvaloniaScheduler.Instance);

            SubscribeToWallpaperServiceEvents();

            IsRunning = _wallpaperService.IsRunning;
            StatusText = _wallpaperService.IsRunning ? _loc["Status_Running"] : _loc["Status_WaitForRun"];

            if (_wallpaperService.IsRunning)
                StartEarthRotation();
        }

        /// <summary>
        /// 由 App 在创建 MainWindow 后注入，用于 SettingsCommand 获取 owner 窗口
        /// </summary>
        public void SetMainWindowAccessor(Func<Window?> getMainWindow)
        {
            _getMainWindow = getMainWindow;
        }

        private void SubscribeToWallpaperServiceEvents()
        {
            _wallpaperService.StatusChanged += OnStatusChanged;
            _wallpaperService.ProgressChanged += OnProgressChanged;
            _wallpaperService.ImageSaved += OnImageSaved;
            _wallpaperService.ErrorOccurred += OnErrorOccurred;
        }

        private void UnsubscribeFromWallpaperServiceEvents()
        {
            _wallpaperService.StatusChanged -= OnStatusChanged;
            _wallpaperService.ProgressChanged -= OnProgressChanged;
            _wallpaperService.ImageSaved -= OnImageSaved;
            _wallpaperService.ErrorOccurred -= OnErrorOccurred;
        }

        private void OnStatusChanged(string status)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsRunning = _wallpaperService.IsRunning;

                StatusText = status switch
                {
                    "Running" => _loc["Status_Running"],
                    "Initializing..." => _loc["Status_Initializing"],
                    "Downloading..." => _loc["Status_Downloading"],
                    "Setting Wallpaper..." => _loc["Status_SettingWallpaper"],
                    "Complete" => _loc["Status_Complete"],
                    "Stopped" => _loc["Status_WaitForRun"],
                    _ => status
                };

                if (_wallpaperService.IsRunning)
                    StartEarthRotation();
                else
                    StopEarthRotation();
            });
        }

        private void OnProgressChanged(int current, int total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (total > 0)
                {
                    ProgressMax = total;
                    ProgressValue = Math.Min(current, total);
                    ProgressText = $"{current}/{total} ({(int)((float)current / total * 100)}%)";
                }

                if (current >= total && total > 0)
                {
                    Task.Delay(1500).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ProgressValue = 0;
                            ProgressText = string.Empty;
                        });
                    });
                }
            });
        }

        private void OnImageSaved(string imagePath)
        {
            _logger.LogInformation("壁纸已保存: {ImagePath}", imagePath);
        }

        private void OnErrorOccurred(Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _logger.LogError(ex, "壁纸服务发生错误");
                IsRunning = false;
                StatusText = _loc["Status_WaitForRun"];
                ProgressText = string.Empty;
                ProgressValue = 0;

                Controls.ModernNotification.Show(
                    _loc.Format("Notify_DownloadFailed", ex.Message),
                    Controls.ModernNotification.NotificationType.Error);
            });
        }

        private void OnStart()
        {
            try
            {
                _logger.LogInformation("用户点击开始按钮");
                _wallpaperService.StartWallpaperUpdates();
                IsRunning = true;
                ProgressValue = 0;
                ProgressText = string.Empty;

                Controls.ModernNotification.Show(
                    _loc["Notify_ServiceStarted"],
                    Controls.ModernNotification.NotificationType.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动壁纸服务时发生错误");
                OnErrorOccurred(ex);
            }
        }

        private void OnStop()
        {
            try
            {
                _logger.LogInformation("用户点击停止按钮");
                _wallpaperService.StopWallpaperUpdates();
                IsRunning = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止壁纸服务时发生错误");
                IsRunning = _wallpaperService.IsRunning;
            }
        }

        private async Task OnSettingsAsync()
        {
            var ownerWindow = _getMainWindow?.Invoke();
            if (ownerWindow == null) return;

            var settingsVm = new SettingsWindowViewModel(
                _provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<CaptureOption>>(),
                _provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Oss.OssOption>>(),
                _provider.GetRequiredService<IConfigureSaver>(),
                _wallpaperService,
                _loc,
                _provider.GetRequiredService<IWallpaperMonitorProvider>());

            var settingsWindow = new Views.SettingsWindow
            {
                DataContext = settingsVm
            };

            // 使用 ShowDialog 以模态方式打开，符合 Avalonia 规范
            await settingsWindow.ShowDialog(ownerWindow);
        }

        private void OnShowMainWindow()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var mainWindow = _lifetime.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }

        private void OnExit()
        {
            _wallpaperService.StopWallpaperUpdates();
            Dispose();
            Dispatcher.UIThread.Post(() =>
            {
                if (_lifetime.MainWindow is Views.MainWindow mainWindow)
                {
                    mainWindow.AllowClose = true;
                }

                _lifetime.Shutdown();
            });
        }

        private void StartEarthRotation()
        {
            if (_earthRotationTimer != null) return;
            _earthRotationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _earthRotationTimer.Tick += (s, e) =>
            {
                EarthRotationAngle += 2f;
                if (EarthRotationAngle >= 360f) EarthRotationAngle = 0f;
            };
            _earthRotationTimer.Start();
        }

        private void StopEarthRotation()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _earthRotationTimer?.Stop();
                _earthRotationTimer = null;
                EarthRotationAngle = 0f;
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeFromWallpaperServiceEvents();
            StopEarthRotation();
        }
    }
}
