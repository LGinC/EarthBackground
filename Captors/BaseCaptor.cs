using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;

namespace EarthBackground.Captors
{
    public abstract class BaseCaptor: ICaptor
    {
        protected CaptureOption Options { get; set; }
        protected HttpClient Client { get; set; }
        protected int BaseRate { get; set; } = 688;
        public IOssDownloader Downloader { get; set; }
        public virtual string ProviderName { get; }
        
        protected  string CurrentImageId { get; set; }
        public virtual Task<string> GetImagePath()
        {
            throw new System.NotImplementedException();
        }
        
        protected void CreateDirectory()
        {
            if (!Directory.Exists(Options.SavePath))
            {
                Directory.CreateDirectory(Options.SavePath);
            }
        }

        public async Task ResetAsync()
        {
            if (Downloader != null)
            {
                await Downloader.ClearOssAsync(Client.BaseAddress.AbsoluteUri);
            }
        }

        protected string ImagePath { set; get; }
        
        /// <summary>
        /// 持久化当前图片id
        /// </summary>
        /// <returns></returns>
        protected Task SetImageId()
        {
            return File.WriteAllTextAsync("imageId.txt", CurrentImageId);
        }

        public BaseCaptor(IOptionsSnapshot<CaptureOption> options,IHttpClientFactory factory, IOssProvider downloaderProvider)
        {
            Client = factory.CreateClient(ProviderName);
            Options = options.Value;
            ImagePath = Path.Combine(Options.SavePath, "wallpaper.bmp");
            Downloader = downloaderProvider.GetDownloader();
            CurrentImageId = !File.Exists("imageId.txt") ? null : File.ReadLines("imageId.txt").FirstOrDefault();
        }
        
        /// <summary>
        /// 拼接图片
        /// </summary>
        /// <returns></returns>
        protected virtual string JoinImage()
        {
            var size = 1 << (int)Options.Resolution;
            using Bitmap bitmap = new Bitmap(BaseRate * size, BaseRate * size);
            Image[,] images = new Image[size, size];
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        images[i, j] = Image.FromFile(Path.Combine(Options.SavePath, $"{i:000}_{j:000}.png"));
                        g.DrawImage(images[i, j], BaseRate * j, BaseRate * i);
                        images[i, j].Dispose();
                    }
                }
                g.Save();
            }
            if (File.Exists(ImagePath))
            {
                File.Delete(ImagePath);
            }

            if (Options.Zoom == 100)
            {
                bitmap.Save(ImagePath, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            else
            {
                var newSize = (int)(bitmap.Height * Options.Zoom * 1.0 / 100);
                using var zoomBitmap = new Bitmap(newSize, newSize);
                using var g2 = Graphics.FromImage(zoomBitmap);
                g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g2.DrawImage(bitmap, 0, 0, newSize, newSize);
                g2.Save();
                zoomBitmap.Save(ImagePath, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            //删除小文件
            foreach (var f in Directory.GetFiles(Options.SavePath).Where(f => f.Contains("_")))
            {
                File.Delete(f);
            }

            return ImagePath;
        }

        public void Dispose()
        {
            Client.Dispose();
            Downloader.Dispose();
        }
    }
}