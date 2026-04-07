using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace EarthBackground.Imaging
{
    public sealed class ApngAssembler
    {
        private readonly ILogger<ApngAssembler> _logger;

        public ApngAssembler(ILogger<ApngAssembler> logger)
        {
            _logger = logger;
        }

        public string CreateFromBitmaps(IReadOnlyList<string> framePaths, string outputPath, int delayMs)
        {
            if (framePaths.Count == 0)
            {
                throw new ArgumentException("At least one frame is required to create an APNG.", nameof(framePaths));
            }

            using var animation = Image.Load<Rgba32>(framePaths[0]);
            var animationMeta = animation.Metadata.GetPngMetadata();
            animationMeta.RepeatCount = 0;
            animationMeta.AnimateRootFrame = true;
            ConfigureFrame(animation.Frames.RootFrame, delayMs, isDeltaFrame: false);
            _logger.LogInformation("APNG frame {Index}: full frame {Width}x{Height}", 0, animation.Width, animation.Height);

            Image<Rgba32> previousFrame = animation.Clone();
            try
            {
                for (int i = 1; i < framePaths.Count; i++)
                {
                    using var originalFrame = Image.Load<Rgba32>(framePaths[i]);
                    using var encodedFrame = originalFrame.Clone();
                    var ratio = ApplyUnchangedPixelTransparency(previousFrame, encodedFrame);
                    _logger.LogInformation("APNG frame {Index}: unchanged-pixel filter ratio={Ratio:P2}", i, ratio);

                    ConfigureFrame(encodedFrame.Frames.RootFrame, delayMs, isDeltaFrame: true);
                    animation.Frames.AddFrame(encodedFrame.Frames.RootFrame);

                    previousFrame.Dispose();
                    previousFrame = originalFrame.Clone();
                }
            }
            finally
            {
                previousFrame.Dispose();
            }

            animation.Save(outputPath, new PngEncoder());
            return outputPath;
        }

        private static void ConfigureFrame(ImageFrame<Rgba32> frame, int delayMs, bool isDeltaFrame)
        {
            var frameMeta = frame.Metadata.GetPngMetadata();
            frameMeta.FrameDelay = new Rational((uint)delayMs, 1000);
            frameMeta.BlendMethod = isDeltaFrame ? PngBlendMethod.Over : PngBlendMethod.Source;
            frameMeta.DisposalMethod = PngDisposalMethod.DoNotDispose;
        }

        private static double ApplyUnchangedPixelTransparency(Image<Rgba32> previousFrame, Image<Rgba32> currentFrame)
        {
            if (previousFrame.Width != currentFrame.Width || previousFrame.Height != currentFrame.Height)
            {
                return 1d;
            }

            long unchangedPixelCount = 0;
            long totalPixelCount = (long)currentFrame.Width * currentFrame.Height;

            for (int y = 0; y < currentFrame.Height; y++)
            {
                for (int x = 0; x < currentFrame.Width; x++)
                {
                    if (previousFrame[x, y].Equals(currentFrame[x, y]))
                    {
                        currentFrame[x, y] = new Rgba32(0, 0, 0, 0);
                        unchangedPixelCount++;
                    }
                }
            }

            return totalPixelCount == 0 ? 0d : (double)unchangedPixelCount / totalPixelCount;
        }
    }
}
