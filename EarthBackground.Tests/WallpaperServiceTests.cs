using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class WallpaperServiceTests : IDisposable
    {
        private readonly Mock<ILogger<WallpaperService>> _loggerMock = new();
        private readonly Mock<IBackgroudSetProvider> _backgroundSetProviderMock = new();
        private readonly Mock<ICaptor> _captorMock = new();
        private readonly Mock<IOssDownloader> _downloaderMock = new();
        private readonly Mock<IBackgroundSetter> _setterMock = new();
        private readonly Mock<ILogger<WindowsDynamicWallpaperSetter>> _dynamicLoggerMock = new();
        private readonly TestOptionsMonitor<CaptureOption> _optionsMonitor;
        private readonly ServiceProvider _serviceProvider;
        private readonly WindowsDynamicWallpaperSetter _dynamicWallpaperSetter;
        private readonly List<string> _tempDirectories = new();

        public WallpaperServiceTests()
        {
            _optionsMonitor = new TestOptionsMonitor<CaptureOption>(new CaptureOption
            {
                Captor = "TestCaptor",
                Interval = 10,
                SetWallpaper = true,
                SaveWallpaper = false,
                SavePath = "images",
                DynamicWallpaper = false,
                RecentHours = 24,
                FrameIntervalMs = 500
            });

            _captorMock.SetupProperty(c => c.Downloader, _downloaderMock.Object);
            _backgroundSetProviderMock.Setup(x => x.GetSetter()).Returns(_setterMock.Object);

            var services = new ServiceCollection();
            services.AddKeyedSingleton<ICaptor>("TestCaptor", _captorMock.Object);
            _serviceProvider = services.BuildServiceProvider();

            _dynamicWallpaperSetter = new WindowsDynamicWallpaperSetter(
                _dynamicLoggerMock.Object,
                _serviceProvider,
                _optionsMonitor);
        }

        [Fact]
        public void Start_ShouldSetStatusToRunning()
        {
            var service = CreateService();
            string? status = null;
            service.StatusChanged += s => status = s;

            service.Start();

            Assert.True(service.IsRunning);
            Assert.Equal("Running", status);
        }

        [Fact]
        public void Stop_ShouldSetStatusToStopped_AndStopDynamicWallpaper()
        {
            var service = CreateService();
            string? status = null;
            service.StatusChanged += s => status = s;
            service.Start();

            service.Stop();

            Assert.False(service.IsRunning);
            Assert.Equal("Stopped", status);
        }

        [Fact]
        public async Task RunCycle_StaticWallpaper_ShouldSetWallpaper_ReportProgress_AndEmitImageSaved()
        {
            _optionsMonitor.CurrentValue = new CaptureOption
            {
                Captor = "TestCaptor",
                DynamicWallpaper = false,
                SetWallpaper = true,
                SaveWallpaper = false,
                Interval = 10
            };

            _captorMock
                .Setup(x => x.GetImagePath(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken token) =>
                {
                    _downloaderMock.Raise(d => d.SetTotal += null!, 3);
                    _downloaderMock.Raise(d => d.SetCurrentProgress += null!);
                    _downloaderMock.Raise(d => d.SetCurrentProgress += null!);
                    await Task.Yield();
                    token.ThrowIfCancellationRequested();
                    return "wallpaper.png";
                });

            var service = CreateService();
            var statuses = new List<string>();
            var progress = new List<(int current, int total)>();
            string? savedImage = null;

            service.StatusChanged += statuses.Add;
            service.ProgressChanged += (current, total) => progress.Add((current, total));
            service.ImageSaved += path => savedImage = path;

            await InvokeRunCycleInternalAsync(service);

            _setterMock.Verify(x => x.SetBackgroundAsync("wallpaper.png", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal("wallpaper.png", savedImage);
            Assert.Contains("Initializing...", statuses);
            Assert.Contains("Downloading...", statuses);
            Assert.Contains("Setting Wallpaper...", statuses);
            Assert.Contains("Complete", statuses);
            Assert.Contains((0, 3), progress);
            Assert.Contains((1, 3), progress);
            Assert.Contains((2, 3), progress);
            Assert.Equal((3, 3), progress[^1]);
        }

        [Fact]
        public async Task RunCycle_DynamicWallpaper_ShouldUseFrameProgress_AndSkipStaticSetter_WhenSetWallpaperDisabled()
        {
            _optionsMonitor.CurrentValue = new CaptureOption
            {
                Captor = "TestCaptor",
                DynamicWallpaper = true,
                SetWallpaper = false,
                SaveWallpaper = false,
                RecentHours = 24,
                FrameIntervalMs = 400
            };

            _captorMock
                .Setup(x => x.GetImagePaths(
                    24,
                    It.IsAny<Action<int, int>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, Action<int, int>? onFrameComplete, CancellationToken _) =>
                {
                    onFrameComplete?.Invoke(1, 3);
                    onFrameComplete?.Invoke(2, 3);
                    onFrameComplete?.Invoke(3, 3);
                    return (IReadOnlyList<string>)new[]
                    {
                        "frame_20260407170000.png",
                        "frame_20260407171000.png",
                        "frame_20260407172000.png"
                    };
                });

            var service = CreateService();
            var progress = new List<(int current, int total)>();
            var statuses = new List<string>();
            string? savedImage = null;

            service.ProgressChanged += (current, total) => progress.Add((current, total));
            service.StatusChanged += statuses.Add;
            service.ImageSaved += path => savedImage = path;

            await InvokeRunCycleInternalAsync(service);

            _setterMock.Verify(x => x.SetBackgroundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Equal("frame_20260407170000.png", savedImage);
            Assert.Contains("Initializing...", statuses);
            Assert.Contains("Downloading...", statuses);
            Assert.Contains("Complete", statuses);
            Assert.DoesNotContain("Setting Wallpaper...", statuses);
            Assert.Contains((1, 5), progress);
            Assert.Contains((2, 5), progress);
            Assert.Contains((3, 5), progress);
            Assert.Equal((5, 5), progress[^1]);
        }

        [Fact]
        public async Task RunCycle_SaveWallpaperEnabled_ShouldCopyImageToConfiguredDirectory()
        {
            var sourceDir = CreateTempDirectory();
            var saveDir = CreateTempDirectory();
            var imagePath = Path.Combine(sourceDir, "wallpaper.png");
            await File.WriteAllTextAsync(imagePath, "test-image");

            _optionsMonitor.CurrentValue = new CaptureOption
            {
                Captor = "TestCaptor",
                DynamicWallpaper = false,
                SetWallpaper = false,
                SaveWallpaper = true,
                SavePath = saveDir
            };

            _captorMock
                .Setup(x => x.GetImagePath(It.IsAny<CancellationToken>()))
                .ReturnsAsync(imagePath);

            var service = CreateService();

            await InvokeRunCycleInternalAsync(service);

            var copiedFile = Path.Combine(saveDir, "wallpaper.png");
            Assert.True(File.Exists(copiedFile));
            Assert.Equal("test-image", await File.ReadAllTextAsync(copiedFile));
        }

        [Fact]
        public async Task RunCycle_ShouldCallCaptorResetAfterFourCycles()
        {
            _optionsMonitor.CurrentValue = new CaptureOption
            {
                Captor = "TestCaptor",
                DynamicWallpaper = false,
                SetWallpaper = false,
                SaveWallpaper = false
            };

            _captorMock
                .Setup(x => x.GetImagePath(It.IsAny<CancellationToken>()))
                .ReturnsAsync("wallpaper.png");

            var service = CreateService();

            for (int i = 0; i < 4; i++)
            {
                await InvokeRunCycleInternalAsync(service);
            }

            _captorMock.Verify(x => x.ResetAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private WallpaperService CreateService()
        {
            return new WallpaperService(
                _serviceProvider,
                _loggerMock.Object,
                _optionsMonitor,
                _backgroundSetProviderMock.Object,
                _dynamicWallpaperSetter);
        }

        private static async Task InvokeRunCycleInternalAsync(WallpaperService service, CancellationToken token = default)
        {
            var method = typeof(WallpaperService).GetMethod("RunCycleInternalAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = method!.Invoke(service, new object[] { token }) as Task;
            Assert.NotNull(task);
            await task!;
        }

        private string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            _tempDirectories.Add(path);
            return path;
        }

        public void Dispose()
        {
            _dynamicWallpaperSetter.Dispose();
            _serviceProvider.Dispose();

            foreach (var dir in _tempDirectories)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
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
