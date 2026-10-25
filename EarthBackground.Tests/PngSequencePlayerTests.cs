using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using EarthBackground.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Xunit;

namespace EarthBackground.Tests
{
    public class PngSequencePlayerTests : IDisposable
    {
        private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));

        [AvaloniaFact]
        public void Open_ShouldSkipFramesWithDifferentDimensions()
        {
            Directory.CreateDirectory(_tempDirectory);
            var firstFrame = CreatePng("frame_1.png", 20, 20);
            var incompatibleFrame = CreatePng("frame_2.png", 22, 22);
            var thirdFrame = CreatePng("frame_3.png", 20, 20);

            using var player = PngSequencePlayer.Open(
                new[] { firstFrame, incompatibleFrame, thirdFrame },
                delayMilliseconds: 100);
            using var bitmap = new WriteableBitmap(player.PixelSize, new Avalonia.Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);

            var first = player.RenderNextFrame(bitmap);
            var second = player.RenderNextFrame(bitmap);

            Assert.Equal(2, player.FrameCount);
            Assert.Equal(0, first.FrameIndex);
            Assert.Equal(1, second.FrameIndex);
        }

        [AvaloniaFact]
        public void RenderNextFrame_ShouldPreserveRgbaAndAlpha_AndReplacePreviousPixels()
        {
            Directory.CreateDirectory(_tempDirectory);
            var firstFrame = Path.Combine(_tempDirectory, "colors.png");
            using (var image = new SKBitmap(new SKImageInfo(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul)))
            using (var output = File.Create(firstFrame))
            {
                image.SetPixel(0, 0, new SKColor(240, 80, 20, 128));
                image.SetPixel(1, 0, SKColors.Blue);
                Assert.True(image.Encode(output, SKEncodedImageFormat.Png, 100));
            }
            var secondFrame = CreatePng("clear.png", 2, 1);
            using var player = PngSequencePlayer.Open(new[] { firstFrame, secondFrame }, 75);
            using var bitmap = new WriteableBitmap(player.PixelSize, new Avalonia.Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);

            var first = player.RenderNextFrame(bitmap);
            using (var pixels = bitmap.Lock())
            {
                var actual = new byte[8];
                Marshal.Copy(pixels.Address, actual, 0, actual.Length);
                Assert.Equal(new byte[] { 240, 80, 20, 128, 0, 0, 255, 255 }, actual);
            }
            Assert.Equal(new FrameRenderResult(75, false, 0), first);

            var second = player.RenderNextFrame(bitmap);
            using (var pixels = bitmap.Lock())
            {
                Assert.Equal(0, Marshal.ReadInt64(pixels.Address));
            }
            Assert.Equal(new FrameRenderResult(75, true, 1), second);
            Assert.Equal(first, player.RenderNextFrame(bitmap));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private string CreatePng(string fileName, int width, int height)
        {
            var path = Path.Combine(_tempDirectory, fileName);
            using var image = new SKBitmap(width, height);
            image.Erase(SKColors.Transparent);
            using var output = File.Create(path);
            Assert.True(image.Encode(output, SKEncodedImageFormat.Png, 100));
            return path;
        }
    }
}
