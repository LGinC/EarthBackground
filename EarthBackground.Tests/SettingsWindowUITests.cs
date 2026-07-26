using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.Oss;
using EarthBackground.ViewModels;
using EarthBackground.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class SettingsWindowUITests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly WallpaperService _wallpaperService;
        private readonly ResourceLocalizationService _localization;
        private readonly Mock<IConfigureSaver> _configureSaverMock = new();
        private readonly Mock<IWallpaperMonitorProvider> _monitorProviderMock = new();
        private readonly System.Collections.Generic.List<Window> _windows = new();

        public SettingsWindowUITests()
        {
            _localization = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
            LocalizedStrings.Instance.Attach(_localization);

            var backgroundProvider = new Mock<IBackgroudSetProvider>();
            var logger = new Mock<ILogger<WallpaperService>>();
            var dynamicWallpaperSetter = new Mock<IDynamicWallpaperSetter>();
            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();

            _monitorProviderMock
                .Setup(x => x.GetMonitors())
                .Returns(new[]
                {
                    new WallpaperMonitor(@"\\?\DISPLAY#MONITOR1", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080),
                    new WallpaperMonitor(@"\\?\DISPLAY#MONITOR2", "DISPLAY2 (2560x1440)", 1920, 0, 2560, 1440)
                });

            backgroundProvider
                .Setup(x => x.GetSetter())
                .Throws(new InvalidOperationException("Not used in settings window UI tests."));

            _wallpaperService = new WallpaperService(
                _serviceProvider,
                logger.Object,
                new TestOptionsMonitor<CaptureOption>(CreateCaptureOption()),
                backgroundProvider.Object,
                dynamicWallpaperSetter.Object);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldBindTitleAndHeader()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);

            Assert.Equal(_localization["Settings_WindowTitle"], window.Title);
            Assert.Contains(
                window.GetLogicalDescendants().OfType<TextBlock>(),
                x => string.Equals(x.Text, _localization["Settings_Header"], StringComparison.Ordinal));
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldShowExpectedActionButtonsAndOptions()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);
            var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();
            var checkBoxes = window.GetLogicalDescendants().OfType<CheckBox>().ToList();
            var comboBoxes = window.GetLogicalDescendants().OfType<ComboBox>().ToList();

            Assert.Contains(buttons, x => string.Equals(x.Content?.ToString(), viewModel.Label_ChoosePath, StringComparison.Ordinal));
            Assert.Contains(buttons, x => string.Equals(x.Content?.ToString(), viewModel.Label_Save, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_AutoStart, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_DynamicWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_SetWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_SaveWallpaper, StringComparison.Ordinal));
            Assert.Contains(checkBoxes, x => string.Equals(x.Content?.ToString(), viewModel.Label_AllDynamicWallpaperMonitors, StringComparison.Ordinal));
            Assert.True(comboBoxes.Count >= 4);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldReflectDynamicWallpaperFieldVisibility()
        {
            var viewModel = CreateViewModel();
            var window = CreateWindow(viewModel);
            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();

            var recentHoursLabel = FindTextBlock(textBlocks, viewModel.Label_RecentHours);
            var frameIntervalLabel = FindTextBlock(textBlocks, viewModel.Label_FrameInterval);
            var loopPauseLabel = FindTextBlock(textBlocks, viewModel.Label_LoopPauseMilliseconds);

            Assert.True(recentHoursLabel.IsVisible);
            Assert.True(frameIntervalLabel.IsVisible);
            Assert.True(loopPauseLabel.IsVisible);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldShowDynamicWallpaperMonitorSelection()
        {
            var viewModel = CreateViewModel();
            viewModel.AllDynamicWallpaperMonitors = false;
            var window = CreateWindow(viewModel);
            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
            var monitorList = window.GetLogicalDescendants().OfType<ItemsControl>()
                .FirstOrDefault(x => ReferenceEquals(x.ItemsSource, viewModel.DynamicWallpaperMonitors));

            Assert.NotNull(FindTextBlock(textBlocks, viewModel.Label_DynamicWallpaperMonitors));
            Assert.NotNull(monitorList);
            Assert.True(monitorList!.IsVisible);
            Assert.Same(viewModel.DynamicWallpaperMonitors, monitorList.ItemsSource);
        }

        [AvaloniaFact]
        public void SettingsWindow_ShouldHideDynamicWallpaperMonitorSelection_WhenDynamicWallpaperDisabled()
        {
            var viewModel = CreateViewModel();
            viewModel.DynamicWallpaper = false;
            var window = CreateWindow(viewModel);
            var textBlocks = window.GetLogicalDescendants().OfType<TextBlock>().ToList();
            var monitorLabel = FindTextBlock(textBlocks, viewModel.Label_DynamicWallpaperMonitors);

            Assert.False(monitorLabel.IsVisible);
        }

        private SettingsWindowViewModel CreateViewModel()
        {
            return new SettingsWindowViewModel(
                new TestOptionsMonitor<CaptureOption>(CreateCaptureOption()),
                new TestOptionsMonitor<OssOption>(new OssOption()),
                _configureSaverMock.Object,
                _wallpaperService,
                _localization,
                _monitorProviderMock.Object);
        }

        private static CaptureOption CreateCaptureOption()
        {
            return new CaptureOption
            {
                Captor = NameConsts.Fy4,
                AutoStart = true,
                DynamicWallpaper = true,
                SetWallpaper = true,
                SaveWallpaper = false,
                SavePath = "images",
                WallpaperFolder = "images",
                Resolution = Resolution.r_2752,
                Interval = 20,
                FrameIntervalMinutes = 10,
                Zoom = 100,
                RecentHours = 24,
                LoopPauseMilliseconds = 3000,
                DynamicWallpaperMonitorIds = Array.Empty<string>()
            };
        }

        private SettingsWindow CreateWindow(SettingsWindowViewModel viewModel)
        {
            var window = new SettingsWindow
            {
                DataContext = viewModel,
                Width = 760,
                Height = 680
            };

            window.ApplyTemplate();
            window.Measure(new Size(window.Width, window.Height));
            window.Arrange(new Rect(0, 0, window.Width, window.Height));
            return window;
        }

        private static TextBlock FindTextBlock(System.Collections.Generic.IEnumerable<TextBlock> textBlocks, string text)
        {
            var result = textBlocks.FirstOrDefault(x => string.Equals(x.Text, text, StringComparison.Ordinal));
            Assert.NotNull(result);
            return result!;
        }

        public void Dispose()
        {
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
