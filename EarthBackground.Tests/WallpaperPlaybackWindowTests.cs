using System;
using System.Runtime.InteropServices;
using Avalonia;
using EarthBackground.Views;
using Xunit;

namespace EarthBackground.Tests
{
    public class WallpaperPlaybackWindowTests
    {
        public static bool IsWindows => OperatingSystem.IsWindows();

        [Theory(Skip = "需要 Windows 原生窗口 API", SkipUnless = nameof(IsWindows))]
        [InlineData(0, 0, 120, 80, 120, 80)]
        [InlineData(125, 73, 150, 100, 25, 27)]
        [InlineData(-1920, -8, 0, 0, 1920, 8)]
        public void ScreenToParentPoint_ShouldUseActualParentOrigin(
            int parentX, int parentY, int screenX, int screenY, int expectedX, int expectedY)
        {
            var parent = CreateWindowEx(0, "STATIC", null, 0x80000000,
                parentX, parentY, 800, 600, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            Assert.NotEqual(IntPtr.Zero, parent);
            try
            {
                var point = WallpaperPlaybackWindow.ScreenToParentPoint(parent, screenX, screenY);
                Assert.Equal(new PixelPoint(expectedX, expectedY), point);
            }
            finally
            {
                DestroyWindow(parent);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string? windowName,
            uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [Theory]
        [InlineData("X11")]
        [InlineData("x11")]
        [InlineData("XID")]
        [InlineData("xid")]
        public void IsX11WindowHandleDescriptor_ShouldAcceptX11Descriptors(string descriptor)
        {
            Assert.True(WallpaperPlaybackWindow.IsX11WindowHandleDescriptor(descriptor));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Wayland")]
        [InlineData("HWND")]
        public void IsX11WindowHandleDescriptor_ShouldRejectNonX11Descriptors(string? descriptor)
        {
            Assert.False(WallpaperPlaybackWindow.IsX11WindowHandleDescriptor(descriptor));
        }
    }
}
