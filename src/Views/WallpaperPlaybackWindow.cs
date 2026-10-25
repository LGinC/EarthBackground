using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using EarthBackground.Platforms.Linux;
using EarthBackground.Platforms.MacOS;
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
        private readonly bool _useMacOSDesktopLevel;
        private readonly bool _useX11DesktopWindow;
        private readonly List<Image> _frameImages = new();
        private readonly List<Border> _transitionLayers = new();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _frameClock = new();
        private WriteableBitmap _bitmap;
        private WriteableBitmap? _nextBitmap;
        private Task<FrameRenderResult>? _pendingFrame;
        private FrameRenderResult _currentFrame;
        private TimeSpan? _transitionStartedAt;
        private bool _closed;
        private DispatcherTimer? _resumeTimer;
        private IntPtr _nativeWindowHandle;
        private IntPtr _desktopHost;
        private TaskCompletionSource<bool>? _openedTcs;
        private bool _started;
        private bool _renderingPaused;

        public IntPtr NativeWindowHandle => _nativeWindowHandle;

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
            _useMacOSDesktopLevel = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            _useX11DesktopWindow = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            _bitmap = new WriteableBitmap(
                _framePlayer.PixelSize,
                new Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Unpremul);
            if (_framePlayer.FrameCount > 1)
            {
                _nextBitmap = new WriteableBitmap(
                    _framePlayer.PixelSize,
                    new Vector(96, 96),
                    PixelFormat.Rgba8888,
                    AlphaFormat.Unpremul);
            }

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
            Topmost = false;
            Background = Brushes.Black;
            Width = _windowW;
            Height = _windowH;
            Position = _useMacOSDesktopLevel || _useX11DesktopWindow ? new PixelPoint(_windowX, _windowY) : new PixelPoint(0, 0);

            var canvas = new Canvas
            {
                Width = _windowW,
                Height = _windowH,
                ClipToBounds = false
            };
            // 实时合成器先处理节点 Opacity，再应用其 RenderOptions；必须在父层启用整体合成。
            RenderOptions.SetRequiresFullOpacityHandling(canvas, true);

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
                if (_nextBitmap != null)
                {
                    // 先把下一帧合成到黑底，再淡入，透明像素也不会残留上一帧。
                    var transition = new Border
                    {
                        Width = region.Width,
                        Height = region.Height,
                        Background = Brushes.Black,
                        Opacity = 0,
                        Child = new Image { Source = _nextBitmap, Stretch = Stretch.Uniform }
                    };
                    Canvas.SetLeft(transition, region.X - _windowX);
                    Canvas.SetTop(transition, region.Y - _windowY);
                    _transitionLayers.Add(transition);
                    canvas.Children.Add(transition);
                }
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

        private async void OnOpened(object? sender, EventArgs e)
        {
            if (_started || _closed)
            {
                return;
            }

            try
            {
                var platformHandle = this.TryGetPlatformHandle();
                var hwnd = platformHandle?.Handle ?? IntPtr.Zero;
                if (hwnd == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Avalonia 窗口未能获取原生窗口句柄");
                }
                _nativeWindowHandle = hwnd;

                if (_useMacOSDesktopLevel)
                {
                    MacOSNativeWindow.ConfigureAsWallpaperWindow(hwnd);
                }
                else if (_useX11DesktopWindow)
                {
                    if (!IsX11WindowHandleDescriptor(platformHandle?.HandleDescriptor))
                    {
                        throw new PlatformNotSupportedException(
                            $"Linux dynamic wallpaper requires an X11/XID window handle, but Avalonia returned '{platformHandle?.HandleDescriptor ?? "<null>"}'.");
                    }

                    X11NativeWindow.ConfigureAsWallpaperWindow(hwnd, _windowX, _windowY, _windowW, _windowH);
                }
                else
                {
                    EmbedIntoWallpaper(hwnd, _workerW, _windowX, _windowY, _windowW, _windowH);
                }

                await StartPlaybackAsync();
                if (_closed) return;

                _logger.LogInformation("Avalonia 壁纸窗口已加载 {Count} 帧", _framePlayer.FrameCount);
                _logger.LogInformation(
                    "Avalonia 壁纸窗口已显示: Window=0x{Window:X}, WorkerW=0x{WorkerW:X}, Bounds={X},{Y},{W},{H}",
                    hwnd.ToInt64(),
                    _workerW.ToInt64(),
                    _windowX,
                    _windowY,
                    _windowW,
                    _windowH);
                _openedTcs?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _openedTcs?.TrySetException(ex);
                _logger.LogError(ex, "动态壁纸初始化失败");
                Close();
            }
        }

        private async Task StartPlaybackAsync()
        {
            if (_framePlayer.FrameCount == 0) return;

            _pendingFrame = Task.Run(() => _framePlayer.RenderNextFrame(_bitmap));
            _currentFrame = await _pendingFrame;
            if (_closed) return;

            _pendingFrame = null;
            foreach (var image in _frameImages)
            {
                image.Source = _bitmap;
            }

            _started = true;
            if (_nextBitmap != null) PrepareNextFrame();
        }

        private double TransitionMilliseconds => _currentFrame.IsLastFrame
            ? 0
            : Math.Min(200, _currentFrame.DelayMilliseconds * 0.4);

        private void PrepareNextFrame()
        {
            var target = _nextBitmap!;
            _pendingFrame = Task.Run(() => _framePlayer.RenderNextFrame(target));
            _transitionStartedAt = null;
            _frameClock.Restart();
            _timer.Interval = TimeSpan.FromMilliseconds(_currentFrame.IsLastFrame
                ? Math.Max(_loopPauseMilliseconds, 1)
                : _currentFrame.DelayMilliseconds - TransitionMilliseconds);
            if (_renderingPaused)
            {
                _frameClock.Stop();
            }
            else
            {
                _timer.Start();
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_renderingPaused || _closed || _pendingFrame == null) return;

            try
            {
                AdvancePlayback(_frameClock.Elapsed);
            }
            catch (Exception ex)
            {
                _timer.Stop();
                _frameClock.Stop();
                _logger.LogError(ex, "动态壁纸帧推进失败");
            }
        }

        private void AdvancePlayback(TimeSpan elapsed)
        {
            var transitionMilliseconds = TransitionMilliseconds;
            if (_transitionStartedAt == null)
            {
                var holdMilliseconds = _currentFrame.IsLastFrame
                    ? Math.Max(_loopPauseMilliseconds, 1)
                    : _currentFrame.DelayMilliseconds - transitionMilliseconds;
                if (elapsed.TotalMilliseconds < holdMilliseconds) return;

                _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 60);
                if (!_pendingFrame!.IsCompleted) return;

                // 只在后台解码完成后读取结果；慢帧继续保持当前画面，不阻塞 UI。
                _pendingFrame.GetAwaiter().GetResult();
                _transitionStartedAt = elapsed;
                foreach (var layer in _transitionLayers)
                {
                    layer.Child!.InvalidateVisual();
                }
            }

            var opacity = transitionMilliseconds == 0
                ? 1
                : Math.Clamp((elapsed - _transitionStartedAt.Value).TotalMilliseconds / transitionMilliseconds, 0, 1);
            foreach (var layer in _transitionLayers)
            {
                layer.Opacity = opacity;
            }
            if (opacity < 1) return;

            _currentFrame = _pendingFrame!.GetAwaiter().GetResult();
            (_bitmap, _nextBitmap) = (_nextBitmap!, _bitmap);
            for (int i = 0; i < _frameImages.Count; i++)
            {
                _frameImages[i].Source = _bitmap;
                ((Image)_transitionLayers[i].Child!).Source = _nextBitmap;
                _transitionLayers[i].Opacity = 0;
            }
            PrepareNextFrame();
        }

        private async void OnClosed(object? sender, EventArgs e)
        {
            try
            {
                await StopPlaybackAsync();
            }
            finally
            {
                if (_desktopHost != IntPtr.Zero)
                {
                    if (!DestroyWindow(_desktopHost))
                    {
                        _logger.LogWarning("释放桌面宿主失败: {Error}", Marshal.GetLastPInvokeError());
                    }
                    _desktopHost = IntPtr.Zero;
                }
            }
        }

        private async Task StopPlaybackAsync()
        {
            if (_closed) return;
            _closed = true;
            _started = false;
            _openedTcs?.TrySetCanceled();
            _timer.Stop();
            _frameClock.Stop();
            _resumeTimer?.Stop();
            _resumeTimer = null;
            try
            {
                if (_pendingFrame != null) await _pendingFrame;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "关闭时结束未完成的帧解码");
            }
            finally
            {
                // 解码任务仍可能持有目标位图；必须等它结束后才能释放。
                _bitmap.Dispose();
                _nextBitmap?.Dispose();
                _framePlayer.Dispose();
            }
        }

        public void PauseRenderingFor(TimeSpan pauseDuration)
        {
            if (pauseDuration <= TimeSpan.Zero || !_started)
            {
                return;
            }

            _renderingPaused = true;
            _timer.Stop();
            _frameClock.Stop();

            _resumeTimer ??= new DispatcherTimer();
            _resumeTimer.Stop();
            _resumeTimer.Interval = pauseDuration;
            _resumeTimer.Tick -= OnResumeRenderingTick;
            _resumeTimer.Tick += OnResumeRenderingTick;
            _resumeTimer.Start();
        }

        public void SuspendRendering()
        {
            _renderingPaused = true;
            _timer.Stop();
            _frameClock.Stop();
            _resumeTimer?.Stop();
        }

        public void ResumeRendering()
        {
            if (_closed) return;

            _resumeTimer?.Stop();
            _renderingPaused = false;
            if (_started && _pendingFrame != null)
            {
                _frameClock.Start();
                _timer.Start();
            }
        }

        public bool IsMacOSWindowVisible()
        {
            return !_useMacOSDesktopLevel || MacOSNativeWindow.IsVisible(_nativeWindowHandle);
        }

        private void OnResumeRenderingTick(object? sender, EventArgs e)
        {
            ResumeRendering();
        }

        private void EmbedIntoWallpaper(IntPtr hwnd, IntPtr workerW, int screenX, int screenY, int screenW, int screenH)
        {
            var parent = GetParent(workerW);
            var shellView = parent != IntPtr.Zero
                ? FindWindowEx(parent, IntPtr.Zero, "SHELLDLL_DefView", null)
                : IntPtr.Zero;
            var raisedDesktop = shellView != IntPtr.Zero &&
                (GetWindowLongPtr(parent, GWL_EXSTYLE).ToInt64() & WS_EX_NOREDIRECTIONBITMAP) != 0;
            if (!raisedDesktop) parent = workerW;

            // 24H2 的 WorkerW 绘制静态壁纸；播放器应是它的兄弟，而不是子窗口。
            var style = (GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64() | WS_CHILD) & ~WS_POPUP;
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(style));
            if (raisedDesktop)
            {
                // Avalonia 注册的窗口类带 CS_OWNDC，不能直接设置 WS_EX_LAYERED。
                // 用系统 STATIC 类承载分层合成，保留 Avalonia 自己的渲染窗口。
                var hostPoint = ScreenToParentPoint(parent, screenX, screenY);
                _desktopHost = CreateWindowEx((uint)WS_EX_LAYERED | WS_EX_NOACTIVATE, "STATIC", null,
                    (uint)WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                    hostPoint.X, hostPoint.Y, screenW, screenH, parent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (_desktopHost == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), "创建桌面分层宿主失败");
                }
                if (!SetLayeredWindowAttributes(_desktopHost, 0, 255, LWA_ALPHA) ||
                    !SetWindowPos(_desktopHost, shellView, hostPoint.X, hostPoint.Y, screenW, screenH,
                        SWP_NOACTIVATE | SWP_SHOWWINDOW))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), "显示桌面分层宿主失败");
                }
                parent = _desktopHost;
            }

            if (SetParent(hwnd, parent) == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "嵌入桌面父窗口失败");
            }
            var point = ScreenToParentPoint(parent, screenX, screenY);
            if (!SetWindowPos(hwnd, HWND_BOTTOM, point.X, point.Y, screenW, screenH,
                SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_FRAMECHANGED))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "设置桌面窗口位置失败");
            }
            _logger.LogInformation("桌面嵌入完成: Parent=0x{Parent:X}, RaisedDesktop={RaisedDesktop}, Client={X},{Y},{W},{H}",
                parent.ToInt64(), raisedDesktop, point.X, point.Y, screenW, screenH);
        }

        internal static PixelPoint ScreenToParentPoint(IntPtr parent, int screenX, int screenY)
        {
            var point = new POINT { X = screenX, Y = screenY };
            if (MapWindowPoints(IntPtr.Zero, parent, ref point, 1) == 0 && Marshal.GetLastPInvokeError() != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "转换桌面窗口坐标失败");
            }
            return new PixelPoint(point.X, point.Y);
        }

        internal static bool IsX11WindowHandleDescriptor(string? handleDescriptor)
            => string.Equals(handleDescriptor, "X11", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(handleDescriptor, "XID", StringComparison.OrdinalIgnoreCase);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_LAYERED = 0x00080000L;
        private const long WS_EX_NOREDIRECTIONBITMAP = 0x00200000L;
        private const uint LWA_ALPHA = 0x00000002;
        private const uint WS_EX_NOACTIVATE = 0x08000000;
        private const uint WS_CLIPCHILDREN = 0x02000000;
        private const uint WS_CLIPSIBLINGS = 0x04000000;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_POPUP = 0x80000000L;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string? windowName,
            uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string? windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte alpha, uint flags);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MapWindowPoints(IntPtr from, IntPtr to, ref POINT point, uint count);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

        private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
        {
            var previous = IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, value)
                : SetWindowLongPtr32(hWnd, nIndex, value);
            if (previous == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "设置桌面窗口样式失败");
            }
        }
    }
}
