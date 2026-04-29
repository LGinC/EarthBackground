using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
    [SupportedOSPlatform("windows")]
    public class WindowsDynamicWallpaperSetter : IBackgroundSetter, IDynamicWallpaperSetter, IDisposable
    {
        public string Platform => nameof(OSPlatform.Windows);

        private readonly ILogger<WindowsDynamicWallpaperSetter> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<CaptureOption> _captureOptions;
        private readonly IWallpaperMonitorProvider _monitorProvider;
        private readonly IWindowsWallpaperOcclusionDetector _occlusionDetector;
        private readonly List<PlaybackSession> _playbackSessions = new();
        private readonly DispatcherTimer _occlusionTimer;
        private string[]? _currentFramePaths;
        private string[]? _currentMonitorIds;
        private int _currentFrameIntervalMs;
        private IntPtr _workerW = IntPtr.Zero;

        public WindowsDynamicWallpaperSetter(
            ILogger<WindowsDynamicWallpaperSetter> logger,
            IServiceProvider serviceProvider,
            IOptionsMonitor<CaptureOption> captureOptions,
            IWallpaperMonitorProvider monitorProvider,
            IWindowsWallpaperOcclusionDetector occlusionDetector)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _captureOptions = captureOptions;
            _monitorProvider = monitorProvider;
            _occlusionDetector = occlusionDetector;
            _occlusionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _occlusionTimer.Tick += OnOcclusionTimerTick;
        }

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            StopDynamicBackground();
            Wallpaper.Set(filePath);
            return Task.CompletedTask;
        }

        public async Task SetDynamicBackgroundAsync(IReadOnlyList<string> filePaths, int frameIntervalMs = 500, Action<int, int>? onProgress = null, CancellationToken token = default)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return;
            }

            token.ThrowIfCancellationRequested();
            var orderedFilePaths = OrderFramePaths(filePaths);
            var monitors = GetTargetMonitors(_monitorProvider.GetMonitors(), _captureOptions.CurrentValue.DynamicWallpaperMonitorIds);
            var targetMonitorIds = OrderMonitorIds(monitors.Select(monitor => monitor.Id));

            if (IsSamePlaybackRequest(orderedFilePaths, frameIntervalMs, targetMonitorIds))
            {
                onProgress?.Invoke(2, 2);
                _logger.LogInformation("动态壁纸帧集合和目标显示器未变化，跳过重建，共 {Count} 帧", orderedFilePaths.Count);
                return;
            }

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

            onProgress?.Invoke(1, 2);
            var frameCount = 0;
            var showStopwatch = Stopwatch.StartNew();
            var newSessions = new List<PlaybackSession>(monitors.Count);
            try
            {
                token.ThrowIfCancellationRequested();
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    token.ThrowIfCancellationRequested();
                    var logger = _serviceProvider.GetRequiredService<ILogger<WallpaperPlaybackWindow>>();
                    foreach (var monitor in monitors)
                    {
                        var framePlayer = await Task.Run(() => PngSequencePlayer.Open(orderedFilePaths, frameIntervalMs), token);
                        if (frameCount == 0)
                        {
                            frameCount = framePlayer.FrameCount;
                        }

                        var displayRegions = new[]
                        {
                            new WallpaperPlaybackWindow.DisplayRegion(monitor.X, monitor.Y, monitor.Width, monitor.Height)
                        };

                        var window = new WallpaperPlaybackWindow(
                            logger,
                            framePlayer,
                            _workerW,
                            displayRegions,
                            _captureOptions.CurrentValue.LoopPauseMilliseconds);
                        newSessions.Add(new PlaybackSession(monitor, window));
                        await window.ShowEmbeddedAsync();
                    }

                    var oldSessions = _playbackSessions.ToArray();
                    _occlusionTimer.Stop();
                    _playbackSessions.Clear();
                    _playbackSessions.AddRange(newSessions);
                    foreach (var session in oldSessions)
                    {
                        session.Window.Close();
                    }

                    OnOcclusionTimerTick(this, EventArgs.Empty);
                    _occlusionTimer.Start();
                });

                _currentFramePaths = orderedFilePaths.ToArray();
                _currentMonitorIds = targetMonitorIds.ToArray();
                _currentFrameIntervalMs = frameIntervalMs;
            }
            catch
            {
                foreach (var session in newSessions)
                {
                    Dispatcher.UIThread.Post(session.Window.Close);
                }

                throw;
            }
            showStopwatch.Stop();
            onProgress?.Invoke(2, 2);

            _logger.LogInformation(
                "动态壁纸已启动，共 {Count} 帧，显示器数={MonitorCount}，播放源=PNG 序列，耗时: 展示={ShowMs}ms",
                frameCount,
                monitors.Count,
                showStopwatch.ElapsedMilliseconds);
        }

        public void StopDynamicBackground()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _occlusionTimer.Stop();
                foreach (var session in _playbackSessions)
                {
                    session.Window.Close();
                }

                _playbackSessions.Clear();
            });

            _currentFramePaths = null;
            _currentMonitorIds = null;
            _currentFrameIntervalMs = 0;
            _workerW = IntPtr.Zero;
            _logger.LogInformation("动态壁纸已停止");
        }

        private bool IsSamePlaybackRequest(
            IReadOnlyList<string> orderedFilePaths,
            int frameIntervalMs,
            IReadOnlyList<string> orderedMonitorIds)
        {
            if (_playbackSessions.Count == 0 || _currentFramePaths == null || _currentMonitorIds == null)
            {
                return false;
            }

            if (_currentFrameIntervalMs != frameIntervalMs ||
                _currentFramePaths.Length != orderedFilePaths.Count ||
                _currentMonitorIds.Length != orderedMonitorIds.Count)
            {
                return false;
            }

            for (int i = 0; i < orderedFilePaths.Count; i++)
            {
                if (!string.Equals(_currentFramePaths[i], orderedFilePaths[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            for (int i = 0; i < orderedMonitorIds.Count; i++)
            {
                if (!string.Equals(_currentMonitorIds[i], orderedMonitorIds[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
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
            _occlusionTimer.Tick -= OnOcclusionTimerTick;
        }

        private void OnOcclusionTimerTick(object? sender, EventArgs e)
        {
            if (_playbackSessions.Count == 0)
            {
                _occlusionTimer.Stop();
                return;
            }

            var excludedWindowHandles = _playbackSessions
                .Select(static session => session.Window.NativeWindowHandle)
                .Where(static handle => handle != IntPtr.Zero)
                .ToHashSet();
            var monitors = _playbackSessions.Select(static session => session.Monitor).ToArray();
            var occludedMonitorIds = _occlusionDetector.GetOccludedMonitorIds(monitors, excludedWindowHandles);

            foreach (var session in _playbackSessions)
            {
                if (occludedMonitorIds.Contains(session.Monitor.Id))
                {
                    session.Window.SuspendRendering();
                }
                else
                {
                    session.Window.ResumeRendering();
                }
            }
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
        private const uint SMTO_NORMAL = 0x0000;

        public static IReadOnlyList<WallpaperMonitor> SelectTargetMonitors(
            IReadOnlyList<WallpaperMonitor> monitors,
            IReadOnlyList<string>? selectedMonitorIds)
        {
            return WallpaperMonitorSelection.SelectTargetMonitors(monitors, selectedMonitorIds);
        }

        private IReadOnlyList<WallpaperMonitor> GetTargetMonitors(
            IReadOnlyList<WallpaperMonitor> monitors,
            IReadOnlyList<string>? selectedMonitorIds)
        {
            var targetMonitors = SelectTargetMonitors(monitors, selectedMonitorIds);
            if (selectedMonitorIds != null && selectedMonitorIds.Count > 0 && targetMonitors.Count == monitors.Count)
            {
                var selectedIds = new HashSet<string>(selectedMonitorIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
                if (selectedIds.Count > 0 && !monitors.Any(monitor => selectedIds.Contains(monitor.Id)))
                {
                    _logger.LogWarning("配置的动态壁纸显示器当前均不可用，回退到全部显示器: {MonitorIds}", string.Join("; ", selectedIds));
                }
            }

            _logger.LogInformation("动态壁纸目标显示器: {Monitors}", string.Join("; ", targetMonitors));
            return targetMonitors;
        }

        private static IReadOnlyList<string> OrderFramePaths(IReadOnlyList<string> filePaths)
        {
            return filePaths
                .OrderBy(static path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IReadOnlyList<string> OrderMonitorIds(IEnumerable<string> monitorIds)
        {
            return monitorIds
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private sealed record PlaybackSession(WallpaperMonitor Monitor, WallpaperPlaybackWindow Window);
    }
}
