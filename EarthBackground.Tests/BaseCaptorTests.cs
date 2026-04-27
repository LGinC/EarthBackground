using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
                    WallpaperFolder = _tempDirectory
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
        public void FilterImageIdsByClientLocalTime_ShouldUseSatelliteTimeZoneOffset()
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

            Assert.Equal(new[] { "20260427010000", "20260427030000" }, result);
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
            using (var image = new Image<Rgba32>(688, 688))
            {
                image.SaveAsPng(framePath);
            }

            Assert.False(captor.TryGetExistingFrame("20260427040021", out var existingFramePath));
            Assert.Equal(framePath, existingFramePath);
            Assert.False(File.Exists(framePath));
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

            public bool TryGetExistingFrame(string imageId, out string framePath)
            {
                return TryGetExistingFrameImagePath(imageId, out framePath);
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
