using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;

namespace EarthBackground.Controls
{
    public class AnimatedGifImage : Avalonia.Controls.Image
    {
        public static readonly StyledProperty<string?> AssetPathProperty =
            AvaloniaProperty.Register<AnimatedGifImage, string?>(nameof(AssetPath));

        public static readonly StyledProperty<int> DecodeWidthProperty =
            AvaloniaProperty.Register<AnimatedGifImage, int>(nameof(DecodeWidth), 64);

        public string? AssetPath
        {
            get => GetValue(AssetPathProperty);
            set => SetValue(AssetPathProperty, value);
        }

        public int DecodeWidth
        {
            get => GetValue(DecodeWidthProperty);
            set => SetValue(DecodeWidthProperty, value);
        }

        private readonly DispatcherTimer _timer = new();
        private readonly List<Bitmap> _frames = new();
        private readonly List<TimeSpan> _delays = new();
        private int _frameIndex;
        private bool _isAttached;

        static AnimatedGifImage()
        {
            AssetPathProperty.Changed.AddClassHandler<AnimatedGifImage>((x, _) => x.Reload());
            DecodeWidthProperty.Changed.AddClassHandler<AnimatedGifImage>((x, _) => x.Reload());
        }

        public AnimatedGifImage()
        {
            _timer.Tick += OnTimerTick;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _isAttached = true;
            Reload();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _isAttached = false;
            _timer.Stop();
            ClearFrames();
            base.OnDetachedFromVisualTree(e);
        }

        private void Reload()
        {
            if (!_isAttached)
                return;

            _timer.Stop();
            ClearFrames();

            if (string.IsNullOrWhiteSpace(AssetPath))
                return;

            using var asset = AssetLoader.Open(new Uri(AssetPath, UriKind.RelativeOrAbsolute));
            try
            {
                DecodeFrames(asset, DecodeWidth, _frames, _delays);
            }
            catch
            {
                ClearFrames();
                throw;
            }

            _frameIndex = 0;
            Source = _frames.Count > 0 ? _frames[0] : null;

            if (_frames.Count > 1)
            {
                _timer.Interval = _delays[0];
                _timer.Start();
            }
        }

        internal static void DecodeFrames(Stream asset, int decodeWidth, List<Bitmap> frames, List<TimeSpan> delays)
        {
            using var codec = SKCodec.Create(asset) ?? throw new InvalidDataException("Cannot decode animation.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var width = Math.Max(1, decodeWidth);
            var height = Math.Max(1, checked((int)Math.Round(info.Height * (width / (double)info.Width))));
            using var prior = new SKBitmap(info);
            using var resized = width == info.Width && height == info.Height
                ? null
                : new SKBitmap(new SKImageInfo(width, height, info.ColorType, info.AlphaType));
            var frameInfo = codec.FrameInfo;
            SKBitmap? temporary = null;
            var priorIndex = -1;
            try
            {
                for (var i = 0; i < Math.Max(1, codec.FrameCount); i++)
                {
                    var metadata = frameInfo.Length > i ? frameInfo[i] : default;
                    var requiredIndex = frameInfo.Length > i ? metadata.RequiredFrame : -1;
                    var restorePrevious = metadata.DisposalMethod == SKCodecAnimationDisposalMethod.RestorePrevious;
                    var frame = restorePrevious ? temporary ??= new SKBitmap(info) : prior;
                    var decodePrior = requiredIndex < 0 ? -1 : priorIndex;
                    // RestorePrevious frames must not overwrite the canvas needed by later frames.
                    if (restorePrevious && decodePrior >= 0)
                        prior.GetPixelSpan().CopyTo(frame.GetPixelSpan());

                    var result = codec.GetPixels(info, frame.GetPixels(), frame.RowBytes, new SKCodecOptions(i, decodePrior));
                    if (result != SKCodecResult.Success)
                        throw new InvalidDataException($"Cannot decode animation frame {i}: {result}.");
                    if (!restorePrevious)
                        priorIndex = i;

                    if (resized is not null)
                    {
                        if (!frame.ScalePixels(resized, new SKSamplingOptions(SKCubicResampler.Mitchell)))
                            throw new InvalidDataException("Cannot resize animation frame.");
                        frame = resized;
                    }

                    frames.Add(new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Premul, frame.GetPixels(),
                        new PixelSize(width, height), new Vector(96, 96), frame.RowBytes));
                    delays.Add(TimeSpan.FromMilliseconds(Math.Max(20, metadata.Duration)));
                }
            }
            finally
            {
                temporary?.Dispose();
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0)
                return;

            _frameIndex = (_frameIndex + 1) % _frames.Count;
            Source = _frames[_frameIndex];
            _timer.Interval = _delays[_frameIndex];
        }

        private void ClearFrames()
        {
            Source = null;
            foreach (var frame in _frames)
                frame.Dispose();

            _frames.Clear();
            _delays.Clear();
            _frameIndex = 0;
        }
    }
}
