using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using EarthBackground.Background;
using EarthBackground.Controls;
using SkiaSharp;
using Xunit;

namespace EarthBackground.Tests
{
    public class GifAndBmpConversionTests
    {
        // 4x2 GIF: keep red, temporarily overlay green, keep blue, clear green, keep blue.
        // Includes partial rectangles, transparent pixels, RestorePrevious and RestoreBackground.
        private const string DisposalGif =
            "R0lGODlhBAACAIEAAAAAAP8AAAD/AAAA/yH5BAUAAAAALAAAAAAEAAIAAAIHDEEQBEEQBQAh+QQNAQAAACwBAAAAAQABAAACAlQBACH5BAUFAAAALAIAAAABAAEAAAICXAEAIfkECQMAAAAsAQABAAEAAQAAAgJUAQAh+QQFBAAAACwDAAEAAQABAAACAlwBADs=";

        [AvaloniaFact]
        public void DecodeFrames_ShouldComposePartialFramesAndDisposals_AndClampDelays()
        {
            using var input = new MemoryStream(Convert.FromBase64String(DisposalGif));
            var frames = new List<Bitmap>();
            var delays = new List<TimeSpan>();
            try
            {
                AnimatedGifImage.DecodeFrames(input, 4, frames, delays);
                Assert.Equal(5, frames.Count);
                Assert.Equal(new[] { 20, 20, 50, 30, 40 }, delays.ConvertAll(delay => (int)delay.TotalMilliseconds));
                var clear = new SKColor(0, 0, 0, 0);
                var red = SKColors.Red;
                var green = new SKColor(0, 255, 0);
                var blue = SKColors.Blue;
                SKColor[][] expected =
                [
                    [red, clear, clear, clear, clear, clear, clear, clear],
                    [red, green, clear, clear, clear, clear, clear, clear],
                    [red, clear, blue, clear, clear, clear, clear, clear],
                    [red, clear, blue, clear, clear, green, clear, clear],
                    [red, clear, blue, clear, clear, clear, clear, blue]
                ];
                for (var i = 0; i < frames.Count; i++)
                {
                    using var actual = DecodeBitmap(frames[i]);
                    Assert.Equal(expected[i], actual.Pixels);
                }
            }
            finally
            {
                foreach (var frame in frames)
                    frame.Dispose();
            }
        }

        [AvaloniaFact]
        public void DecodeFrames_ShouldResizeWithTransparentEdges_WithoutDarkFringes()
        {
            using var input = new MemoryStream(Convert.FromBase64String(DisposalGif));
            var frames = new List<Bitmap>();
            var delays = new List<TimeSpan>();
            try
            {
                AnimatedGifImage.DecodeFrames(input, 8, frames, delays);
                Assert.All(frames, frame => Assert.Equal(new PixelSize(8, 4), frame.PixelSize));
                using var actual = DecodeBitmap(frames[0]);
                Assert.Equal(0, actual.GetPixel(7, 3).Alpha);
                Assert.Contains(actual.Pixels, pixel => pixel.Alpha is > 0 and < 255
                    && pixel.Red > 240 && pixel.Green == 0 && pixel.Blue == 0);
            }
            finally
            {
                foreach (var frame in frames)
                    frame.Dispose();
            }
        }

        [Fact]
        public void ConvertToBmp_ShouldWriteTopDownBgra_AndIgnoreAlphaWithoutDarkeningRgb()
        {
            using var source = new SKBitmap(new SKImageInfo(3, 2, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            source.Pixels =
            [
                new SKColor(200, 80, 40, 128), new SKColor(20, 220, 60, 64), SKColors.Blue,
                SKColors.White, SKColors.Black, SKColors.Yellow
            ];
            using var png = source.Encode(SKEncodedImageFormat.Png, 100);
            using var input = new MemoryStream();
            png.SaveTo(input);
            input.Position = 0;
            using var output = new MemoryStream();

            WindowsBackgroudSetter.ConvertToBmp(input, output);

            var bytes = output.ToArray();
            Assert.Equal((byte)'B', bytes[0]);
            Assert.Equal((byte)'M', bytes[1]);
            Assert.Equal(54 + 3 * 2 * 4, bytes.Length);
            Assert.Equal(bytes.Length, BitConverter.ToInt32(bytes, 2));
            Assert.Equal(54, BitConverter.ToInt32(bytes, 10));
            Assert.Equal(-2, BitConverter.ToInt32(bytes, 22));
            Assert.Equal(32, BitConverter.ToInt16(bytes, 28));
            Assert.Equal(0, BitConverter.ToInt32(bytes, 30));
            Assert.Equal(new byte[] { 40, 80, 200, 0 }, bytes[54..58]);
            using var decoded = SKBitmap.Decode(bytes);
            Assert.NotNull(decoded);
            Assert.Equal(3, decoded.Width);
            Assert.Equal(2, decoded.Height);
            Assert.Equal(new[]
            {
                new SKColor(200, 80, 40), new SKColor(20, 220, 60), SKColors.Blue,
                SKColors.White, SKColors.Black, SKColors.Yellow
            }, decoded.Pixels);
        }

        private static SKBitmap DecodeBitmap(Bitmap bitmap)
        {
            using var encoded = new MemoryStream();
            bitmap.Save(encoded, new PngBitmapEncoderOptions());
            encoded.Position = 0;
            return SKBitmap.Decode(encoded);
        }
    }
}
