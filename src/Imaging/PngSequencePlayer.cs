using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace EarthBackground.Imaging
{
    internal sealed class PngSequencePlayer : IWallpaperFramePlayer
    {
        private readonly string[] _filePaths;
        private readonly SharedPixelBufferHandle? _sharedBufferHandle;
        private readonly int _delayMilliseconds;
        private readonly int _width;
        private readonly int _height;
        private int _currentFrameIndex = -1;
        private bool _disposed;

        private PngSequencePlayer(string[] filePaths, int width, int height, int delayMilliseconds)
        {
            _filePaths = filePaths;
            _width = width;
            _height = height;
            _delayMilliseconds = delayMilliseconds;
        }

        private PngSequencePlayer(string[] filePaths, int width, int height, int delayMilliseconds, SharedPixelBuffer sharedBuffer)
        {
            _filePaths = filePaths;
            _width = width;
            _height = height;
            _delayMilliseconds = delayMilliseconds;
            _sharedBufferHandle = sharedBuffer.Acquire();
        }

        public int FrameCount => _filePaths.Length;

        public PixelSize PixelSize => new(_width, _height);

        public static PngSequencePlayer Open(IReadOnlyList<string> filePaths, int delayMilliseconds)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                throw new ArgumentException("At least one frame is required.", nameof(filePaths));
            }

            using var firstFrame = Image.Load<Rgba32>(filePaths[0]);
            var compatibleFilePaths = FilterCompatibleFrames(filePaths, firstFrame.Width, firstFrame.Height);
            if (compatibleFilePaths.Length == 0)
            {
                throw new InvalidOperationException("No compatible PNG frames were found.");
            }

            return new PngSequencePlayer(
                compatibleFilePaths,
                firstFrame.Width,
                firstFrame.Height,
                Math.Max(delayMilliseconds, 1));
        }

        public static PngSequencePlayer Open(IReadOnlyList<string> filePaths, int delayMilliseconds, SharedPixelBuffer sharedBuffer)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                throw new ArgumentException("At least one frame is required.", nameof(filePaths));
            }

            using var firstFrame = Image.Load<Rgba32>(filePaths[0]);
            var compatibleFilePaths = FilterCompatibleFrames(filePaths, firstFrame.Width, firstFrame.Height);
            if (compatibleFilePaths.Length == 0)
            {
                throw new InvalidOperationException("No compatible PNG frames were found.");
            }

            if (sharedBuffer.Width != firstFrame.Width || sharedBuffer.Height != firstFrame.Height)
            {
                throw new ArgumentException(
                    $"Shared pixel buffer size mismatch: expected {firstFrame.Width}x{firstFrame.Height}, got {sharedBuffer.Width}x{sharedBuffer.Height}.");
            }

            return new PngSequencePlayer(
                compatibleFilePaths,
                firstFrame.Width,
                firstFrame.Height,
                Math.Max(delayMilliseconds, 1),
                sharedBuffer);
        }

        public FrameRenderResult RenderNextFrame(WriteableBitmap bitmap)
        {
            if (_filePaths.Length == 0 || _disposed)
            {
                return new FrameRenderResult(100, true, 0);
            }

            _currentFrameIndex = (_currentFrameIndex + 1) % _filePaths.Length;

            using var image = Image.Load<Rgba32>(_filePaths[_currentFrameIndex]);

            if (_sharedBufferHandle?.Buffer != null)
            {
                // Copy to shared buffer first (for multi-monitor sync), then to bitmap
                var sharedBufferArray = _sharedBufferHandle.Buffer.GetBuffer();
                CopyImageToBuffer(image, sharedBufferArray);
                CopyBufferToBitmap(sharedBufferArray, bitmap);
            }
            else
            {
                // Copy directly from decoded image to WriteableBitmap — no intermediate buffer
                CopyImageToBitmap(image, bitmap);
            }

            return new FrameRenderResult(_delayMilliseconds, _currentFrameIndex == _filePaths.Length - 1, _currentFrameIndex);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _sharedBufferHandle?.Dispose();
        }

        /// <summary>
        /// Copy pixel data directly from Image to WriteableBitmap, row by row.
        /// Uses a single row-sized temp buffer to avoid allocating a full-frame buffer.
        /// </summary>
        private void CopyImageToBitmap(Image<Rgba32> image, WriteableBitmap bitmap)
        {
            var rowByteCount = _width * 4; // Rgba32 = 4 bytes per pixel
            var rowBuffer = ArrayPool<byte>.Shared.Rent(rowByteCount);
            try
            {
                using var framebuffer = bitmap.Lock();
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        MemoryMarshal.AsBytes(row).CopyTo(rowBuffer);
                        Marshal.Copy(rowBuffer, 0, framebuffer.Address + (y * framebuffer.RowBytes), rowByteCount);
                    }
                });
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rowBuffer);
            }
        }

        /// <summary>
        /// Copy pixel data from Image to a byte buffer (for shared buffer scenario).
        /// </summary>
        private void CopyImageToBuffer(Image<Rgba32> image, byte[] buffer)
        {
            var rowBytes = _width * 4;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    MemoryMarshal.AsBytes(row).CopyTo(buffer.AsSpan(y * rowBytes, rowBytes));
                }
            });
        }

        /// <summary>
        /// Copy pixel data from a byte buffer to WriteableBitmap, row by row.
        /// </summary>
        private void CopyBufferToBitmap(byte[] source, WriteableBitmap bitmap)
        {
            using var framebuffer = bitmap.Lock();
            var sourceRowBytes = _width * 4;
            for (int y = 0; y < _height; y++)
            {
                Marshal.Copy(
                    source,
                    y * sourceRowBytes,
                    framebuffer.Address + (y * framebuffer.RowBytes),
                    sourceRowBytes);
            }
        }

        private static string[] FilterCompatibleFrames(IReadOnlyList<string> filePaths, int width, int height)
        {
            List<string>? result = null;
            for (int i = 0; i < filePaths.Count; i++)
            {
                var path = filePaths[i];
                var frame = Image.Identify(path);
                if (frame == null || frame.Width != width || frame.Height != height)
                {
                    result ??= CopyBefore(filePaths, i);
                    continue;
                }

                result?.Add(path);
            }

            return result?.ToArray() ?? ToArray(filePaths);
        }

        private static List<string> CopyBefore(IReadOnlyList<string> filePaths, int exclusiveEnd)
        {
            var result = new List<string>(filePaths.Count);
            for (int i = 0; i < exclusiveEnd; i++)
            {
                result.Add(filePaths[i]);
            }

            return result;
        }

        private static string[] ToArray(IReadOnlyList<string> filePaths)
        {
            var result = new string[filePaths.Count];
            for (int i = 0; i < filePaths.Count; i++)
            {
                result[i] = filePaths[i];
            }

            return result;
        }
    }
}