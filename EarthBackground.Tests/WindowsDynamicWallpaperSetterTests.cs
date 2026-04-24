using System;
using EarthBackground.Background;
using Xunit;

namespace EarthBackground.Tests
{
    public class WindowsDynamicWallpaperSetterTests
    {
        private static readonly WallpaperMonitor[] Monitors =
        {
            new(@"\\?\DISPLAY#MONITOR1", "DISPLAY1 (1920x1080)", 0, 0, 1920, 1080),
            new(@"\\?\DISPLAY#MONITOR2", "DISPLAY2 (2560x1440)", 1920, 0, 2560, 1440)
        };

        [Fact]
        public void SelectTargetMonitors_ShouldUseAllMonitors_WhenSelectionIsEmpty()
        {
            var selected = WindowsDynamicWallpaperSetter.SelectTargetMonitors(Monitors, Array.Empty<string>());

            Assert.Equal(Monitors, selected);
        }

        [Fact]
        public void SelectTargetMonitors_ShouldUseMatchingMonitors_WhenSelectionHasIds()
        {
            var selected = WindowsDynamicWallpaperSetter.SelectTargetMonitors(
                Monitors,
                new[] { @"\\?\DISPLAY#MONITOR2" });

            Assert.Single(selected);
            Assert.Equal(@"\\?\DISPLAY#MONITOR2", selected[0].Id);
        }

        [Fact]
        public void SelectTargetMonitors_ShouldFallbackToAllMonitors_WhenSelectionIsUnavailable()
        {
            var selected = WindowsDynamicWallpaperSetter.SelectTargetMonitors(
                Monitors,
                new[] { @"\\?\DISPLAY#MISSING" });

            Assert.Equal(Monitors, selected);
        }
    }
}
