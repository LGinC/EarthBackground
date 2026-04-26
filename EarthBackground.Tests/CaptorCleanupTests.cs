using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using Xunit;

namespace EarthBackground.Tests
{
    public class CaptorCleanupTests : IDisposable
    {
        private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task FY4Captor_ShouldDeleteFrameDirectory_WhenFrameDownloadFails()
        {
            Directory.CreateDirectory(_tempDirectory);
            var downloader = new FailingDownloader();
            using var captor = new FY4Captor(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = _tempDirectory,
                    WallpaperFolder = _tempDirectory,
                    Resolution = 0,
                    Zoom = 100
                }),
                new TestHttpClientFactory(),
                new TestOssProvider(downloader));

            var imageId = "20260426185000";
            var frameDir = Path.Combine(_tempDirectory, $"frame_{imageId}");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeGetOrCreateFrameAsync(captor, imageId));

            Assert.Equal("download failed", ex.Message);
            Assert.False(Directory.Exists(frameDir));
        }

        [Theory]
        [MemberData(nameof(CiraSliderCaptorCases))]
        public async Task CiraSliderCaptors_ShouldUseLatestSatelliteTilePath(
            CiraSliderCaptor captor,
            string expectedTilePathSegment)
        {
            Directory.CreateDirectory(_tempDirectory);
            var downloader = new CapturingDownloader();
            captor.Downloader = downloader;

            await InvokeSaveImageAsync(captor, "20260426185000", Path.Combine(_tempDirectory, "frame_20260426185000"));

            Assert.Equal(
                $"https://slider.cira.colostate.edu/data/imagery/2026/04/26/{expectedTilePathSegment}/20260426185000/00/000_000.png",
                downloader.Url);
        }

        public static IEnumerable<object[]> CiraSliderCaptorCases()
        {
            yield return new object[] { CreateCiraCaptor(static (options, factory, provider) => new HimawariCaptor(options, factory, provider)), "himawari---full_disk/geocolor" };
            yield return new object[] { CreateCiraCaptor(static (options, factory, provider) => new GoesCaptor(options, factory, provider)), "goes-19---full_disk/geocolor" };
            yield return new object[] { CreateCiraCaptor(static (options, factory, provider) => new GeoKompsatCaptor(options, factory, provider)), "gk2a---full_disk/geocolor" };
            yield return new object[] { CreateCiraCaptor(static (options, factory, provider) => new MeteosatCaptor(options, factory, provider)), "meteosat-12---full_disk/geocolor" };
            yield return new object[] { CreateCiraCaptor(static (options, factory, provider) => new JpssCaptor(options, factory, provider)), "jpss---northern_hemisphere/cira_geocolor" };
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private static async Task<string> InvokeGetOrCreateFrameAsync(FY4Captor captor, string imageId)
        {
            var method = typeof(FY4Captor).GetMethod("GetOrCreateFrameAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = method!.Invoke(captor, new object[] { imageId, TestContext.Current.CancellationToken }) as Task<string>;
            Assert.NotNull(task);
            return await task!;
        }

        private static CiraSliderCaptor CreateCiraCaptor(
            Func<IOptionsSnapshot<CaptureOption>, IHttpClientFactory, IOssProvider, CiraSliderCaptor> factory)
        {
            return factory(
                new TestOptionsSnapshot<CaptureOption>(new CaptureOption
                {
                    SavePath = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N")),
                    WallpaperFolder = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N")),
                    Resolution = 0,
                    Zoom = 100
                }),
                new TestHttpClientFactory(new Uri("https://slider.cira.colostate.edu/data/")),
                new TestOssProvider(new CapturingDownloader()));
        }

        private static async Task InvokeSaveImageAsync(CiraSliderCaptor captor, string imageId, string saveDir)
        {
            var method = typeof(CiraSliderCaptor).GetMethod("SaveImageAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var task = method!.Invoke(captor, new object[] { imageId, saveDir, TestContext.Current.CancellationToken }) as Task;
            Assert.NotNull(task);
            await task!;
        }

        private sealed class FailingDownloader : IOssDownloader
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
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "000_000.png.download"), "partial");
                throw new InvalidOperationException("download failed");
            }

            public Task ClearOssAsync(string domain, CancellationToken token = default) => Task.CompletedTask;

            public void Dispose()
            {
            }
        }

        private sealed class CapturingDownloader : IOssDownloader
        {
            public string ProviderName => NameConsts.DirectDownload;

            public string? Url { get; private set; }

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
                foreach (var (url, file) in images)
                {
                    Url = url;
                    return Task.FromResult<IEnumerable<(string url, string path)>>(
                        new[] { (url, Path.Combine(directory, file)) });
                }

                return Task.FromResult<IEnumerable<(string url, string path)>>(Array.Empty<(string url, string path)>());
            }

            public Task ClearOssAsync(string domain, CancellationToken token = default) => Task.CompletedTask;

            public void Dispose()
            {
            }
        }

        private sealed class TestHttpClientFactory : IHttpClientFactory
        {
            private readonly Uri? _baseAddress;

            public TestHttpClientFactory(Uri? baseAddress = null)
            {
                _baseAddress = baseAddress;
            }

            public HttpClient CreateClient(string name) => new()
            {
                BaseAddress = _baseAddress
            };
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
            private readonly IOssDownloader _downloader;

            public TestOssProvider(IOssDownloader downloader)
            {
                _downloader = downloader;
            }

            public IOssDownloader GetDownloader(string? name = null) => _downloader;
        }
    }
}
