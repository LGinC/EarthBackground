using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace EarthBackground.Tests
{
    public class BaseCaptorTests : IDisposable
    {
        private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task BuildFrameSequenceAsync_ShouldNotReportFailedFrame_AndCancelRemainingFrames()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    FrameIntervalMinutes = 10
                }),
                new TestHttpClientFactory(),
                new TestOssProvider());

            var progressCount = 0;
            var waitingFrameStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var waitingFrameCanceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => captor.BuildFramesAsync(
                new[] { "wait", "fail" },
                async (imageId, token) =>
                {
                    if (imageId == "fail")
                    {
                        await waitingFrameStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
                        throw new InvalidOperationException("download failed");
                    }

                    try
                    {
                        waitingFrameStarted.TrySetResult(true);
                        await Task.Delay(TimeSpan.FromMinutes(1), token);
                    }
                    catch (OperationCanceledException)
                    {
                        waitingFrameCanceled.TrySetResult(true);
                        throw;
                    }

                    return "unexpected.png";
                },
                (_, _) => progressCount++,
                TestContext.Current.CancellationToken));

            Assert.Equal("download failed", ex.Message);
            Assert.Equal(0, progressCount);
            await waitingFrameCanceled.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }

        [Fact]
        public void FilterImageIdsByClientLocalTime_ShouldUseLatestAvailableFrameAsWindowEnd()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    FrameIntervalMinutes = 10
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                TimeSpan.FromHours(8),
                new DateTimeOffset(2026, 4, 27, 12, 0, 0, TimeSpan.FromHours(8)));

            var result = captor.FilterImageIds(
                new[] { "20260427110000", "20260427090000", "20260427070000" },
                3);

            Assert.Equal(new[] { "20260427090000", "20260427110000" }, result);
        }

        [Fact]
        public void FilterImageIdsByClientLocalTime_ShouldConvertUtcSatelliteTimeToClientLocalTime()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                TimeSpan.Zero,
                new DateTimeOffset(2026, 4, 27, 12, 0, 0, TimeSpan.FromHours(8)));

            var result = captor.FilterImageIds(
                new[] { "20260427030000", "20260427010000", "20260427000000" },
                3);

            Assert.Equal(new[] { "20260427000000", "20260427010000", "20260427030000" }, result);
        }

        [Fact]
        public void ExpandAndFilterImageIdsByRecentAvailableTime_ShouldBackfillLimitedLatestTimestampLists()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    FrameIntervalMinutes = 10
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                TimeSpan.Zero,
                new DateTimeOffset(2026, 4, 29, 14, 0, 0, TimeSpan.FromHours(8)));

            var latest = new DateTime(2026, 4, 29, 5, 30, 0);
            var limitedLatestTimes = Enumerable
                .Range(0, 100)
                .Select(i => latest.AddMinutes(-10 * i).ToString("yyyyMMddHHmmss"))
                .ToArray();

            var result = captor.ExpandAndFilterImageIds(limitedLatestTimes, 24);

            Assert.Equal(145, result.Length);
            Assert.Equal("20260428053000", result[0]);
            Assert.Equal("20260429053000", result[^1]);
        }

        [Fact]
        public void TryGetExistingFrameImagePath_ShouldDeleteFrameWithUnexpectedSize()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    Resolution = 0,
                    Zoom = 100
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                baseRate: 678);

            var framePath = Path.Combine(_tempDirectory, "frame_20260427040021.png");
            WritePng(framePath, 688, 688, SKColors.Transparent);

            Assert.False(captor.TryGetExistingFrame("20260427040021", out var existingFramePath));
            Assert.Equal(framePath, existingFramePath);
            Assert.False(File.Exists(framePath));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(50)]
        [InlineData(150)]
        public void JoinImageFromDir_ShouldPreserveTilePositionsAndTransparency_WhenResizing(int zoom)
        {
            var tileDirectory = Path.Combine(_tempDirectory, "tiles");
            Directory.CreateDirectory(tileDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    Resolution = Resolution.r_1376,
                    Zoom = zoom
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                baseRate: 8);

            var colors = new[] { SKColors.Red, SKColors.Lime, new SKColor(0, 0, 255, 128), SKColors.Transparent };
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    WritePng(Path.Combine(tileDirectory, $"{row:000}_{column:000}.png"), 8, 8, colors[row * 2 + column]);
                }
            }

            var framePath = Path.Combine(_tempDirectory, "frame_20260427040021.png");
            Assert.Equal(framePath, captor.JoinTiles(tileDirectory, framePath));
            using var image = SKBitmap.Decode(framePath);
            Assert.NotNull(image);
            var expectedSize = 16 * zoom / 100;
            Assert.Equal(expectedSize, image.Width);
            Assert.Equal(expectedSize, image.Height);
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    var pixel = image.GetPixel((2 * column + 1) * expectedSize / 4, (2 * row + 1) * expectedSize / 4);
                    var expected = colors[row * 2 + column];
                    Assert.Equal(expected.Alpha, pixel.Alpha);
                    if (expected.Alpha != 0)
                    {
                        Assert.Equal(expected, pixel);
                    }
                }
            }

            Assert.Empty(Directory.GetFiles(tileDirectory));
            Assert.True(captor.TryGetExistingFrame("20260427040021", out var cachedPath));
            Assert.Equal(framePath, cachedPath);
            using var codec = SKCodec.Create(framePath);
            Assert.Equal(SKEncodedImageFormat.Png, codec.EncodedFormat);
        }

        [Fact]
        public void JoinImageFromDir_ShouldRejectInvalidTile_WithoutDeletingIt()
        {
            Directory.CreateDirectory(_tempDirectory);
            using var captor = new TestCaptor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    Resolution = 0
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(),
                baseRate: 8);
            var tilePath = Path.Combine(_tempDirectory, "000_000.png");
            File.WriteAllText(tilePath, "invalid PNG");
            var outputPath = Path.Combine(_tempDirectory, "output.png");

            var error = Assert.Throws<InvalidDataException>(() => captor.JoinTiles(_tempDirectory, outputPath));

            Assert.Contains(tilePath, error.Message);
            Assert.True(File.Exists(tilePath));
            Assert.False(File.Exists(outputPath));
        }

        private static void WritePng(string path, int width, int height, SKColor color)
        {
            using var image = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            image.Erase(color);
            using var stream = File.Create(path);
            Assert.True(image.Encode(stream, SKEncodedImageFormat.Png, 100));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private sealed class TestCaptor : BaseCaptor
        {
            private readonly TimeSpan _satelliteTimeZoneOffset;
            private readonly DateTimeOffset _clientLocalNow;

            public TestCaptor(
                IOptionsSnapshot<CaptureOption> options,
                IHttpClientFactory factory,
                IOssProvider downloaderProvider,
                TimeSpan? satelliteTimeZoneOffset = null,
                DateTimeOffset? clientLocalNow = null,
                int? baseRate = null)
                : base(options, factory, downloaderProvider)
            {
                _satelliteTimeZoneOffset = satelliteTimeZoneOffset ?? TimeSpan.Zero;
                _clientLocalNow = clientLocalNow ?? DateTimeOffset.Now;
                BaseRate = baseRate ?? BaseRate;
            }

            protected override TimeSpan SatelliteTimeZoneOffset => _satelliteTimeZoneOffset;

            protected override DateTimeOffset ClientLocalNow => _clientLocalNow;

            public Task<IReadOnlyList<string>> BuildFramesAsync(
                IReadOnlyList<string> imageIds,
                Func<string, CancellationToken, Task<string>> frameFactory,
                Action<int, int>? onFrameComplete,
                CancellationToken token)
            {
                return BuildFrameSequenceAsync(imageIds, frameFactory, onFrameComplete, token);
            }

            public string[] FilterImageIds(IEnumerable<string> imageIds, int recentHours)
            {
                return FilterImageIdsByClientLocalTime(imageIds, recentHours);
            }

            public string[] ExpandAndFilterImageIds(IEnumerable<string> imageIds, int recentHours)
            {
                return ExpandAndFilterImageIdsByRecentAvailableTime(imageIds, recentHours);
            }

            public bool TryGetExistingFrame(string imageId, out string framePath)
            {
                return TryGetExistingFrameImagePath(imageId, out framePath);
            }

            public string JoinTiles(string sourceDirectory, string outputPath)
            {
                return JoinImageFromDir(sourceDirectory, outputPath);
            }

            protected override int GetFrameProcessingParallelism() => 2;
        }

        private sealed class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new();
        }

        private sealed class TestOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class
        {
            public TestOptionsSnapshot(T value)
            {
                Value = value;
            }

            public T Value { get; }

            public T Get(string? name) => Value;
        }

        private sealed class TestOssProvider : IOssProvider
        {
            public IOssDownloader GetDownloader(string? name = null) => new TestDownloader();
        }

        private sealed class TestDownloader : IOssDownloader
        {
            public string ProviderName => NameConsts.DirectDownload;

            public event Action<int>? SetTotal
            {
                add { }
                remove { }
            }

            public event Action? SetCurrentProgress
            {
                add { }
                remove { }
            }

            public Task<IEnumerable<(string url, string path)>> DownloadAsync(
                IEnumerable<(string url, string file)> images,
                string directory,
                CancellationToken token = default)
            {
                return Task.FromResult<IEnumerable<(string url, string path)>>(Array.Empty<(string url, string path)>());
            }

            public Task ClearOssAsync(string domain, CancellationToken token = default) => Task.CompletedTask;

            public void Dispose()
            {
            }
        }
    }
}
