using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Background
{
    public sealed class LinuxWallpaperMonitorProvider : IWallpaperMonitorProvider
    {
        private const string FallbackMonitorPrefix = "LinuxDisplay:";
        private readonly ILogger<LinuxWallpaperMonitorProvider> _logger;

        public LinuxWallpaperMonitorProvider(ILogger<LinuxWallpaperMonitorProvider> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<WallpaperMonitor> GetMonitors()
        {
            try
            {
                var xrandrOutput = TryGetXrandrMonitorOutput();
                var xrandrMonitors = ParseXrandrMonitors(xrandrOutput);
                if (xrandrMonitors.Count > 0)
                {
                    _logger.LogInformation("通过 xrandr 检测到显示器: {Monitors}", string.Join("; ", xrandrMonitors));
                    return xrandrMonitors;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "xrandr 显示器枚举失败，回退到 Avalonia Screens");
            }

            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    return GetFallbackMonitorsOnUiThread();
                }

                return Dispatcher.UIThread.Invoke(GetFallbackMonitorsOnUiThread);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Avalonia 显示器枚举失败，使用单显示器兜底值");
                return BuildFallbackMonitors(new[] { new ScreenSnapshot(0, 0, 1920, 1080) });
            }
        }

        internal static IReadOnlyList<WallpaperMonitor> ParseXrandrMonitors(string? output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return Array.Empty<WallpaperMonitor>();
            }

            var monitors = new List<WallpaperMonitor>();
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var rawLine in lines)
            {
                if (rawLine.StartsWith("Monitors:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryParseXrandrMonitor(rawLine, out var monitor))
                {
                    continue;
                }

                monitors.Add(monitor);
            }

            return monitors;
        }

        internal static IReadOnlyList<WallpaperMonitor> BuildFallbackMonitors(IReadOnlyList<ScreenSnapshot> screens)
        {
            if (screens.Count == 0)
            {
                return new[]
                {
                    new WallpaperMonitor(
                        FallbackMonitorPrefix + "1920x1080@0,0",
                        "Display1 (1920x1080)",
                        0,
                        0,
                        1920,
                        1080)
                };
            }

            var monitors = new WallpaperMonitor[screens.Count];
            for (var i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                monitors[i] = new WallpaperMonitor(
                    $"{FallbackMonitorPrefix}{screen.Width}x{screen.Height}@{screen.X},{screen.Y}",
                    $"Display{i + 1} ({screen.Width}x{screen.Height})",
                    screen.X,
                    screen.Y,
                    screen.Width,
                    screen.Height);
            }

            return monitors;
        }

        internal readonly record struct ScreenSnapshot(int X, int Y, int Width, int Height);

        private IReadOnlyList<WallpaperMonitor> GetFallbackMonitorsOnUiThread()
        {
            var screens = GetScreens();
            if (screens == null)
            {
                return BuildFallbackMonitors(Array.Empty<ScreenSnapshot>());
            }

            var snapshots = screens.All
                .Select(static screen => new ScreenSnapshot(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height))
                .ToArray();

            if (snapshots.Length == 0 && screens.Primary != null)
            {
                var bounds = screens.Primary.Bounds;
                snapshots = new[] { new ScreenSnapshot(bounds.X, bounds.Y, bounds.Width, bounds.Height) };
            }

            var monitors = BuildFallbackMonitors(snapshots);
            _logger.LogInformation("通过 Avalonia Screens 检测到 Linux 显示器: {Monitors}", string.Join("; ", monitors));
            return monitors;
        }

        private static string? TryGetXrandrMonitorOutput()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return null;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xrandr",
                    Arguments = "--listmonitors",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }

        private static bool TryParseXrandrMonitor(string line, out WallpaperMonitor monitor)
        {
            monitor = default;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                return false;
            }

            var geometryTokenIndex = -1;
            for (var i = 0; i < parts.Length; i++)
            {
                if (TryParseGeometry(parts[i], out _, out _, out _, out _))
                {
                    geometryTokenIndex = i;
                    break;
                }
            }

            if (geometryTokenIndex < 0)
            {
                return false;
            }

            var monitorName = parts[^1];
            if (!TryParseGeometry(parts[geometryTokenIndex], out var width, out var height, out var x, out var y))
            {
                return false;
            }

            monitor = new WallpaperMonitor(
                FallbackMonitorPrefix + monitorName,
                $"{monitorName} ({width}x{height})",
                x,
                y,
                width,
                height);
            return true;
        }

        private static bool TryParseGeometry(string value, out int width, out int height, out int x, out int y)
        {
            width = 0;
            height = 0;
            x = 0;
            y = 0;

            var firstSlash = value.IndexOf('/');
            var xSeparator = value.IndexOf('x', StringComparison.OrdinalIgnoreCase);
            if (firstSlash <= 0 || xSeparator <= firstSlash)
            {
                return false;
            }

            if (!int.TryParse(value.AsSpan(0, firstSlash), NumberStyles.None, CultureInfo.InvariantCulture, out width))
            {
                return false;
            }

            var secondSlash = value.IndexOf('/', xSeparator + 1);
            if (secondSlash <= xSeparator + 1 ||
                !int.TryParse(value.AsSpan(xSeparator + 1, secondSlash - xSeparator - 1), NumberStyles.None, CultureInfo.InvariantCulture, out height))
            {
                return false;
            }

            var firstPlus = value.IndexOf('+', secondSlash + 1);
            if (firstPlus < 0)
            {
                return false;
            }

            var secondPlus = value.IndexOf('+', firstPlus + 1);
            if (secondPlus < 0)
            {
                return false;
            }

            if (!int.TryParse(value.AsSpan(firstPlus + 1, secondPlus - firstPlus - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out x) ||
                !int.TryParse(value.AsSpan(secondPlus + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out y))
            {
                return false;
            }

            return true;
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
