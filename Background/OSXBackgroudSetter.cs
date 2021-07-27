using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public class OSXBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.OSX);

        public Task SetBackgroundAsync(string filePath)
        {
            throw new PlatformNotSupportedException($"{Platform} not support");
        }
    }
}
