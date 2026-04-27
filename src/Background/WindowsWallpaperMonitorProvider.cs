using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace EarthBackground.Background
{
    [SupportedOSPlatform("windows")]
    public sealed class WindowsWallpaperMonitorProvider : IWallpaperMonitorProvider
    {
        private readonly ILogger<WindowsWallpaperMonitorProvider> _logger;

        public WindowsWallpaperMonitorProvider(ILogger<WindowsWallpaperMonitorProvider> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<WallpaperMonitor> GetMonitors()
        {
            try
            {
                var monitors = GetMonitorsFromDesktopWallpaper();
                if (monitors.Count > 0)
                {
                    _logger.LogInformation("通过 IDesktopWallpaper 检测到显示器: {Monitors}", string.Join("; ", monitors));
                    return monitors;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IDesktopWallpaper 显示器枚举失败，回退到 EnumDisplayMonitors");
            }

            var fallback = GetMonitorsFromEnumDisplayMonitors();
            _logger.LogInformation("通过 EnumDisplayMonitors 检测到显示器: {Monitors}", string.Join("; ", fallback));
            return fallback;
        }

        private static List<WallpaperMonitor> GetMonitorsFromDesktopWallpaper()
        {
            var desktopWallpaperType = Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD"), throwOnError: true)!;
            var desktopWallpaper = (IDesktopWallpaper)Activator.CreateInstance(desktopWallpaperType)!;
            try
            {
                var displayDevices = GetActiveDisplayDevices();
                desktopWallpaper.GetMonitorDevicePathCount(out var count);
                var monitors = new List<WallpaperMonitor>((int)count);

                for (uint i = 0; i < count; i++)
                {
                    desktopWallpaper.GetMonitorDevicePathAt(i, out var monitorIdPtr);
                    var monitorId = PtrToStringAndFree(monitorIdPtr);
                    if (string.IsNullOrWhiteSpace(monitorId))
                    {
                        continue;
                    }

                    desktopWallpaper.GetMonitorRECT(monitorId, out var rect);
                    var displayDevice = FindDisplayDevice(displayDevices, rect);
                    var displayName = displayDevice != null
                        ? $"{displayDevice.Value.Name} ({rect.Width}x{rect.Height})"
                        : $"DISPLAY{i + 1} ({rect.Width}x{rect.Height})";
                    monitors.Add(new WallpaperMonitor(
                        monitorId,
                        displayName,
                        rect.Left,
                        rect.Top,
                        rect.Width,
                        rect.Height));
                }

                return monitors;
            }
            finally
            {
                if (Marshal.IsComObject(desktopWallpaper))
                {
                    Marshal.FinalReleaseComObject(desktopWallpaper);
                }
            }
        }

        private static List<WallpaperMonitor> GetMonitorsFromEnumDisplayMonitors()
        {
            var monitors = new List<WallpaperMonitor>();
            var displayDevices = GetActiveDisplayDevices();

            bool OnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT rect, IntPtr dwData)
            {
                var displayDevice = FindDisplayDevice(displayDevices, rect);
                var displayName = displayDevice != null
                    ? $"{displayDevice.Value.Name} ({rect.Width}x{rect.Height})"
                    : $"DISPLAY{monitors.Count + 1} ({rect.Width}x{rect.Height})";
                var id = displayDevice?.DeviceName ?? $"DISPLAY{monitors.Count + 1}:{rect.Left},{rect.Top},{rect.Width},{rect.Height}";
                monitors.Add(new WallpaperMonitor(
                    id,
                    displayName,
                    rect.Left,
                    rect.Top,
                    rect.Width,
                    rect.Height));
                return true;
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, OnMonitor, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                monitors.Add(new WallpaperMonitor("DISPLAY1:0,0,1920,1080", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080));
            }

            return monitors;
        }

        private static List<DisplayDeviceInfo> GetActiveDisplayDevices()
        {
            var displays = new List<DisplayDeviceInfo>();

            for (uint i = 0; ; i++)
            {
                var displayDevice = DISPLAY_DEVICE.Create();
                if (!EnumDisplayDevices(null, i, ref displayDevice, 0))
                {
                    break;
                }

                const int displayDeviceActive = 0x00000001;
                const int displayDeviceMirroringDriver = 0x00000008;
                if ((displayDevice.StateFlags & displayDeviceActive) == 0 ||
                    (displayDevice.StateFlags & displayDeviceMirroringDriver) != 0)
                {
                    continue;
                }

                var devMode = DEVMODE.Create();
                if (!EnumDisplaySettingsEx(displayDevice.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode, 0))
                {
                    continue;
                }

                displays.Add(new DisplayDeviceInfo(
                    displayDevice.DeviceName,
                    displayDevice.DeviceName.Replace(@"\\.\", string.Empty, StringComparison.OrdinalIgnoreCase),
                    devMode.dmPositionX,
                    devMode.dmPositionY,
                    devMode.dmPelsWidth,
                    devMode.dmPelsHeight));
            }

            return displays;
        }

        private static DisplayDeviceInfo? FindDisplayDevice(IReadOnlyList<DisplayDeviceInfo> displays, RECT rect)
        {
            foreach (var display in displays)
            {
                if (display.X == rect.Left &&
                    display.Y == rect.Top &&
                    display.Width == rect.Width &&
                    display.Height == rect.Height)
                {
                    return display;
                }
            }

            return null;
        }

        private static string PtrToStringAndFree(IntPtr value)
        {
            if (value == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                return Marshal.PtrToStringUni(value) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeCoTaskMem(value);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettingsEx(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

        private const int ENUM_CURRENT_SETTINGS = -1;

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        private readonly record struct DisplayDeviceInfo(string DeviceName, string Name, int X, int Y, int Width, int Height);

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public int StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;

            public static DISPLAY_DEVICE Create()
            {
                return new DISPLAY_DEVICE
                {
                    cb = Marshal.SizeOf<DISPLAY_DEVICE>(),
                    DeviceName = string.Empty,
                    DeviceString = string.Empty,
                    DeviceID = string.Empty,
                    DeviceKey = string.Empty
                };
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;

            public static DEVMODE Create()
            {
                return new DEVMODE
                {
                    dmSize = (short)Marshal.SizeOf<DEVMODE>(),
                    dmDeviceName = string.Empty,
                    dmFormName = string.Empty
                };
            }
        }

        [ComImport]
        [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDesktopWallpaper
        {
            void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
            void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, out IntPtr wallpaper);
            void GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId);
            void GetMonitorDevicePathCount(out uint count);
            void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out RECT displayRect);
            void SetBackgroundColor(uint color);
            void GetBackgroundColor(out uint color);
            void SetPosition(int position);
            void GetPosition(out int position);
            void SetSlideshow(IntPtr items);
            void GetSlideshow(out IntPtr items);
            void SetSlideshowOptions(uint options, uint slideshowTick);
            void GetSlideshowOptions(out uint options, out uint slideshowTick);
            void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, int direction);
            void GetStatus(out uint state);
            void Enable(bool enable);
        }
    }
}
