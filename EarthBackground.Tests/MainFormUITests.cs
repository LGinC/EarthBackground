using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.ViewModels;
using EarthBackground.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class MainFormUITests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly WallpaperService _wallpaperService;
        private readonly ResourceLocalizationService _localization;
        private readonly Mock<ILogger<MainWindowViewModel>> _loggerMock = new();
        private readonly Mock<IClassicDesktopStyleApplicationLifetime> _lifetimeMock = new();
        private readonly List<MainWindowViewModel> _viewModels = new();
        private readonly List<Window> _windows = new();

        public MainFormUITests()
        {
            _localization = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
            LocalizedStrings.Instance.Attach(_localization);

            var options = new TestOptionsMonitor<CaptureOption>(new CaptureOption
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
                .Throws(new InvalidOperationException("Not used in main window UI tests."));

            _wallpaperService = new WallpaperService(
                _serviceProvider,
                logger.Object,
                options,
                backgroundProvider.Object,
                dynamicWallpaperSetter.Object);

            _lifetimeMock.SetupProperty(x => x.MainWindow, new Window());
            _lifetimeMock.SetupProperty(x => x.ShutdownMode, ShutdownMode.OnExplicitShutdown);
            _lifetimeMock.SetupGet(x => x.Windows).Returns(Array.Empty<Window>());
            _lifetimeMock.SetupGet(x => x.Args).Returns(Array.Empty<string>());
        }

        [AvaloniaFact]
        public void MainWindow_ShouldBindWindowTitleAndHeader()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);

            Assert.Equal(_localization["MainWindow_Title"], window.Title);
            Assert.Equal(_localization["MainWindow_Header"], FindText(window, "HeaderTitleText"));
        }

        [AvaloniaFact]
        public void MainWindow_ShouldReflectButtonBindings()
        {
            var viewModel = CreateViewModel();
            viewModel.IsRunning = false;
            var window = CreateWindow(viewModel);

            var startButton = FindContainingButton(window, "BtnStartText");
            var stopButton = FindContainingButton(window, "BtnStopText");
            var settingsButton = FindContainingButton(window, "BtnSettingsText");
            var exitButton = FindContainingButton(window, "BtnExitText");

            Assert.Equal("Start", FindText(window, "BtnStartText"));
            Assert.Equal("Stop", FindText(window, "BtnStopText"));
            Assert.Equal("Settings", FindText(window, "BtnSettingsText"));
            Assert.Equal("Exit", FindText(window, "BtnExitText"));
            Assert.True(startButton.IsEnabled);
            Assert.False(stopButton.IsEnabled);
            Assert.True(settingsButton.IsEnabled);
            Assert.True(exitButton.IsEnabled);
        }

        [AvaloniaFact]
        public void MainWindow_ShouldReflectStatusProgressAndEarthRotationBindings()
        {
            var viewModel = CreateViewModel();
            viewModel.StatusText = "Downloading...";
            viewModel.ProgressText = "3/5 (60%)";
            viewModel.ProgressValue = 3;
            viewModel.ProgressMax = 5;
            viewModel.EarthRotationAngle = 42;
            viewModel.IsRunning = true;

            var window = CreateWindow(viewModel);
            var progressBar = window.GetLogicalDescendants().OfType<ProgressBar>().FirstOrDefault();

            Assert.Equal(viewModel.StatusText, FindText(window, "StatusTextBlock"));
            Assert.Equal(viewModel.ProgressText, FindText(window, "ProgressTextBlock"));
            Assert.NotNull(progressBar);
            Assert.Equal(viewModel.ProgressValue, progressBar!.Value);
            Assert.Equal(viewModel.ProgressMax, progressBar.Maximum);
        }

        private MainWindowViewModel CreateViewModel()
        {
            var viewModel = new MainWindowViewModel(
                _loggerMock.Object,
                _serviceProvider,
                _wallpaperService,
                _localization,
                _lifetimeMock.Object);
            _viewModels.Add(viewModel);
            return viewModel;
        }

        private MainWindow CreateWindow(MainWindowViewModel viewModel)
        {
            var window = new MainWindow
            {
                DataContext = viewModel,
                Width = 480,
                Height = 300
            };

            window.ApplyTemplate();
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            return window;
        }

        private static string FindText(Control root, string name)
        {
            var control = root.FindControl<TextBlock>(name);
            Assert.NotNull(control);
            return control!.Text ?? string.Empty;
        }

        private static Button FindContainingButton(Control root, string textBlockName)
        {
            var textBlock = root.FindControl<TextBlock>(textBlockName);
            Assert.NotNull(textBlock);
            var button = textBlock!.GetLogicalAncestors().OfType<Button>().FirstOrDefault();
            Assert.NotNull(button);
            return button!;
        }

        public void Dispose()
        {
            foreach (var viewModel in _viewModels)
                viewModel.Dispose();
            _wallpaperService.StopWallpaperUpdates();
            _serviceProvider.Dispose();
        }

        private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
            public T CurrentValue { get; set; }
            public T Get(string? name) => CurrentValue;
            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }
    }
}
