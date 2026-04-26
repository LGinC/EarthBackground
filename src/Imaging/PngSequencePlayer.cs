using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EarthBackground.Imaging
{
    internal sealed class PngSequencePlayer : IWallpaperFramePlayer
    {
        private readonly string[] _filePaths;
        private readonly byte[] _pixelBuffer;
        private readonly int _delayMilliseconds;
        private readonly int _width;
        private readonly int _height;
        private int _currentFrameIndex = -1;

        private PngSequencePlayer(string[] filePaths, int width, int height, int delayMilliseconds)
        {
            _filePaths = filePaths;
            _width = width;
            _height = height;
            _delayMilliseconds = delayMilliseconds;
            _pixelBuffer = new byte[checked(width * height * 4)];
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

        public FrameRenderResult RenderNextFrame(WriteableBitmap bitmap)
        {
            if (_filePaths.Length == 0)
            {
                return new FrameRenderResult(100, true, 0);
            }

            _currentFrameIndex = (_currentFrameIndex + 1) % _filePaths.Length;
            using var frameImage = Image.Load<Rgba32>(_filePaths[_currentFrameIndex]);
            if (frameImage.Width != _width || frameImage.Height != _height)
            {
                throw new InvalidOperationException(
                    $"PNG frame size mismatch: expected {_width}x{_height}, got {frameImage.Width}x{frameImage.Height}. Path: {_filePaths[_currentFrameIndex]}");
            }

            frameImage.CopyPixelDataTo(_pixelBuffer);
            CopyPixelsToBitmap(bitmap);

            return new FrameRenderResult(_delayMilliseconds, _currentFrameIndex == _filePaths.Length - 1, _currentFrameIndex);
        }

        public void Dispose()
        {
        }

        private void CopyPixelsToBitmap(WriteableBitmap bitmap)
        {
            using var framebuffer = bitmap.Lock();
            var sourceRowBytes = _width * 4;
            for (int y = 0; y < _height; y++)
            {
                Marshal.Copy(
                    _pixelBuffer,
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
