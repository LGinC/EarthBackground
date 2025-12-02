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
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.Windows));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.Linux));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return setters.FirstOrDefault(s => s.Platform == nameof(OSPlatform.OSX));
            }

            return null;
        }
    }
}
