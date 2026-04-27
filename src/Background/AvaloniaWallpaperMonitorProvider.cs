using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Background
{
    public sealed class AvaloniaWallpaperMonitorProvider : IWallpaperMonitorProvider
    {
        private readonly ILogger<AvaloniaWallpaperMonitorProvider> _logger;

        public AvaloniaWallpaperMonitorProvider(ILogger<AvaloniaWallpaperMonitorProvider> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<WallpaperMonitor> GetMonitors()
        {
            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    return GetMonitorsOnUiThread();
                }

                return Dispatcher.UIThread.Invoke(GetMonitorsOnUiThread);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Avalonia 显示器枚举失败，使用主显示器兜底值");
                return new[] { new WallpaperMonitor("DISPLAY1:0,0,1920,1080", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080) };
            }
        }

        private IReadOnlyList<WallpaperMonitor> GetMonitorsOnUiThread()
        {
            var screens = GetScreens();
            if (screens == null)
            {
                return new[] { new WallpaperMonitor("DISPLAY1:0,0,1920,1080", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080) };
            }

            var monitors = screens.All
                .Select((screen, index) =>
                {
                    var bounds = screen.Bounds;
                    var id = $"NSScreen{index + 1}:{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}";
                    var name = $"NSScreen{index + 1} ({bounds.Width}x{bounds.Height})";
                    return new WallpaperMonitor(id, name, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                })
                .ToArray();

            if (monitors.Length == 0 && screens.Primary != null)
            {
                var bounds = screens.Primary.Bounds;
                monitors = new[]
                {
                    new WallpaperMonitor(
                        $"NSScreen1:{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}",
                        $"NSScreen1 ({bounds.Width}x{bounds.Height})",
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height)
                };
            }

            _logger.LogInformation("通过 Avalonia Screens 检测到显示器: {Monitors}", string.Join("; ", monitors));
            return monitors;
        }

        private static Screens? GetScreens()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
            {
                return mainWindow.Screens;
            }

            return null;
        }
    }
}
