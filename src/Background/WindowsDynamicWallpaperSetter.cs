using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using EarthBackground.Imaging;
using EarthBackground.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EarthBackground.Background
{
    public class WindowsDynamicWallpaperSetter : IBackgroundSetter, IDisposable
    {
        public string Platform => nameof(OSPlatform.Windows);

        private readonly ILogger<WindowsDynamicWallpaperSetter> _logger;
        private readonly ApngAssembler _apngAssembler;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<CaptureOption> _captureOptions;
        private readonly List<WallpaperPlaybackWindow> _playbackWindows = new();
        private IntPtr _workerW = IntPtr.Zero;

        public WindowsDynamicWallpaperSetter(
            ILogger<WindowsDynamicWallpaperSetter> logger,
            ApngAssembler apngAssembler,
            IServiceProvider serviceProvider,
            IOptionsMonitor<CaptureOption> captureOptions)
        {
            _logger = logger;
            _apngAssembler = apngAssembler;
            _serviceProvider = serviceProvider;
            _captureOptions = captureOptions;
        }

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            StopDynamicBackground();
            Wallpaper.Set(filePath);
            return Task.CompletedTask;
        }

        public async Task SetDynamicBackgroundAsync(IReadOnlyList<string> filePaths, int frameIntervalMs = 500, CancellationToken token = default)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return;
            }

            var orderedFilePaths = OrderFramePaths(filePaths);

            StopDynamicBackground();

            _workerW = GetWorkerW(out var progman);
            if (_workerW == IntPtr.Zero)
            {
                _logger.LogWarning("未能获取 WorkerW 窗口，回退到静态壁纸");
                Wallpaper.Set(orderedFilePaths[0]);
                return;
            }

            _logger.LogInformation(
                "获取到 WorkerW: 0x{WorkerW:X}, Progman: 0x{Progman:X}, WorkerWClass={WorkerWClass}, ProgmanClass={ProgmanClass}",
                _workerW.ToInt64(),
                progman.ToInt64(),
                GetWindowClassName(_workerW),
                GetWindowClassName(progman));

            var monitors = GetMonitors();
            var apngPath = Path.Combine(Path.GetDirectoryName(orderedFilePaths[0]) ?? AppContext.BaseDirectory, "wallpaper.apng");

            var assembleStopwatch = Stopwatch.StartNew();
            _apngAssembler.CreateFromBitmaps(orderedFilePaths, apngPath, frameIntervalMs);
            assembleStopwatch.Stop();
            _logger.LogInformation("APNG 已生成: {ApngPath}，耗时 {ElapsedMs}ms", apngPath, assembleStopwatch.ElapsedMilliseconds);

            var parseStopwatch = Stopwatch.StartNew();
            var apngFrames = ApngParser.Parse(apngPath);
            parseStopwatch.Stop();
            _logger.LogInformation("APNG 已解析: {Count} 帧，耗时 {ElapsedMs}ms", apngFrames.Count, parseStopwatch.ElapsedMilliseconds);

            var showStopwatch = Stopwatch.StartNew();
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var logger = _serviceProvider.GetRequiredService<ILogger<WallpaperPlaybackWindow>>();
                var displayRegions = new List<WallpaperPlaybackWindow.DisplayRegion>(monitors.Count);
                foreach (var monitor in monitors)
                {
                    displayRegions.Add(new WallpaperPlaybackWindow.DisplayRegion(monitor.X, monitor.Y, monitor.Width, monitor.Height));
                }

                var window = new WallpaperPlaybackWindow(
                    logger,
                    apngFrames,
                    _workerW,
                    displayRegions,
                    _captureOptions.CurrentValue.LoopPauseMilliseconds);
                _playbackWindows.Add(window);
                await window.ShowEmbeddedAsync();
            });
            showStopwatch.Stop();

            _logger.LogInformation(
                "动态壁纸已启动，共 {Count} 帧，显示器数={MonitorCount}，播放源=APNG，APNG={ApngPath}，耗时: 生成={AssembleMs}ms, 解析={ParseMs}ms, 展示={ShowMs}ms",
                apngFrames.Count,
                monitors.Count,
                apngPath,
                assembleStopwatch.ElapsedMilliseconds,
                parseStopwatch.ElapsedMilliseconds,
                showStopwatch.ElapsedMilliseconds);
        }

        public void StopDynamicBackground()
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var window in _playbackWindows)
                {
                    window.Close();
                }

                _playbackWindows.Clear();
            });

            _workerW = IntPtr.Zero;
            _logger.LogInformation("动态壁纸已停止");
        }

        private IntPtr GetWorkerW(out IntPtr progman)
        {
            progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                _logger.LogWarning("未找到 Progman 窗口");
                return IntPtr.Zero;
            }

            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, new IntPtr(1), SMTO_NORMAL, 1000, out _);

            IntPtr workerW = TryGetWorkerWFromProgman(progman);
            if (workerW != IntPtr.Zero)
            {
                _logger.LogInformation("Win11 24H2+ 模式：WorkerW 是 Progman 子窗口");
                return workerW;
            }

            workerW = TryGetWorkerWFromTopLevel();
            if (workerW != IntPtr.Zero)
            {
                _logger.LogInformation("旧版模式：WorkerW 是顶层窗口");
                return workerW;
            }

            _logger.LogWarning("两种方式均未找到 WorkerW");
            return IntPtr.Zero;
        }

        private IntPtr TryGetWorkerWFromProgman(IntPtr progman)
        {
            IntPtr shellDllDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDllDefView == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return FindWindowEx(progman, shellDllDefView, "WorkerW", null);
        }

        private IntPtr TryGetWorkerWFromTopLevel()
        {
            IntPtr workerW = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                IntPtr shellDllDefView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDllDefView != IntPtr.Zero)
                {
                    workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                    return false;
                }

                return true;
            }, IntPtr.Zero);
            return workerW;
        }

        public void Dispose()
        {
            StopDynamicBackground();
        }

        private string GetWindowClassName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return "<zero>";

            var className = new System.Text.StringBuilder(256);
            var length = GetClassName(hwnd, className, className.Capacity);
            return length > 0 ? className.ToString() : "<unknown>";
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        private const uint SMTO_NORMAL = 0x0000;

        private IReadOnlyList<MonitorBounds> GetMonitors()
        {
            var monitors = new List<MonitorBounds>();

            bool OnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT rect, IntPtr dwData)
            {
                monitors.Add(new MonitorBounds(rect.Left, rect.Top, rect.Width, rect.Height));
                return true;
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, OnMonitor, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                monitors.Add(new MonitorBounds(0, 0, 1920, 1080));
            }

            _logger.LogInformation("检测到显示器: {Monitors}", string.Join("; ", monitors));
            return monitors;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        private readonly record struct MonitorBounds(int X, int Y, int Width, int Height)
        {
            public override string ToString() => $"{X},{Y},{Width},{Height}";
        }

        private static IReadOnlyList<string> OrderFramePaths(IReadOnlyList<string> filePaths)
        {
            return filePaths
                .OrderBy(static path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
