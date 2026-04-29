using EarthBackground.Views;
using Xunit;

namespace EarthBackground.Tests
{
    public class WallpaperPlaybackWindowTests
    {
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
