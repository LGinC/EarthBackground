using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(asset);
            var width = Math.Max(1, DecodeWidth);
            var height = Math.Max(1, (int)Math.Round(image.Height * (width / (double)image.Width)));

            for (var i = 0; i < image.Frames.Count; i++)
            {
                using var frame = image.Frames.CloneFrame(i);
                frame.Mutate(x => x.Resize(width, height));

                using var output = new MemoryStream();
                frame.Save(output, PngFormat.Instance);
                output.Position = 0;
                _frames.Add(new Bitmap(output));

                var delay = image.Frames[i].Metadata.GetGifMetadata().FrameDelay;
                _delays.Add(TimeSpan.FromMilliseconds(Math.Max(20, delay * 10)));
            }

            _frameIndex = 0;
            Source = _frames.Count > 0 ? _frames[0] : null;

            if (_frames.Count > 1)
            {
                _timer.Interval = _delays[0];
                _timer.Start();
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
