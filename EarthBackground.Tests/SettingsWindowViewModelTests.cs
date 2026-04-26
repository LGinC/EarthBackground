using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Background;
using EarthBackground.Localization;
using EarthBackground.Oss;
using EarthBackground.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class SettingsWindowViewModelTests : IDisposable
    {
        private readonly CaptureOption _captureOption;
        private readonly OssOption _ossOption;
        private readonly TestOptionsMonitor<CaptureOption> _captureOptionsMonitor;
        private readonly TestOptionsMonitor<OssOption> _ossOptionsMonitor;
        private readonly Mock<IConfigureSaver> _configureSaverMock = new();
        private readonly Mock<IWallpaperMonitorProvider> _monitorProviderMock = new();
        private readonly WallpaperService _wallpaperService;
        private readonly ServiceProvider _serviceProvider;
        private readonly string _imageIdPath;

        public SettingsWindowViewModelTests()
        {
            _captureOption = new CaptureOption
            {
                Captor = NameConsts.Fy4,
                AutoStart = false,
                SetWallpaper = true,
                SaveWallpaper = true,
                SavePath = "images",
                WallpaperFolder = "images",
                Resolution = Resolution.r_2752,
                Zoom = 80,
                Interval = 20,
                DynamicWallpaper = true,
                FrameIntervalMs = 500,
                RecentHours = 24,
                LoopPauseMilliseconds = 3000,
                DynamicWallpaperMonitorIds = Array.Empty<string>()
            };

            _ossOption = new OssOption
            {
                CloudName = NameConsts.Qiqiuyun,
                UserName = "user",
                ApiKey = "key",
                ApiSecret = "secret",
                Zone = "z1",
                Bucket = "bucket",
                Domain = "domain.example.com",
                IsEnable = true
            };

            _captureOptionsMonitor = new TestOptionsMonitor<CaptureOption>(_captureOption);
            _ossOptionsMonitor = new TestOptionsMonitor<OssOption>(_ossOption);

            var backgroundProvider = new Mock<IBackgroudSetProvider>();
            var logger = new Mock<ILogger<WallpaperService>>();
            var dynamicLogger = new Mock<ILogger<WindowsDynamicWallpaperSetter>>();

            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
            _monitorProviderMock
                .Setup(x => x.GetMonitors())
                .Returns(new[]
                {
                    new WallpaperMonitor(@"\\?\DISPLAY#MONITOR1", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080),
                    new WallpaperMonitor(@"\\?\DISPLAY#MONITOR2", "DISPLAY2 (2560x1440)", 1920, 0, 2560, 1440)
                });

            var dynamicWallpaperSetter = new WindowsDynamicWallpaperSetter(
                dynamicLogger.Object,
                _serviceProvider,
                _captureOptionsMonitor,
                _monitorProviderMock.Object);

            backgroundProvider
                .Setup(x => x.GetSetter())
                .Throws(new InvalidOperationException("Not used in SettingsWindowViewModel tests."));

            _wallpaperService = new WallpaperService(
                _serviceProvider,
                logger.Object,
                _captureOptionsMonitor,
                backgroundProvider.Object,
                dynamicWallpaperSetter);

            _imageIdPath = NameConsts.ImageIdPath;
            if (File.Exists(_imageIdPath))
            {
                File.Delete(_imageIdPath);
            }
        }

        [Fact]
        public void Constructor_ShouldInitializeSelectionsAndLocalizedLabels()
        {
            var viewModel = CreateViewModel();

            Assert.Equal(NameConsts.Fy4, viewModel.SelectedCaptor?.Value);
            Assert.Equal(Resolution.r_2752, viewModel.SelectedResolution?.Value);
            Assert.Equal(NameConsts.Qiqiuyun, viewModel.SelectedDownloader?.Value);
            Assert.Equal("z1", viewModel.SelectedZone?.Value);
            Assert.True(viewModel.DynamicWallpaper);
            Assert.True(viewModel.SaveWallpaper);
            Assert.True(viewModel.ChooseSavePathEnabled);
            Assert.True(viewModel.AllDynamicWallpaperMonitors);
            Assert.False(viewModel.DynamicWallpaperMonitorListVisible);
            Assert.Equal(2, viewModel.DynamicWallpaperMonitors.Count);
            Assert.False(string.IsNullOrWhiteSpace(viewModel.WindowTitle));
            Assert.False(string.IsNullOrWhiteSpace(viewModel.Label_DynamicWallpaper));
            Assert.Equal(ResolveInAppDirectory(_captureOption.SavePath), viewModel.SavePath);
        }

        [Fact]
        public async Task Save_ShouldPersistSelectedDynamicWallpaperMonitors()
        {
            var viewModel = CreateViewModel();
            CaptureOption? savedCapture = null;
            _configureSaverMock
                .Setup(x => x.SaveAsync(It.IsAny<CaptureOption>(), It.IsAny<OssOption>()))
                .Callback<CaptureOption, OssOption>((capture, _) => savedCapture = capture)
                .Returns(Task.CompletedTask);

            viewModel.AllDynamicWallpaperMonitors = false;
            viewModel.DynamicWallpaperMonitors[0].IsSelected = true;
            viewModel.DynamicWallpaperMonitors[1].IsSelected = true;

            await InvokeOnSaveAsync(viewModel);

            Assert.NotNull(savedCapture);
            Assert.Equal(
                new[] { @"\\?\DISPLAY#MONITOR1", @"\\?\DISPLAY#MONITOR2" },
                savedCapture!.DynamicWallpaperMonitorIds);
        }

        [Fact]
        public async Task Save_ShouldPersistEmptyMonitorIds_WhenAllDynamicWallpaperMonitorsSelected()
        {
            var viewModel = CreateViewModel();
            CaptureOption? savedCapture = null;
            _configureSaverMock
                .Setup(x => x.SaveAsync(It.IsAny<CaptureOption>(), It.IsAny<OssOption>()))
                .Callback<CaptureOption, OssOption>((capture, _) => savedCapture = capture)
                .Returns(Task.CompletedTask);

            viewModel.AllDynamicWallpaperMonitors = true;
            viewModel.DynamicWallpaperMonitors[0].IsSelected = true;

            await InvokeOnSaveAsync(viewModel);

            Assert.NotNull(savedCapture);
            Assert.Empty(savedCapture!.DynamicWallpaperMonitorIds);
        }

        [Fact]
        public void SaveWallpaper_Setter_ShouldToggleChooseSavePathEnabled()
        {
            var viewModel = CreateViewModel();

            viewModel.SaveWallpaper = false;
            Assert.False(viewModel.ChooseSavePathEnabled);

            viewModel.SaveWallpaper = true;
            Assert.True(viewModel.ChooseSavePathEnabled);
        }

        [Fact]
        public void AllDynamicWallpaperMonitors_Setter_ShouldSelectFirstMonitor_WhenSwitchingToCustomSelection()
        {
            var viewModel = CreateViewModel();

            Assert.True(viewModel.AllDynamicWallpaperMonitors);
            Assert.All(viewModel.DynamicWallpaperMonitors, monitor => Assert.False(monitor.IsSelected));

            viewModel.AllDynamicWallpaperMonitors = false;

            Assert.False(viewModel.AllDynamicWallpaperMonitors);
            Assert.True(viewModel.DynamicWallpaperMonitorListVisible);
            Assert.True(viewModel.DynamicWallpaperMonitors[0].IsSelected);
        }

        [Fact]
        public void SelectedDownloader_ShouldUpdateFieldAvailability()
        {
            var viewModel = CreateViewModel();

            viewModel.SelectedDownloader = new NameValue<string>("Direct Download", NameConsts.DirectDownload);
            Assert.False(viewModel.UsernameEnabled);
            Assert.False(viewModel.ApiKeyEnabled);
            Assert.False(viewModel.ApiSecretEnabled);
            Assert.False(viewModel.ZoneEnabled);
            Assert.False(viewModel.DomainEnabled);
            Assert.False(viewModel.BucketEnabled);

            viewModel.SelectedDownloader = new NameValue<string>("Cloudinary", NameConsts.Cloudinary);
            Assert.True(viewModel.UsernameEnabled);
            Assert.True(viewModel.ApiKeyEnabled);
            Assert.True(viewModel.ApiSecretEnabled);
            Assert.False(viewModel.ZoneEnabled);
            Assert.False(viewModel.DomainEnabled);
            Assert.False(viewModel.BucketEnabled);

            viewModel.SelectedDownloader = new NameValue<string>("Qiniu Cloud", NameConsts.Qiqiuyun);
            Assert.False(viewModel.UsernameEnabled);
            Assert.True(viewModel.ApiKeyEnabled);
            Assert.True(viewModel.ApiSecretEnabled);
            Assert.True(viewModel.ZoneEnabled);
            Assert.True(viewModel.DomainEnabled);
            Assert.True(viewModel.BucketEnabled);
            Assert.True(viewModel.Zones.Count >= 5);
        }

        [Fact]
        public async Task Save_ShouldPersistUpdatedCaptureAndOssOptions()
        {
            var viewModel = CreateViewModel();
            File.WriteAllText(_imageIdPath, "old-image-id");

            CaptureOption? savedCapture = null;
            OssOption? savedOss = null;
            _configureSaverMock
                .Setup(x => x.SaveAsync(It.IsAny<CaptureOption>(), It.IsAny<OssOption>()))
                .Callback<CaptureOption, OssOption>((capture, oss) =>
                {
                    savedCapture = capture;
                    savedOss = oss;
                })
                .Returns(Task.CompletedTask);

            viewModel.SelectedCaptor = new NameValue<string>("Himawari-9", NameConsts.Himawari);
            viewModel.SelectedResolution = new NameValue<Resolution>("1376*1376", Resolution.r_1376);
            viewModel.SelectedDownloader = new NameValue<string>("Cloudinary", NameConsts.Cloudinary);
            viewModel.Interval = 15;
            viewModel.Zoom = 90;
            viewModel.RecentHours = 12;
            viewModel.LoopPauseMilliseconds = 1500;
            viewModel.DynamicWallpaper = true;
            viewModel.SetWallpaper = false;
            viewModel.SaveWallpaper = true;
            viewModel.ApiKey = "new-key";
            viewModel.ApiSecret = "new-secret";
            viewModel.Username = "new-user";
            viewModel.Domain = "cdn.example.com";
            viewModel.Bucket = "new-bucket";

            await InvokeOnSaveAsync(viewModel);

            _configureSaverMock.Verify(x => x.SaveAsync(It.IsAny<CaptureOption>(), It.IsAny<OssOption>()), Times.Once);
            Assert.NotNull(savedCapture);
            Assert.NotNull(savedOss);

            Assert.Equal(NameConsts.Himawari, savedCapture!.Captor);
            Assert.Equal(Resolution.r_1376, savedCapture.Resolution);
            Assert.Equal(15, savedCapture.Interval);
            Assert.Equal(90, savedCapture.Zoom);
            Assert.Equal(12, savedCapture.RecentHours);
            Assert.Equal(1500, savedCapture.LoopPauseMilliseconds);
            Assert.True(savedCapture.DynamicWallpaper);
            Assert.False(savedCapture.SetWallpaper);
            Assert.True(savedCapture.SaveWallpaper);
            Assert.Equal(ResolveInAppDirectory(_captureOption.SavePath), savedCapture.SavePath);
            Assert.Empty(savedCapture.DynamicWallpaperMonitorIds);

            Assert.True(savedOss!.IsEnable);
            Assert.Equal(NameConsts.Cloudinary, savedOss.CloudName);
            Assert.Equal("new-user", savedOss.UserName);
            Assert.Equal("new-key", savedOss.ApiKey);
            Assert.Equal("new-secret", savedOss.ApiSecret);
            Assert.Equal("new-bucket", savedOss.Bucket);
            Assert.Equal("http://cdn.example.com", savedOss.Domain);

            Assert.False(File.Exists(_imageIdPath));
        }

        [Fact]
        public async Task Save_ShouldClearImageCache_WhenCaptorChanges()
        {
            var saveDir = CreateTempDirectory();
            _captureOption.SavePath = saveDir;
            _captureOption.WallpaperFolder = saveDir;
            _captureOption.Captor = NameConsts.Fy4;

            var frameFile = Path.Combine(saveDir, "frame_20260425203000.png");
            var wallpaperFile = Path.Combine(saveDir, "wallpaper.png");
            var partialFile = Path.Combine(saveDir, "000_000.png.download");
            var keepFile = Path.Combine(saveDir, "saved-wallpaper.png");
            var frameDir = Path.Combine(saveDir, "frame_20260425203000");
            Directory.CreateDirectory(frameDir);
            File.WriteAllText(frameFile, "frame");
            File.WriteAllText(wallpaperFile, "wallpaper");
            File.WriteAllText(partialFile, "partial");
            File.WriteAllText(keepFile, "keep");
            File.WriteAllText(Path.Combine(frameDir, "000_000.png"), "tile");

            var viewModel = CreateViewModel();
            viewModel.SelectedCaptor = new NameValue<string>("GOES-19", NameConsts.Goes);

            _configureSaverMock
                .Setup(x => x.SaveAsync(It.IsAny<CaptureOption>(), It.IsAny<OssOption>()))
                .Returns(Task.CompletedTask);

            await InvokeOnSaveAsync(viewModel);

            Assert.False(File.Exists(frameFile));
            Assert.False(File.Exists(wallpaperFile));
            Assert.False(File.Exists(partialFile));
            Assert.False(Directory.Exists(frameDir));
            Assert.True(File.Exists(keepFile));
        }

        private SettingsWindowViewModel CreateViewModel()
        {
            return new SettingsWindowViewModel(
                _captureOptionsMonitor,
                _ossOptionsMonitor,
                _configureSaverMock.Object,
                _wallpaperService,
                new ResourceLocalizationService(),
                _monitorProviderMock.Object);
        }

        private static async Task InvokeOnSaveAsync(SettingsWindowViewModel viewModel)
        {
            var method = typeof(SettingsWindowViewModel).GetMethod("OnSave", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = method!.Invoke(viewModel, null) as Task;
            Assert.NotNull(task);
            await task!;
        }

        private static string ResolveInAppDirectory(string? path)
        {
            var baseDirectory = AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(baseDirectory, "images");
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path);
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            if (File.Exists(_imageIdPath))
            {
                File.Delete(_imageIdPath);
            }

            if (Path.IsPathRooted(_captureOption.SavePath) && Directory.Exists(_captureOption.SavePath))
            {
                Directory.Delete(_captureOption.SavePath, true);
            }

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
    }
}
