using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public class WindowsBackgroudSetter : IBackgroundSetter
    {
        public string Platform => throw new NotImplementedException();

        public Task SetBackgroudAsync(string filePath)
        {
            //设置windows壁纸文件格式必须为bmp
            if(Image.FromFile(filePath).RawFormat != ImageFormat.Bmp)
            {
                throw new InvalidDataException("windows壁纸文件格式必须为bmp");
            }
            Wallpaper.Set(filePath);
            return Task.CompletedTask;
        }
    }
}
