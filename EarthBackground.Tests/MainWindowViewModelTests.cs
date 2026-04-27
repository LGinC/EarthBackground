using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class MainWindowViewModelTests : IDisposable
    {
        private readonly Mock<ILogger<MainWindowViewModel>> _loggerMock = new();
        private readonly Mock<IClassicDesktopStyleApplicationLifetime> _lifetimeMock = new();
        private readonly TestOptionsMonitor<CaptureOption> _optionsMonitor;
        private readonly ServiceProvider _serviceProvider;
        private readonly WallpaperService _wallpaperService;

        public MainWindowViewModelTests()
        {
            _optionsMonitor = new TestOptionsMonitor<CaptureOption>(new CaptureOption
            {
                Captor = "TestCaptor",
                Interval = 10,
                SetWallpaper = false,
                SaveWallpaper = false,
                DynamicWallpaper = false
            });

            var backgroundProvider = new Mock<IBackgroudSetProvider>();
            var logger = new Mock<ILogger<WallpaperService>>();
            var dynamicWallpaperSetter = new Mock<IDynamicWallpaperSetter>();
            var monitorProvider = new Mock<IWallpaperMonitorProvider>();

            var services = new ServiceCollection();
            services.AddSingleton(monitorProvider.Object);
            _serviceProvider = services.BuildServiceProvider();

            backgroundProvider
                .Setup(x => x.GetSetter())
                .Throws(new InvalidOperationException("Not used in MainWindowViewModel tests."));

            _wallpaperService = new WallpaperService(
                _serviceProvider,
                logger.Object,
                _optionsMonitor,
                backgroundProvider.Object,
                dynamicWallpaperSetter.Object);

            _lifetimeMock.SetupProperty(x => x.MainWindow, new Window());
            _lifetimeMock.SetupProperty(x => x.ShutdownMode, ShutdownMode.OnExplicitShutdown);
            _lifetimeMock.SetupGet(x => x.Windows).Returns(Array.Empty<Window>());
            _lifetimeMock.SetupGet(x => x.Args).Returns(Array.Empty<string>());
        }

        [AvaloniaFact]
        public void Constructor_ShouldInitializeLocalizedTextsAndButtonStates()
        {
            var viewModel = CreateViewModel();

            Assert.False(string.IsNullOrWhiteSpace(viewModel.WindowTitle));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.HeaderTitle));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.BtnStart));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.BtnStop));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.NotifyHiddenToTray));
            Assert.Equal("等待运行...", viewModel.StatusText);
            Assert.False(viewModel.IsRunning);
            Assert.True(viewModel.CanStart);
            Assert.False(viewModel.CanStop);
        }

        [AvaloniaFact]
        public async Task OnStatusChanged_ShouldMapKnownStatus_AndUpdateRunningFlags()
        {
            var viewModel = CreateViewModel();

            _wallpaperService.StartWallpaperUpdates();
            InvokePrivate(viewModel, "OnStatusChanged", "Initializing...");
            await FlushUiAsync();
            Assert.Equal("初始化中...", viewModel.StatusText);
            Assert.True(viewModel.IsRunning);
            Assert.False(viewModel.CanStart);
            Assert.True(viewModel.CanStop);

            InvokePrivate(viewModel, "OnStatusChanged", "Downloading...");
            await FlushUiAsync();
            Assert.Equal("下载中...", viewModel.StatusText);

            _wallpaperService.StopWallpaperUpdates();
            InvokePrivate(viewModel, "OnStatusChanged", "Stopped");
            await FlushUiAsync();
            Assert.Equal("等待运行...", viewModel.StatusText);
            Assert.False(viewModel.IsRunning);
            Assert.True(viewModel.CanStart);
            Assert.False(viewModel.CanStop);
        }

        [AvaloniaFact]
        public async Task OnProgressChanged_ShouldFormatAndClampProgressValues()
        {
            var viewModel = CreateViewModel();

            InvokePrivate(viewModel, "OnProgressChanged", 3, 5);
            await FlushUiAsync();

            Assert.Equal(5, viewModel.ProgressMax);
            Assert.Equal(3, viewModel.ProgressValue);
            Assert.Equal("3/5 (60%)", viewModel.ProgressText);

            InvokePrivate(viewModel, "OnProgressChanged", 9, 5);
            await FlushUiAsync();

            Assert.Equal(5, viewModel.ProgressValue);
            Assert.Equal("9/5 (180%)", viewModel.ProgressText);
        }

        [AvaloniaFact]
        public async Task OnShowMainWindow_ShouldShowAndActivateConfiguredMainWindow()
        {
            var viewModel = CreateViewModel();
            var window = new Window
            {
                WindowState = WindowState.Minimized
            };
            _lifetimeMock.SetupProperty(x => x.MainWindow, window);

            InvokePrivate(viewModel, "OnShowMainWindow");
            await FlushUiAsync();

            Assert.Equal(WindowState.Normal, window.WindowState);
        }

        [AvaloniaFact]
        public async Task OnExit_ShouldStopServiceAndRequestShutdown()
        {
            var viewModel = CreateViewModel();
            _wallpaperService.StartWallpaperUpdates();

            InvokePrivate(viewModel, "OnExit");
            await FlushUiAsync();

            Assert.False(_wallpaperService.IsRunning);
            _lifetimeMock.Verify(x => x.Shutdown(It.IsAny<int>()), Times.Once);
        }

        private MainWindowViewModel CreateViewModel()
        {
            return new MainWindowViewModel(
                _loggerMock.Object,
                _serviceProvider,
                _wallpaperService,
                new TestLocalizationService(),
                _lifetimeMock.Object);
        }

        private static void InvokePrivate(object instance, string methodName, params object[] parameters)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(instance, parameters);
        }

        private static Task FlushUiAsync()
        {
            return Dispatcher.UIThread.InvokeAsync(() => { }).GetTask();
        }

        public void Dispose()
        {
            _wallpaperService.StopWallpaperUpdates();
            _serviceProvider.Dispose();
        }

        private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            public TestOptionsMonitor(T currentValue)
            {
                CurrentValue = currentValue;
            }

            public T CurrentValue { get; set; }

            public T Get(string? name) => CurrentValue;

            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }

        private sealed class TestLocalizationService : ILocalizationService
        {
            public string this[string key] => key switch
            {
                "MainWindow_Title" => "EarthBackground",
                "MainWindow_Header" => "EarthBackground",
                "Btn_Start" => "Start",
                "Btn_Stop" => "Stop",
                "Btn_Settings" => "Settings",
                "Btn_Exit" => "Exit",
                "Notify_HiddenToTray" => "EarthBackground 仍在系统托盘运行。如需关闭程序，请点击“退出”。",
                "Status_WaitForRun" => "等待运行...",
                "Status_Running" => "运行中",
                "Status_Initializing" => "初始化中...",
                "Status_Downloading" => "下载中...",
                "Status_SettingWallpaper" => "设置壁纸...",
                "Status_Complete" => "已完成",
                _ => key
            };

            public string Format(string key, params object[] args)
            {
                return string.Format(this[key], args);
            }
        }
    }
}
