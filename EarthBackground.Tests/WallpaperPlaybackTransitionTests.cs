using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EarthBackground.Imaging;
using EarthBackground.Views;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace EarthBackground.Tests
{
    public class WallpaperPlaybackTransitionTests
    {
        [AvaloniaTheory]
        [InlineData(500, 200)]
        [InlineData(100, 40)]
        public async Task Playback_ShouldFadeWithinFrameInterval_AndKeepLoopPause(int interval, int transition)
        {
            await using var playback = await Playback.CreateAsync(interval);
            await playback.PendingFrame;
            var hold = interval - transition;

            playback.Advance(hold - 1);
            Assert.Equal(new SKColor(255, 0, 0), playback.CurrentPixel);
            Assert.Equal(0, playback.Overlay.Opacity);

            playback.Advance(hold);
            playback.Advance(hold + transition / 2.0);
            Assert.Equal(0.5, playback.Overlay.Opacity, 6);
            Assert.Equal(new SKColor(255, 0, 0), playback.CurrentPixel);
            Assert.Equal(new SKColor(0, 0, 255), playback.NextPixel);
            var blended = playback.CapturePixels();
            Assert.InRange(blended.Left.Red, (byte)126, (byte)129);
            Assert.InRange(blended.Left.Blue, (byte)126, (byte)129);
            Assert.InRange(blended.Right.Red, (byte)126, (byte)129);
            Assert.Equal(0, blended.Right.Blue);

            playback.Advance(interval);
            Assert.Equal(new SKColor(0, 0, 255), playback.CurrentPixel);
            Assert.Equal(0, playback.Overlay.Opacity);
            var finished = playback.CapturePixels();
            Assert.Equal(new SKColor(0, 0, 255), finished.Left);
            Assert.Equal(new SKColor(0, 0, 0), finished.Right);
            await playback.PendingFrame;

            playback.Advance(999);
            Assert.Equal(new SKColor(0, 0, 255), playback.CurrentPixel);
            Assert.Equal(0, playback.Overlay.Opacity);
            playback.Advance(1000);
            Assert.Equal(new SKColor(255, 0, 0), playback.CurrentPixel);
            Assert.Equal(0, playback.Overlay.Opacity);
            Assert.False(playback.Player.DecodedOnUiThread);
        }

        [AvaloniaFact]
        public async Task Compositor_ShouldKeepIdenticalFramesAtConstantBrightness_ThroughoutFade()
        {
            await using var playback = await Playback.CreateAsync(500, identicalFrames: true);
            await playback.PendingFrame;
            var canvas = playback.Window.Content;
            playback.Window.Content = null;
            var host = new Window
            {
                Width = 20,
                Height = 20,
                WindowDecorations = WindowDecorations.None,
                Content = canvas
            };
            try
            {
                host.Show();
                foreach (var milliseconds in new[] { 0, 300, 350, 400, 450, 500 })
                {
                    playback.Advance(milliseconds);
                    // 读取合成器实际输出；RenderTargetBitmap 的离屏路径无法覆盖此问题。
                    using var frame = Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(host);
                    Assert.NotNull(frame);
                    using var pixels = frame.Lock();
                    var center = pixels.Address + 10 * pixels.RowBytes + 10 * 4;
                    for (int channel = 0; channel < 3; channel++)
                    {
                        Assert.InRange(Marshal.ReadByte(center, channel), (byte)159, (byte)161);
                    }
                }
            }
            finally
            {
                host.Content = null;
                host.Close();
                playback.Window.Content = canvas;
            }
        }

        [AvaloniaFact]
        public async Task Playback_ShouldHoldCurrentFrame_UntilNextDecodeCompletes()
        {
            await using var playback = await Playback.CreateAsync(500, blockSecondFrame: true);
            await playback.Player.SecondFrameEntered.Task;

            playback.Advance(300);
            playback.Advance(1000);
            Assert.Equal(new SKColor(255, 0, 0), playback.CurrentPixel);
            Assert.Equal(0, playback.Overlay.Opacity);

            playback.Player.ReleaseSecondFrame.TrySetResult(true);
            await playback.PendingFrame;
            playback.Advance(1000);
            Assert.Equal(0, playback.Overlay.Opacity);
            playback.Advance(1100);
            Assert.Equal(0.5, playback.Overlay.Opacity, 6);
            playback.Advance(1200);
            Assert.Equal(new SKColor(0, 0, 255), playback.CurrentPixel);
        }

        [AvaloniaFact]
        public async Task Close_ShouldWaitForDecode_BeforeDisposingItsResources()
        {
            await using var playback = await Playback.CreateAsync(500, blockSecondFrame: true);
            await playback.Player.SecondFrameEntered.Task;

            var stopped = playback.StopAsync();
            Assert.False(stopped.IsCompleted);
            Assert.False(playback.Player.Disposed);
            playback.Window.ResumeRendering();
            Assert.False(playback.Timer.IsEnabled);

            playback.Player.ReleaseSecondFrame.TrySetResult(true);
            await stopped;
            Assert.True(playback.Player.Disposed);
            Assert.True(playback.PendingFrame.IsCompletedSuccessfully);
        }

        [AvaloniaFact]
        public async Task SingleFrame_ShouldRemainStatic_WithoutStartingAnimation()
        {
            await using var playback = await Playback.CreateAsync(500, singleFrame: true);
            Assert.Equal(new SKColor(255, 0, 0), playback.CurrentPixel);
            Assert.False(playback.Timer.IsEnabled);
        }

        private sealed class Playback : IAsyncDisposable
        {
            private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            private readonly string _directory;
            public WallpaperPlaybackWindow Window { get; }
            public GatedPlayer Player { get; }
            public DispatcherTimer Timer => Field<DispatcherTimer>("_timer");
            public Task<FrameRenderResult> PendingFrame => Field<Task<FrameRenderResult>>("_pendingFrame");
            public Border Overlay => ((Canvas)Window.Content!).Children.OfType<Border>().Single();
            public SKColor CurrentPixel => ReadPixel(((Canvas)Window.Content!).Children.OfType<Image>().Single());
            public SKColor NextPixel => ReadPixel((Image)Overlay.Child!);

            private Playback(string directory, GatedPlayer player)
            {
                _directory = directory;
                Player = player;
                Window = new WallpaperPlaybackWindow(
                    NullLogger<WallpaperPlaybackWindow>.Instance,
                    player,
                    IntPtr.Zero,
                    new[] { new WallpaperPlaybackWindow.DisplayRegion(0, 0, 20, 20) },
                    loopPauseMilliseconds: 1000);
                ((Canvas)Window.Content!).Background = Window.Background;
            }

            public static async Task<Playback> CreateAsync(int interval, bool blockSecondFrame = false, bool singleFrame = false, bool identicalFrames = false)
            {
                var directory = Path.Combine(Path.GetTempPath(), "EarthBackground.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                var paths = singleFrame
                    ? new[] { Path.Combine(directory, "first.png") }
                    : new[] { Path.Combine(directory, "first.png"), Path.Combine(directory, "second.png") };
                var colors = identicalFrames
                    ? new[] { new SKColor(160, 160, 160), new SKColor(160, 160, 160) }
                    : new[] { new SKColor(255, 0, 0), new SKColor(0, 0, 255) };
                for (int i = 0; i < paths.Length; i++)
                {
                    using var image = new SKBitmap(2, 2);
                    image.Erase(colors[i]);
                    if (i == 1 && !identicalFrames)
                    {
                        image.SetPixel(1, 0, SKColors.Transparent);
                        image.SetPixel(1, 1, SKColors.Transparent);
                    }
                    using var output = File.Create(paths[i]);
                    Assert.True(image.Encode(output, SKEncodedImageFormat.Png, 100));
                }

                var playback = new Playback(directory, new GatedPlayer(PngSequencePlayer.Open(paths, interval), blockSecondFrame));
                await (Task)playback.Invoke("StartPlaybackAsync")!;
                playback.Timer.Stop();
                return playback;
            }

            public void Advance(double milliseconds)
            {
                Invoke("AdvancePlayback", TimeSpan.FromMilliseconds(milliseconds));
                Timer.Stop();
            }

            public (SKColor Left, SKColor Right) CapturePixels()
            {
                var canvas = (Canvas)Window.Content!;
                canvas.Measure(new Size(20, 20));
                canvas.Arrange(new Rect(0, 0, 20, 20));
                using var target = new RenderTargetBitmap(new PixelSize(20, 20), new Vector(96, 96));
                target.Render(canvas);
                using var stream = new MemoryStream();
                target.Save(stream, new PngBitmapEncoderOptions());
                stream.Position = 0;
                using var image = SKBitmap.Decode(stream);
                return (image.GetPixel(1, 10), image.GetPixel(18, 10));
            }

            public Task StopAsync() => (Task)Invoke("StopPlaybackAsync")!;

            public async ValueTask DisposeAsync()
            {
                Player.ReleaseSecondFrame.TrySetResult(true);
                await StopAsync();
                Window.Close();
                Directory.Delete(_directory, true);
            }

            private T Field<T>(string name) => (T)typeof(WallpaperPlaybackWindow).GetField(name, PrivateInstance)!.GetValue(Window)!;

            private object? Invoke(string name, params object[] arguments)
                => typeof(WallpaperPlaybackWindow).GetMethod(name, PrivateInstance)!.Invoke(Window, arguments);

            private static SKColor ReadPixel(Image image)
            {
                using var pixels = ((WriteableBitmap)image.Source!).Lock();
                return new SKColor(
                    Marshal.ReadByte(pixels.Address, 0),
                    Marshal.ReadByte(pixels.Address, 1),
                    Marshal.ReadByte(pixels.Address, 2),
                    Marshal.ReadByte(pixels.Address, 3));
            }
        }

        private sealed class GatedPlayer : IWallpaperFramePlayer
        {
            private readonly IWallpaperFramePlayer _inner;
            private int _renderCount;
            public TaskCompletionSource<bool> SecondFrameEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> ReleaseSecondFrame { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool DecodedOnUiThread { get; private set; }
            public bool Disposed { get; private set; }
            public int FrameCount => _inner.FrameCount;
            public PixelSize PixelSize => _inner.PixelSize;

            public GatedPlayer(IWallpaperFramePlayer inner, bool blockSecondFrame)
            {
                _inner = inner;
                if (!blockSecondFrame) ReleaseSecondFrame.SetResult(true);
            }

            public FrameRenderResult RenderNextFrame(WriteableBitmap bitmap)
            {
                DecodedOnUiThread |= Dispatcher.UIThread.CheckAccess();
                if (++_renderCount == 2)
                {
                    SecondFrameEntered.SetResult(true);
                    ReleaseSecondFrame.Task.GetAwaiter().GetResult();
                }
                ObjectDisposedException.ThrowIf(Disposed, this);
                return _inner.RenderNextFrame(bitmap);
            }

            public void Dispose()
            {
                Disposed = true;
                _inner.Dispose();
            }
        }
    }
}
