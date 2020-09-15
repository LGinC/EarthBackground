using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public class WindowsBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.Windows);

        public Task SetBackgroudAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            //设置windows壁纸文件格式必须为bmp
            //using var image = Image.FromFile(filePath);
            //if (image.RawFormat.Guid != ImageFormat.Bmp.Guid)
            if (fileInfo.Extension != ".bmp" && fileInfo.Extension != ".BMP")
            {
                throw new InvalidDataException($"windows壁纸文件格式必须为.bmp,而传入格式为{fileInfo.Extension}");
            }
            Wallpaper.Set(fileInfo.FullName);
            return Task.CompletedTask;
        }
    }
}
