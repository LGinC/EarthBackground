using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EarthBackground.Imaging;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Views
{
    internal sealed class WallpaperPlaybackWindow : Window
    {
        public readonly record struct DisplayRegion(int X, int Y, int Width, int Height);

        private readonly ILogger<WallpaperPlaybackWindow> _logger;
        private readonly IWallpaperFramePlayer _framePlayer;
        private readonly IntPtr _workerW;
        private readonly int _windowX;
        private readonly int _windowY;
        private readonly int _windowW;
        private readonly int _windowH;
        private readonly int _loopPauseMilliseconds;
        private readonly List<Image> _frameImages = new();
        private readonly DispatcherTimer _timer;
        private readonly WriteableBitmap _bitmap;
        private TaskCompletionSource<bool>? _openedTcs;
        private bool _started;

        internal WallpaperPlaybackWindow(
            ILogger<WallpaperPlaybackWindow> logger,
            IWallpaperFramePlayer framePlayer,
            IntPtr workerW,
            IReadOnlyList<DisplayRegion> displayRegions,
            int loopPauseMilliseconds)
        {
            _logger = logger;
            _framePlayer = framePlayer;
            _workerW = workerW;
            _loopPauseMilliseconds = Math.Max(loopPauseMilliseconds, 0);
            _bitmap = new WriteableBitmap(
                _framePlayer.PixelSize,
                new Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Unpremul);

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxRight = int.MinValue;
            var maxBottom = int.MinValue;
            foreach (var region in displayRegions)
            {
                minX = Math.Min(minX, region.X);
                minY = Math.Min(minY, region.Y);
                maxRight = Math.Max(maxRight, region.X + region.Width);
                maxBottom = Math.Max(maxBottom, region.Y + region.Height);
            }

            _windowX = minX;
            _windowY = minY;
            _windowW = maxRight - minX;
            _windowH = maxBottom - minY;

            WindowDecorations = WindowDecorations.None;
            ShowInTaskbar = false;
            CanResize = false;
            ShowActivated = false;
            Background = Brushes.Black;
            Width = _windowW;
            Height = _windowH;
            Position = new PixelPoint(0, 0);

            var canvas = new Canvas
            {
                Width = _windowW,
                Height = _windowH,
                ClipToBounds = false
            };

            foreach (var region in displayRegions)
            {
                var image = new Image
                {
                    Width = region.Width,
                    Height = region.Height,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Canvas.SetLeft(image, region.X - _windowX);
                Canvas.SetTop(image, region.Y - _windowY);
                _frameImages.Add(image);
                canvas.Children.Add(image);
            }

            Content = canvas;

            _timer = new DispatcherTimer();
            _timer.Tick += OnTick;
            Opened += OnOpened;
            Closed += OnClosed;
        }

        public Task ShowEmbeddedAsync()
        {
            _openedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Show();
            return _openedTcs.Task;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (_started)
            {
                return;
            }

            var platformHandle = this.TryGetPlatformHandle();
            var hwnd = platformHandle?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero)
            {
                _openedTcs?.TrySetException(new InvalidOperationException("Avalonia 窗口未能获取 HWND"));
                return;
            }

            EmbedIntoWallpaper(hwnd, _workerW, _windowX, _windowY, _windowW, _windowH);

            if (_framePlayer.FrameCount > 0)
            {
                var firstFrame = _framePlayer.RenderNextFrame(_bitmap);
                foreach (var image in _frameImages)
                {
                    image.Source = _bitmap;
                }
                _timer.Interval = TimeSpan.FromMilliseconds(firstFrame.DelayMilliseconds);
                _timer.Start();
            }

            _started = true;
            _logger.LogInformation("Avalonia 壁纸窗口已加载 {Count} 帧", _framePlayer.FrameCount);
            _logger.LogInformation(
                "Avalonia 壁纸窗口已嵌入 WorkerW: Window=0x{Window:X}, WorkerW=0x{WorkerW:X}, Bounds={X},{Y},{W},{H}",
                hwnd.ToInt64(),
                _workerW.ToInt64(),
                _windowX,
                _windowY,
                _windowW,
                _windowH);
            _openedTcs?.TrySetResult(true);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_framePlayer.FrameCount == 0)
            {
                return;
            }

            try
            {
                var renderedFrame = _framePlayer.RenderNextFrame(_bitmap);
                foreach (var image in _frameImages)
                {
                    image.InvalidateVisual();
                }

                InvalidateVisual();

                var nextDelay = renderedFrame.IsLastFrame ? _loopPauseMilliseconds : renderedFrame.DelayMilliseconds;
                _timer.Interval = TimeSpan.FromMilliseconds(nextDelay);
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _logger.LogError(ex, "动态壁纸帧推进失败");
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _timer.Stop();
            _bitmap.Dispose();
            _framePlayer.Dispose();
            _started = false;
        }

        private void EmbedIntoWallpaper(IntPtr hwnd, IntPtr workerW, int screenX, int screenY, int screenW, int screenH)
        {
            var parentPoint = ConvertVirtualScreenToWallpaperParentPoint(screenX, screenY);
            var style = GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();
            style |= WS_CHILD;
            style &= ~WS_POPUP;
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));
            SetParent(hwnd, workerW);
            SetWindowPos(hwnd, HWND_BOTTOM, parentPoint.X, parentPoint.Y, screenW, screenH, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        internal static PixelPoint ConvertVirtualScreenToWallpaperParentPoint(int screenX, int screenY)
        {
            var virtualScreenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
            var virtualScreenY = GetSystemMetrics(SM_YVIRTUALSCREEN);
            return new PixelPoint(screenX - virtualScreenX, screenY - virtualScreenY);
        }

        private const int GWL_STYLE = -16;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_POPUP = 0x80000000L;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, value) : SetWindowLongPtr32(hWnd, nIndex, value);
    }
}
