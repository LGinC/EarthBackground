using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using Xunit;

namespace EarthBackground.Tests
{
    public class DirectDownloaderTests : IDisposable
    {
        private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task DownloadAsync_ShouldRetry_WhenResponseStreamEndsPrematurely()
        {
            Directory.CreateDirectory(_tempDirectory);
            var handler = new SequenceHandler(
                () => new StreamContent(new ThrowingReadStream(new byte[] { 1, 2, 3, 4 })),
                () => new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5, 6 }));
            var downloader = new DirectDownloader(new TestHttpClientFactory(new HttpClient(handler)));
            var progressCount = 0;
            downloader.SetCurrentProgress += () => progressCount++;

            await downloader.DownloadAsync(
                new[] { ("https://example.test/tile.png", "tile.png") },
                _tempDirectory,
                TestContext.Current.CancellationToken);

            var path = Path.Combine(_tempDirectory, "tile.png");
            Assert.True(File.Exists(path));
            Assert.Equal(
                new byte[] { 1, 2, 3, 4, 5, 6 },
                await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal(1, progressCount);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task CloudinaryDownloadAsync_ShouldUseSharedRetryDownloader()
        {
            Directory.CreateDirectory(_tempDirectory);
            var handler = new SequenceHandler(
                () => new StreamContent(new ThrowingReadStream(new byte[] { 1, 2, 3, 4 })),
                () => new ByteArrayContent(new byte[] { 7, 8, 9 }));
            var downloader = new CloudinaryDownloader(
                new TestOptionsSnapshot<OssOption>(new OssOption()),
                new TestHttpClientFactory(new HttpClient(handler)));

            await downloader.DownloadAsync(
                new[] { ("https://example.test/cloudinary-tile.png", "cloudinary-tile.png") },
                _tempDirectory,
                TestContext.Current.CancellationToken);

            var path = Path.Combine(_tempDirectory, "cloudinary-tile.png");
            Assert.Equal(
                new byte[] { 7, 8, 9 },
                await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal(2, handler.RequestCount);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private sealed class TestHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public TestHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name) => _client;
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

        private sealed class SequenceHandler : HttpMessageHandler
        {
            private readonly Func<HttpContent>[] _contentFactories;

            public SequenceHandler(params Func<HttpContent>[] contentFactories)
            {
                _contentFactories = contentFactories;
            }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var index = Math.Min(RequestCount, _contentFactories.Length - 1);
                RequestCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = _contentFactories[index]()
                });
            }
        }

        private sealed class ThrowingReadStream : Stream
        {
            private readonly byte[] _buffer;
            private int _position;

            public ThrowingReadStream(byte[] buffer)
            {
                _buffer = buffer;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _buffer.Length;
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _buffer.Length)
                {
                    throw new IOException("The response ended prematurely.");
                }

                var bytesToCopy = Math.Min(count, _buffer.Length - _position);
                Array.Copy(_buffer, _position, buffer, offset, bytesToCopy);
                _position += bytesToCopy;
                return bytesToCopy;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_position >= _buffer.Length)
                {
                    throw new IOException("The response ended prematurely.");
                }

                var bytesToCopy = Math.Min(buffer.Length, _buffer.Length - _position);
                _buffer.AsMemory(_position, bytesToCopy).CopyTo(buffer);
                _position += bytesToCopy;
                return ValueTask.FromResult(bytesToCopy);
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
