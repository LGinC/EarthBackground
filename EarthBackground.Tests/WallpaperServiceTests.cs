using System;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Background;
using EarthBackground.Captors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EarthBackground.Tests
{
    public class WallpaperServiceTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<ILogger<WallpaperService>> _loggerMock;
        private readonly Mock<IOptionsMonitor<CaptureOption>> _optionsMock;
        private readonly Mock<IBackgroudSetProvider> _backgroundSetProviderMock;
        private readonly Mock<ICaptor> _captorMock;
        private readonly Mock<IOssDownloader> _downloaderMock;
        private readonly Mock<IBackgroundSetter> _setterMock;

        public WallpaperServiceTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _loggerMock = new Mock<ILogger<WallpaperService>>();
            _optionsMock = new Mock<IOptionsMonitor<CaptureOption>>();
            _backgroundSetProviderMock = new Mock<IBackgroudSetProvider>();
            _captorMock = new Mock<ICaptor>();
            _downloaderMock = new Mock<IOssDownloader>();
            _setterMock = new Mock<IBackgroundSetter>();

            // Setup Scope
            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);
            _scopeFactoryMock.Setup(x => x.CreateScope())
                .Returns(_scopeMock.Object);
            _scopeMock.Setup(x => x.ServiceProvider)
                .Returns(_serviceProviderMock.Object);

            // Setup Captor
            _captorMock.Setup(x => x.Downloader).Returns(_downloaderMock.Object);
            _captorMock.Setup(x => x.GetImagePath()).ReturnsAsync("test_image.jpg");

            // Setup Setter
            _backgroundSetProviderMock.Setup(x => x.GetSetter()).Returns(_setterMock.Object);

            // Setup Options
            _optionsMock.Setup(x => x.CurrentValue).Returns(new CaptureOption 
            { 
                Captor = "TestCaptor",
                Interval = 10,
                SetWallpaper = true,
                SaveWallpaper = false
            });
        }

        [Fact]
        public async Task Start_ShouldSetStatusToRunning()
        {
            // Arrange
            var service = new WallpaperService(
                _serviceProviderMock.Object,
                _loggerMock.Object,
                _optionsMock.Object,
                _backgroundSetProviderMock.Object);

            string? status = null;
            service.StatusChanged += (s) => status = s;

            // Act
            service.Start();

            // Assert
            Assert.True(service.IsRunning);
            Assert.Equal("Running", status);
        }

        [Fact]
        public void Stop_ShouldSetStatusToStopped()
        {
            // Arrange
            var service = new WallpaperService(
                _serviceProviderMock.Object,
                _loggerMock.Object,
                _optionsMock.Object,
                _backgroundSetProviderMock.Object);

            service.Start();
            string? status = null;
            service.StatusChanged += (s) => status = s;

            // Act
            service.Stop();

            // Assert
            Assert.False(service.IsRunning);
            Assert.Equal("Stopped", status);
        }

        // Note: Testing ExecuteAsync directly is tricky because it's protected and runs a loop.
        // We can test the logic if we extract RunCycleAsync to internal or public, 
        // or we can use reflection, or we can just trust the integration.
        // For unit testing, it's better to test small units.
        // However, since WallpaperService logic is mainly in RunCycleAsync, we might want to test that.
        // But RunCycleAsync is private.
        // I will stick to testing public API and maybe integration style if needed, 
        // but for now Start/Stop is good.
        // To test the cycle, I would need to refactor WallpaperService to be more testable 
        // (e.g. extract the cycle logic to a separate class or make method internal + InternalsVisibleTo).
        // Let's assume for now we just test Start/Stop and basic state.
    }
}
