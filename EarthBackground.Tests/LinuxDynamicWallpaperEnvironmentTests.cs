using System;
using EarthBackground.Background;
using Xunit;

namespace EarthBackground.Tests
{
    public class LinuxDynamicWallpaperEnvironmentTests
    {
        [Fact]
        public void ShouldBlockDynamicWallpaper_ShouldReturnTrue_ForWslXrdpMonitor()
        {
            var monitors = new[]
            {
                new WallpaperMonitor("LinuxDisplay:rdp0", "rdp0 (2560x1440)", 0, 0, 2560, 1440)
            };

            var blocked = LinuxDynamicWallpaperEnvironment.ShouldBlockDynamicWallpaper(
                monitors,
                new LinuxDynamicWallpaperEnvironment.SessionInfo(true, null, null, null));

            Assert.True(blocked);
        }

        [Fact]
        public void ShouldBlockDynamicWallpaper_ShouldReturnTrue_ForXrdpDesktopSession()
        {
            var monitors = new[]
            {
                new WallpaperMonitor("LinuxDisplay:HDMI-1", "HDMI-1 (1920x1080)", 0, 0, 1920, 1080)
            };

            var blocked = LinuxDynamicWallpaperEnvironment.ShouldBlockDynamicWallpaper(
                monitors,
                new LinuxDynamicWallpaperEnvironment.SessionInfo(false, "xrdp", null, null));

            Assert.True(blocked);
        }

        [Fact]
        public void ShouldBlockDynamicWallpaper_ShouldReturnTrue_ForRemoteDesktopMonitorWithoutWslMarkers()
        {
            var monitors = new[]
            {
                new WallpaperMonitor("LinuxDisplay:rdp0", "rdp0 (2560x1440)", 0, 0, 2560, 1440)
            };

            var blocked = LinuxDynamicWallpaperEnvironment.ShouldBlockDynamicWallpaper(
                monitors,
                new LinuxDynamicWallpaperEnvironment.SessionInfo(false, null, null, null));

            Assert.True(blocked);
        }

        [Fact]
        public void ShouldBlockDynamicWallpaper_ShouldReturnFalse_ForRegularX11Monitor()
        {
            var monitors = new[]
            {
                new WallpaperMonitor("LinuxDisplay:HDMI-1", "HDMI-1 (1920x1080)", 0, 0, 1920, 1080)
            };

            var blocked = LinuxDynamicWallpaperEnvironment.ShouldBlockDynamicWallpaper(
                monitors,
                new LinuxDynamicWallpaperEnvironment.SessionInfo(false, "ubuntu", "ubuntu:GNOME", null));

            Assert.False(blocked);
        }
    }
}
