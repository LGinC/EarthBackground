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

        [Fact]
        public void GetEarthRegion_ShouldUseCenteredHalfWidthAndEightyPercentHeight()
        {
            var region = WindowsWallpaperOcclusionDetector.GetEarthRegion(Monitors[0]);

            Assert.Equal(new ScreenRect(480, 108, 1440, 972), region);
        }

        [Fact]
        public void GetEarthRegion_ShouldRespectMonitorOffset()
        {
            var region = WindowsWallpaperOcclusionDetector.GetEarthRegion(Monitors[1]);

            Assert.Equal(new ScreenRect(2560, 144, 3840, 1296), region);
        }

        [Fact]
        public void IsEarthRegionOccludedByWindow_ShouldReturnTrue_WhenWindowCoversSeventyPercent()
        {
            var earthRegion = WindowsWallpaperOcclusionDetector.GetEarthRegion(Monitors[0]);
            var coveredHeight = (int)Math.Ceiling(earthRegion.Height * 0.70);
            var window = new ScreenRect(
                earthRegion.Left,
                earthRegion.Top,
                earthRegion.Right,
                earthRegion.Top + coveredHeight);

            Assert.True(WindowsWallpaperOcclusionDetector.IsEarthRegionOccludedByWindow(Monitors[0], window));
        }

        [Fact]
        public void IsEarthRegionOccludedByWindow_ShouldReturnFalse_WhenWindowCoversLessThanSeventyPercent()
        {
            var earthRegion = WindowsWallpaperOcclusionDetector.GetEarthRegion(Monitors[0]);
            var coveredHeight = (int)Math.Floor(earthRegion.Height * 0.69);
            var window = new ScreenRect(
                earthRegion.Left,
                earthRegion.Top,
                earthRegion.Right,
                earthRegion.Top + coveredHeight);

            Assert.False(WindowsWallpaperOcclusionDetector.IsEarthRegionOccludedByWindow(Monitors[0], window));
        }

        [Fact]
        public void IsEarthRegionOccludedByWindow_ShouldReturnFalse_WhenWindowOnlyCoversCorner()
        {
            var window = new ScreenRect(0, 0, 300, 300);

            Assert.False(WindowsWallpaperOcclusionDetector.IsEarthRegionOccludedByWindow(Monitors[0], window));
        }

        [Fact]
        public void IsEarthRegionOccludedByWindow_ShouldOnlyMatchCoveredMonitor()
        {
            var secondEarthRegion = WindowsWallpaperOcclusionDetector.GetEarthRegion(Monitors[1]);

            Assert.False(WindowsWallpaperOcclusionDetector.IsEarthRegionOccludedByWindow(Monitors[0], secondEarthRegion));
            Assert.True(WindowsWallpaperOcclusionDetector.IsEarthRegionOccludedByWindow(Monitors[1], secondEarthRegion));
        }

        [Theory]
        [InlineData("Progman")]
        [InlineData("WorkerW")]
        [InlineData("SHELLDLL_DefView")]
        [InlineData("Shell_TrayWnd")]
        [InlineData("Shell_SecondaryTrayWnd")]
        public void IsExcludedShellWindowClass_ShouldExcludeDesktopShellWindows(string className)
        {
            Assert.True(WindowsWallpaperOcclusionDetector.IsExcludedShellWindowClass(className));
        }
    }
}
