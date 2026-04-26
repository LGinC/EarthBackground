using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Captors;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
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
            var waitingFrameCanceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => captor.BuildFramesAsync(
                new[] { "fail", "wait" },
                async (imageId, token) =>
                {
                    if (imageId == "fail")
                    {
                        throw new InvalidOperationException("download failed");
                    }

                    try
                    {
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

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private sealed class TestCaptor : BaseCaptor
        {
            public TestCaptor(
                IOptionsSnapshot<CaptureOption> options,
                IHttpClientFactory factory,
                IOssProvider downloaderProvider)
                : base(options, factory, downloaderProvider)
            {
            }

            public Task<IReadOnlyList<string>> BuildFramesAsync(
                IReadOnlyList<string> imageIds,
                Func<string, CancellationToken, Task<string>> frameFactory,
                Action<int, int>? onFrameComplete,
                CancellationToken token)
            {
                return BuildFrameSequenceAsync(imageIds, frameFactory, onFrameComplete, token);
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
