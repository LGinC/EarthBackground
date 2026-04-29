using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Background
{
    internal sealed class WindowsWallpaperOcclusionDetector : IWindowsWallpaperOcclusionDetector
    {
        private const double EarthRegionWidthRatio = 0.50;
        private const double EarthRegionHeightRatio = 0.80;
        private const double OcclusionThreshold = 0.70;
        private const int DWMWA_CLOAKED = 14;

        private static readonly HashSet<string> ExcludedClassNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Progman",
            "WorkerW",
            "SHELLDLL_DefView",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Button"
        };

        private readonly ILogger<WindowsWallpaperOcclusionDetector> _logger;

        public WindowsWallpaperOcclusionDetector(ILogger<WindowsWallpaperOcclusionDetector> logger)
        {
            _logger = logger;
        }

        public IReadOnlySet<string> GetOccludedMonitorIds(
            IReadOnlyList<WallpaperMonitor> monitors,
            IReadOnlySet<IntPtr> excludedWindowHandles)
        {
            var occludedMonitorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (monitors.Count == 0)
            {
                return occludedMonitorIds;
            }

            try
            {
                EnumWindows((hwnd, _) =>
                {
                    if (!TryGetCandidateWindowRect(hwnd, excludedWindowHandles, out var windowRect))
                    {
                        return true;
                    }

                    foreach (var monitor in monitors)
                    {
                        if (occludedMonitorIds.Contains(monitor.Id))
                        {
                            continue;
                        }

                        if (IsEarthRegionOccludedByWindow(monitor, windowRect))
                        {
                            occludedMonitorIds.Add(monitor.Id);
                        }
                    }

                    return occludedMonitorIds.Count < monitors.Count;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "检测动态壁纸遮挡状态失败");
            }

            return occludedMonitorIds;
        }

        private static bool TryGetCandidateWindowRect(
            IntPtr hwnd,
            IReadOnlySet<IntPtr> excludedWindowHandles,
            out ScreenRect windowRect)
        {
            windowRect = default;

            if (hwnd == IntPtr.Zero ||
                excludedWindowHandles.Contains(hwnd) ||
                !IsWindowVisible(hwnd) ||
                IsIconic(hwnd) ||
                IsCloaked(hwnd))
            {
                return false;
            }

            var className = GetWindowClassName(hwnd);
            if (ExcludedClassNames.Contains(className))
            {
                return false;
            }

            if (!GetWindowRect(hwnd, out var rect))
            {
                return false;
            }

            windowRect = new ScreenRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            return windowRect.Width > 0 && windowRect.Height > 0;
        }

        internal static ScreenRect GetEarthRegion(WallpaperMonitor monitor)
        {
            var width = (int)Math.Round(monitor.Width * EarthRegionWidthRatio, MidpointRounding.AwayFromZero);
            var height = (int)Math.Round(monitor.Height * EarthRegionHeightRatio, MidpointRounding.AwayFromZero);
            var left = monitor.X + ((monitor.Width - width) / 2);
            var top = monitor.Y + ((monitor.Height - height) / 2);
            return new ScreenRect(left, top, left + width, top + height);
        }

        internal static bool IsEarthRegionOccludedByWindow(WallpaperMonitor monitor, ScreenRect windowRect)
        {
            var earthRegion = GetEarthRegion(monitor);
            var intersection = earthRegion.Intersect(windowRect);
            if (intersection.Area <= 0 || earthRegion.Area <= 0)
            {
                return false;
            }

            return intersection.Area / (double)earthRegion.Area >= OcclusionThreshold;
        }

        internal static bool IsExcludedShellWindowClass(string className)
            => ExcludedClassNames.Contains(className);

        private static bool IsCloaked(IntPtr hwnd)
        {
            return DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out var cloaked, Marshal.SizeOf<int>()) == 0 && cloaked != 0;
        }

        private static string GetWindowClassName(IntPtr hwnd)
        {
            var className = new StringBuilder(256);
            var length = GetClassName(hwnd, className, className.Capacity);
            return length > 0 ? className.ToString() : string.Empty;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }

    internal readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Math.Max(0, Right - Left);

        public int Height => Math.Max(0, Bottom - Top);

        public long Area => (long)Width * Height;

        public ScreenRect Intersect(ScreenRect other)
        {
            return new ScreenRect(
                Math.Max(Left, other.Left),
                Math.Max(Top, other.Top),
                Math.Min(Right, other.Right),
                Math.Min(Bottom, other.Bottom));
        }
    }
}
