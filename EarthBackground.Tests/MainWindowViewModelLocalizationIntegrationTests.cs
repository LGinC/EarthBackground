using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
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
    public class MainWindowViewModelLocalizationIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<MainWindowViewModel>> _loggerMock = new();
        private readonly Mock<IClassicDesktopStyleApplicationLifetime> _lifetimeMock = new();
        private readonly LocalOptionsMonitor<CaptureOption> _optionsMonitor;
        private readonly ServiceProvider _serviceProvider;
        private readonly WallpaperService _wallpaperService;

        public MainWindowViewModelLocalizationIntegrationTests()
        {
            _optionsMonitor = new LocalOptionsMonitor<CaptureOption>(new CaptureOption
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
                .Throws(new InvalidOperationException("Not used in localization integration tests."));

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
        public void ViewModel_WithResourceLocalization_ZhCn_ShouldNotExposeResourceKeys()
        {
            var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("zh-CN"));
            var viewModel = new MainWindowViewModel(
                _loggerMock.Object,
                _serviceProvider,
                _wallpaperService,
                loc,
                _lifetimeMock.Object);

            Assert.Equal("🌍 Earth Background", viewModel.HeaderTitle);
            Assert.Equal("🚀 开始", viewModel.BtnStart);
            Assert.Equal("⏹ 停止", viewModel.BtnStop);
            Assert.Equal("⚙ 设置", viewModel.BtnSettings);
            Assert.Equal("✕ 退出", viewModel.BtnExit);
            Assert.Equal("等待运行...", viewModel.StatusText);
            Assert.DoesNotContain("MainWindow_", viewModel.HeaderTitle);
            Assert.DoesNotContain("Btn_", viewModel.BtnStart);
        }

        [AvaloniaFact]
        public void ViewModel_WithResourceLocalization_En_ShouldNotExposeResourceKeys()
        {
            var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
            var viewModel = new MainWindowViewModel(
                _loggerMock.Object,
                _serviceProvider,
                _wallpaperService,
                loc,
                _lifetimeMock.Object);

            Assert.Equal("Earth Background", viewModel.HeaderTitle);
            Assert.Equal("🚀 Start", viewModel.BtnStart);
            Assert.Equal("Wait for run", viewModel.StatusText);
        }

        public void Dispose()
        {
            _wallpaperService.StopWallpaperUpdates();
            _serviceProvider.Dispose();
        }

        private sealed class LocalOptionsMonitor<T> : IOptionsMonitor<T>
        {
            public LocalOptionsMonitor(T currentValue) => CurrentValue = currentValue;
            public T CurrentValue { get; set; }
            public T Get(string? name) => CurrentValue;
            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }
    }
}
