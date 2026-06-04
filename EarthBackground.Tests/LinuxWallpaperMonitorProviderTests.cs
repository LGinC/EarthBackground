using System;
using System.Collections.Generic;
using EarthBackground.Background;
using Xunit;

namespace EarthBackground.Tests
{
    public class LinuxWallpaperMonitorProviderTests
    {
        [Fact]
        public void ParseXrandrMonitors_ShouldUseStableOutputNameAsMonitorId()
        {
            const string xrandrOutput = "Monitors: 2\n 0: +*HDMI-1 1920/530x1080/300+0+0  HDMI-1\n 1: +DP-1 2560/600x1440/340+1920+0  DP-1\n";

            var monitors = LinuxWallpaperMonitorProvider.ParseXrandrMonitors(xrandrOutput);

            Assert.Collection(
                monitors,
                monitor =>
                {
                    Assert.Equal("LinuxDisplay:HDMI-1", monitor.Id);
                    Assert.Equal("HDMI-1 (1920x1080)", monitor.DisplayName);
                    Assert.Equal(0, monitor.X);
                    Assert.Equal(0, monitor.Y);
                    Assert.Equal(1920, monitor.Width);
                    Assert.Equal(1080, monitor.Height);
                },
                monitor =>
                {
                    Assert.Equal("LinuxDisplay:DP-1", monitor.Id);
                    Assert.Equal("DP-1 (2560x1440)", monitor.DisplayName);
                    Assert.Equal(1920, monitor.X);
                    Assert.Equal(0, monitor.Y);
                    Assert.Equal(2560, monitor.Width);
                    Assert.Equal(1440, monitor.Height);
                });
        }

        [Fact]
        public void BuildFallbackMonitors_ShouldUseLinuxPrefixAndGeometry()
        {
            var monitors = LinuxWallpaperMonitorProvider.BuildFallbackMonitors(
                new[]
                {
                    new LinuxWallpaperMonitorProvider.ScreenSnapshot(0, 0, 1920, 1080),
                    new LinuxWallpaperMonitorProvider.ScreenSnapshot(1920, 0, 2560, 1440)
                });

            Assert.Collection(
                monitors,
                monitor =>
                {
                    Assert.Equal("LinuxDisplay:1920x1080@0,0", monitor.Id);
                    Assert.Equal("Display1 (1920x1080)", monitor.DisplayName);
                },
                monitor =>
                {
                    Assert.Equal("LinuxDisplay:2560x1440@1920,0", monitor.Id);
                    Assert.Equal("Display2 (2560x1440)", monitor.DisplayName);
                });
        }
    }
}
