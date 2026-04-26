using System;
using System.IO;
using Avalonia.Media.Imaging;
using EarthBackground.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace EarthBackground.Tests
{
    public class PngSequencePlayerTests : IDisposable
    {
        private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public void Open_ShouldSkipFramesWithDifferentDimensions()
        {
            Directory.CreateDirectory(_tempDirectory);
            var firstFrame = CreatePng("frame_1.png", 20, 20);
            var incompatibleFrame = CreatePng("frame_2.png", 22, 22);
            var thirdFrame = CreatePng("frame_3.png", 20, 20);

            using var player = PngSequencePlayer.Open(
                new[] { firstFrame, incompatibleFrame, thirdFrame },
                delayMilliseconds: 100);
            using var bitmap = new WriteableBitmap(player.PixelSize, new Avalonia.Vector(96, 96));

            var first = player.RenderNextFrame(bitmap);
            var second = player.RenderNextFrame(bitmap);

            Assert.Equal(2, player.FrameCount);
            Assert.Equal(0, first.FrameIndex);
            Assert.Equal(1, second.FrameIndex);
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
            using var image = new Image<Rgba32>(width, height);
            image.SaveAsPng(path);
            return path;
        }
    }
}
