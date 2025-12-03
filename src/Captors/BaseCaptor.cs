using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Captors
{
    public abstract class BaseCaptor: ICaptor
    {
        protected CaptureOption Options { get; set; } = null!;
        protected HttpClient Client { get; set; } = null!;
        protected int BaseRate { get; set; } = 688;
        public IOssDownloader Downloader { get; set; } = null!;
        public virtual string ProviderName { get; } = string.Empty;
        
        protected string CurrentImageId { get; set; } = string.Empty;
        public virtual Task<string> GetImagePath(CancellationToken token = default)
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

        public async Task ResetAsync(CancellationToken token = default)
        {
            if (Downloader != null && Client.BaseAddress != null)
            {
                await Downloader.ClearOssAsync(Client.BaseAddress.AbsoluteUri, token);
            }
        }

        protected string ImagePath { set; get; } = string.Empty;
        
        /// <summary>
        /// 持久化当前图片id
        /// </summary>
        /// <returns></returns>
        protected Task SetImageId(CancellationToken token = default) => File.WriteAllTextAsync(NameConsts.ImageIdPath, CurrentImageId, token);

        public BaseCaptor(IOptionsSnapshot<CaptureOption> options,IHttpClientFactory factory, IOssProvider downloaderProvider)
        {
            Client = factory.CreateClient(ProviderName);
            Options = options.Value;
            ImagePath = Path.Combine(Options.SavePath, "wallpaper.bmp");
            Downloader = downloaderProvider.GetDownloader();
            CurrentImageId = !File.Exists(NameConsts.ImageIdPath) ? string.Empty : File.ReadLines(NameConsts.ImageIdPath).FirstOrDefault() ?? string.Empty;
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