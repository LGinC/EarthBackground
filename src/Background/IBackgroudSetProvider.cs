using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace EarthBackground.Background
{
    public interface IBackgroudSetProvider
    {
        IBackgroundSetter GetSetter();
    }

    public class BackgroudSetProvider : IBackgroudSetProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public BackgroudSetProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IBackgroundSetter GetSetter()
        {
            var setters = _serviceProvider.GetServices<IBackgroundSetter>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.Windows))
                    ?? throw new PlatformNotSupportedException("No Windows wallpaper setter is registered.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.Linux))
                    ?? throw new PlatformNotSupportedException("No Linux wallpaper setter is registered.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.OSX))
                    ?? throw new PlatformNotSupportedException("No macOS wallpaper setter is registered.");
            }

            throw new PlatformNotSupportedException("Current platform is not supported.");
        }
    }
}
