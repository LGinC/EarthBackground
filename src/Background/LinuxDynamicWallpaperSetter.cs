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
    public sealed class LinuxDynamicWallpaperSetter : IDynamicWallpaperSetter, IDisposable
    {
        public string Platform => nameof(OSPlatform.Linux);

        private readonly ILogger<LinuxDynamicWallpaperSetter> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<CaptureOption> _captureOptions;
        private readonly IWallpaperMonitorProvider _monitorProvider;
        private readonly List<WallpaperPlaybackWindow> _playbackWindows = new();
        private string[]? _currentFramePaths;
        private string[]? _currentMonitorIds;
        private int _currentFrameIntervalMs;

        public LinuxDynamicWallpaperSetter(
            ILogger<LinuxDynamicWallpaperSetter> logger,
            IServiceProvider serviceProvider,
            IOptionsMonitor<CaptureOption> captureOptions,
            IWallpaperMonitorProvider monitorProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _captureOptions = captureOptions;
            _monitorProvider = monitorProvider;
        }

        public async Task SetDynamicBackgroundAsync(
            IReadOnlyList<string> filePaths,
            int frameIntervalMs = 500,
            Action<int, int>? onProgress = null,
            CancellationToken token = default)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return;
            }

            token.ThrowIfCancellationRequested();
            var orderedFilePaths = OrderFramePaths(filePaths);
            var monitors = WallpaperMonitorSelection.SelectTargetMonitors(
                _monitorProvider.GetMonitors(),
                _captureOptions.CurrentValue.DynamicWallpaperMonitorIds);
            var targetMonitorIds = OrderMonitorIds(monitors.Select(monitor => monitor.Id));

            if (IsSamePlaybackRequest(orderedFilePaths, frameIntervalMs, targetMonitorIds))
            {
                onProgress?.Invoke(2, 2);
                _logger.LogInformation("Linux 动态壁纸帧集合和目标显示器未变化，跳过重建，共 {Count} 帧", orderedFilePaths.Count);
                return;
            }

            onProgress?.Invoke(1, 2);
            var stopwatch = Stopwatch.StartNew();
            var newWindows = new List<WallpaperPlaybackWindow>();
            try
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    foreach (var monitor in monitors)
                    {
                        token.ThrowIfCancellationRequested();
                        var framePlayer = await Task.Run(() => PngSequencePlayer.Open(orderedFilePaths, frameIntervalMs), token);
                        var logger = _serviceProvider.GetRequiredService<ILogger<WallpaperPlaybackWindow>>();
                        var displayRegions = new[]
                        {
                            new WallpaperPlaybackWindow.DisplayRegion(monitor.X, monitor.Y, monitor.Width, monitor.Height)
                        };

                        var window = new WallpaperPlaybackWindow(
                            logger,
                            framePlayer,
                            IntPtr.Zero,
                            displayRegions,
                            _captureOptions.CurrentValue.LoopPauseMilliseconds);
                        newWindows.Add(window);
                        await window.ShowEmbeddedAsync();
                    }

                    var oldWindows = _playbackWindows.ToArray();
                    _playbackWindows.Clear();
                    _playbackWindows.AddRange(newWindows);
                    foreach (var window in oldWindows)
                    {
                        window.Close();
                    }
                });

                _currentFramePaths = orderedFilePaths.ToArray();
                _currentMonitorIds = targetMonitorIds.ToArray();
                _currentFrameIntervalMs = frameIntervalMs;
            }
            catch
            {
                foreach (var window in newWindows)
                {
                    Dispatcher.UIThread.Post(window.Close);
                }

                throw;
            }
            stopwatch.Stop();
            onProgress?.Invoke(2, 2);

            _logger.LogInformation(
                "Linux X11 动态壁纸已启动，共 {Count} 帧，显示器数={MonitorCount}，每个显示器独立窗口，耗时 {ElapsedMs}ms",
                orderedFilePaths.Count,
                monitors.Count,
                stopwatch.ElapsedMilliseconds);
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

            _currentFramePaths = null;
            _currentMonitorIds = null;
            _currentFrameIntervalMs = 0;
            _logger.LogInformation("Linux 动态壁纸已停止");
        }

        private bool IsSamePlaybackRequest(
            IReadOnlyList<string> orderedFilePaths,
            int frameIntervalMs,
            IReadOnlyList<string> orderedMonitorIds)
        {
            if (_playbackWindows.Count == 0 || _currentFramePaths == null || _currentMonitorIds == null)
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

        public void Dispose()
        {
            StopDynamicBackground();
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
    }
}
