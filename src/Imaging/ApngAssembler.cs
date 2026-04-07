using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

        public string CreateFromBitmaps(IReadOnlyList<string> framePaths, string outputPath, int delayMs, CancellationToken token = default)
        {
            if (framePaths.Count == 0)
            {
                throw new ArgumentException("At least one frame is required to create an APNG.", nameof(framePaths));
            }

            var prepareStopwatch = Stopwatch.StartNew();
            token.ThrowIfCancellationRequested();
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
                    token.ThrowIfCancellationRequested();
                    using var originalFrame = Image.Load<Rgba32>(framePaths[i]);
                    using var encodedFrame = originalFrame.Clone();
                    var ratio = ApplyUnchangedPixelTransparency(previousFrame, encodedFrame, token);
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

            token.ThrowIfCancellationRequested();
            prepareStopwatch.Stop();

            var saveStopwatch = Stopwatch.StartNew();
            animation.Save(outputPath, CreateFastEncoder());
            saveStopwatch.Stop();

            _logger.LogInformation(
                "APNG assembled: frames={Count}, prepare={PrepareMs}ms, encode={EncodeMs}ms, output={OutputPath}",
                framePaths.Count,
                prepareStopwatch.ElapsedMilliseconds,
                saveStopwatch.ElapsedMilliseconds,
                outputPath);

            return outputPath;
        }

        private static PngEncoder CreateFastEncoder()
        {
            return new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestSpeed,
                FilterMethod = PngFilterMethod.None,
                InterlaceMethod = PngInterlaceMode.None
            };
        }

        private static void ConfigureFrame(ImageFrame<Rgba32> frame, int delayMs, bool isDeltaFrame)
        {
            var frameMeta = frame.Metadata.GetPngMetadata();
            frameMeta.FrameDelay = new Rational((uint)delayMs, 1000);
            frameMeta.BlendMethod = isDeltaFrame ? PngBlendMethod.Over : PngBlendMethod.Source;
            frameMeta.DisposalMethod = PngDisposalMethod.DoNotDispose;
        }

        private static double ApplyUnchangedPixelTransparency(Image<Rgba32> previousFrame, Image<Rgba32> currentFrame, CancellationToken token)
        {
            if (previousFrame.Width != currentFrame.Width || previousFrame.Height != currentFrame.Height)
            {
                return 1d;
            }

            long unchangedPixelCount = 0;
            long totalPixelCount = (long)currentFrame.Width * currentFrame.Height;

            previousFrame.ProcessPixelRows(currentFrame, (previousAccessor, currentAccessor) =>
            {
                for (int y = 0; y < currentAccessor.Height; y++)
                {
                    token.ThrowIfCancellationRequested();
                    Span<Rgba32> previousRow = previousAccessor.GetRowSpan(y);
                    Span<Rgba32> currentRow = currentAccessor.GetRowSpan(y);

                    for (int x = 0; x < currentRow.Length; x++)
                    {
                        if (!previousRow[x].Equals(currentRow[x]))
                        {
                            continue;
                        }

                        currentRow[x] = default;
                        unchangedPixelCount++;
                    }
                }
            });

            return totalPixelCount == 0 ? 0d : (double)unchangedPixelCount / totalPixelCount;
        }
    }
}
