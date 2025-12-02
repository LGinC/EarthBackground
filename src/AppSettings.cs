using EarthBackground.Oss;

namespace EarthBackground
{
    public class AppSettings
    {
        public CaptureOption CaptureOptions { get; set; } = new();

        public OssOption OssOptions { get; set; } = new();
    }
}
