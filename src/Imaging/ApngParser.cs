using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EarthBackground.Imaging
{
    internal static class ApngParser
    {
        public static IReadOnlyList<ApngFrame> Parse(string filePath)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
            var frames = new List<ApngFrame>(image.Frames.Count);

            for (int i = 0; i < image.Frames.Count; i++)
            {
                var frameMeta = image.Frames[i].Metadata.GetPngMetadata();
                var delay = frameMeta.FrameDelay.Denominator == 0
                    ? 100
                    : (int)System.Math.Round(frameMeta.FrameDelay.ToDouble() * 1000d);
                if (delay <= 0)
                {
                    delay = 100;
                }

                using var frameImage = image.Frames.CloneFrame(i);
                frames.Add(new ApngFrame(ToBitmap(frameImage), delay));
            }

            return frames;
        }

        private static Bitmap ToBitmap(SixLabors.ImageSharp.Image<Rgba32> image)
        {
            var bitmap = new WriteableBitmap(
                new PixelSize(image.Width, image.Height),
                new Avalonia.Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Unpremul);

            using var framebuffer = bitmap.Lock();
            var pixels = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);
            var bytes = MemoryMarshal.Cast<Rgba32, byte>(new Span<Rgba32>(pixels)).ToArray();

            for (int y = 0; y < image.Height; y++)
            {
                var sourceOffset = y * image.Width * 4;
                var target = framebuffer.Address + (y * framebuffer.RowBytes);
                Marshal.Copy(bytes, sourceOffset, target, image.Width * 4);
            }

            return bitmap;
        }
    }

    public sealed class ApngFrame : System.IDisposable
    {
        public ApngFrame(Bitmap bitmap, int delayMilliseconds)
        {
            Bitmap = bitmap;
            DelayMilliseconds = delayMilliseconds;
        }

        public Bitmap Bitmap { get; }
        public int DelayMilliseconds { get; }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
