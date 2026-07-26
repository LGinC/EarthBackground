using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.ViewModels;
using EarthBackground.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests;

public class MainWindowLocalizationUiTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly WallpaperService _wallpaperService;
    private readonly Mock<ILogger<MainWindowViewModel>> _loggerMock = new();
    private readonly Mock<IClassicDesktopStyleApplicationLifetime> _lifetimeMock = new();

    public MainWindowLocalizationUiTests()
    {
        var options = new LocalOptionsMonitor<CaptureOption>(new CaptureOption
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
            .Throws(new InvalidOperationException("Not used in UI localization tests."));

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
    public void MainWindow_HotBinding_ShouldShowChineseLocalizedTexts()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("zh-CN"));
        LocalizedStrings.Instance.Attach(loc);

        var viewModel = new MainWindowViewModel(
            _loggerMock.Object,
            _serviceProvider,
            _wallpaperService,
            loc,
            _lifetimeMock.Object);

        var window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();

        Assert.Equal("EarthBackground - 地球背景", window.Title);
        Assert.Equal("🌍 Earth Background", FindText(window, "HeaderTitleText"));
        Assert.Equal("等待运行...", FindText(window, "StatusTextBlock"));
        Assert.Equal("开始", FindText(window, "BtnStartText"));
        Assert.Equal("停止", FindText(window, "BtnStopText"));
        Assert.Equal("设置", FindText(window, "BtnSettingsText"));
        Assert.Equal("退出", FindText(window, "BtnExitText"));
        Assert.DoesNotContain("MainWindow_", window.Title ?? string.Empty);
        Assert.DoesNotContain("Btn_", FindText(window, "BtnStartText"));

        window.Close();
    }

    [AvaloniaFact]
    public void MainWindow_HotBinding_ShouldRefreshWhenLanguageChanges()
    {
        var loc = new ResourceLocalizationService(CultureInfo.GetCultureInfo("en-US"));
        LocalizedStrings.Instance.Attach(loc);

        var viewModel = new MainWindowViewModel(
            _loggerMock.Object,
            _serviceProvider,
            _wallpaperService,
            loc,
            _lifetimeMock.Object);

        var window = new MainWindow
        {
            DataContext = viewModel
        };
        window.Show();

        Assert.Equal("Earth Background", FindText(window, "HeaderTitleText"));
        Assert.Equal("Start", FindText(window, "BtnStartText"));

        loc.SetLanguage("zh-CN");

        Assert.Equal("🌍 Earth Background", FindText(window, "HeaderTitleText"));
        Assert.Equal("开始", FindText(window, "BtnStartText"));
        Assert.Equal("EarthBackground - 地球背景", window.Title);

        window.Close();
    }

    private static string FindText(Control root, string name)
    {
        var control = root.FindControl<TextBlock>(name);
        Assert.NotNull(control);
        return control!.Text ?? string.Empty;
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
