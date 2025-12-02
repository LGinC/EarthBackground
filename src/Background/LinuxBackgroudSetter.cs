using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public class LinuxBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.Linux);

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            throw new PlatformNotSupportedException($"{Platform} not support");
        }
    }
}
