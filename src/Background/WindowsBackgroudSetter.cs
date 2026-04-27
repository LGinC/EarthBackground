using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;

namespace EarthBackground.Background
{
    [SupportedOSPlatform("windows")]
    public class WindowsBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.Windows);

        private static readonly string WallpaperBmpPath =
            Path.Combine(AppContext.BaseDirectory, "images", "wallpaper_static.bmp");

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                Wallpaper.Set(fileInfo.FullName);
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(WallpaperBmpPath)!);
            using var image = Image.Load(fileInfo.FullName);
            image.Save(WallpaperBmpPath, new BmpEncoder());
            Wallpaper.Set(WallpaperBmpPath);
            return Task.CompletedTask;
        }
    }
}
