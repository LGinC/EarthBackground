using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace EarthBackground.Imaging
{
    internal sealed class PngSequencePlayer : IWallpaperFramePlayer
    {
        private readonly string[] _filePaths;
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

        public int FrameCount => _filePaths.Length;

        public PixelSize PixelSize => new(_width, _height);

        public static PngSequencePlayer Open(IReadOnlyList<string> filePaths, int delayMilliseconds)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                throw new ArgumentException("At least one frame is required.", nameof(filePaths));
            }

            using var firstFrame = SKCodec.Create(filePaths[0])
                ?? throw new InvalidDataException($"无法识别图像：{filePaths[0]}");
            var size = firstFrame.Info;
            var compatibleFilePaths = FilterCompatibleFrames(filePaths, size.Width, size.Height);
            if (compatibleFilePaths.Length == 0)
            {
                throw new InvalidOperationException("No compatible PNG frames were found.");
            }

            return new PngSequencePlayer(
                compatibleFilePaths,
                size.Width,
                size.Height,
                Math.Max(delayMilliseconds, 1));
        }

        public FrameRenderResult RenderNextFrame(WriteableBitmap bitmap)
        {
            if (_filePaths.Length == 0 || _disposed)
            {
                return new FrameRenderResult(100, true, 0);
            }

            _currentFrameIndex = (_currentFrameIndex + 1) % _filePaths.Length;

            using var codec = SKCodec.Create(_filePaths[_currentFrameIndex])
                ?? throw new InvalidDataException($"无法识别图像：{_filePaths[_currentFrameIndex]}");
            if (codec.Info.Width != _width || codec.Info.Height != _height)
            {
                throw new InvalidDataException("播放期间 PNG 帧尺寸发生变化。");
            }

            using var framebuffer = bitmap.Lock();
            var info = new SKImageInfo(_width, _height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var result = codec.GetPixels(info, framebuffer.Address, framebuffer.RowBytes, SKCodecOptions.Default);
            if (result != SKCodecResult.Success)
            {
                throw new InvalidDataException($"PNG 帧解码失败：{result}");
            }

            return new FrameRenderResult(_delayMilliseconds, _currentFrameIndex == _filePaths.Length - 1, _currentFrameIndex);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        private static string[] FilterCompatibleFrames(IReadOnlyList<string> filePaths, int width, int height)
        {
            List<string>? result = null;
            for (int i = 1; i < filePaths.Count; i++)
            {
                var path = filePaths[i];
                using var frame = SKCodec.Create(path)
                    ?? throw new InvalidDataException($"无法识别图像：{path}");
                if (frame.Info.Width != width || frame.Info.Height != height)
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