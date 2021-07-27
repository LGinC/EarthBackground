using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public class LinuxBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.Linux);

        public Task SetBackgroundAsync(string filePath)
        {
            throw new PlatformNotSupportedException($"{Platform} not support");
        }
    }
}
