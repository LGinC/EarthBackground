using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;

namespace EarthBackground
{
    public class Himawari8Captor : ICaptor
    {
        const int baseRate = 688;
        //const string jsonUrl = "https://himawari8-dl.nict.go.jp/himawari8/img/FULL_24h/latest.json";
        public string ProviderName => NameConsts.Himawari8;
        
        private readonly CaptureOption _option;
        private readonly IConfigureSaver _saver;

        private readonly string path;

        private readonly HttpClient _client;



        public Himawari8Captor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IConfigureSaver saver, IOssProvider downloaderProvider)
        {
            _option = options.Value;
            _saver = saver;
            _client = factory.CreateClient(ProviderName);
            Downloader = downloaderProvider.GetDownloader();
            path = Path.Combine(_option.SavePath, "wallpaper.bmp");
            GetImageId();
        }


        public CaptureOption Option => _option;

        public IOssDownloader Downloader { get; set; }
        private string _imageId;

        /// <summary>
        /// 获取图片id
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetImageIdAsync()
        {
            //var response = await _client.GetAsync(_option.ImageIdUrl);
            //if (!response.IsSuccessStatusCode)
            //{
            //    throw new InvalidOperationException($"{response.StatusCode.ToString()}:{response.Content}");
            //}
            ////20200915005000

            //var reStr = await response.Content.ReadAsStringAsync();
            //var json = JsonSerializer.Deserialize<DateResult>(reStr);
            //return json.date.ToString("yyyy/MM/dd/hhmmss");
            var t = await _client.GetAsync(!string.IsNullOrEmpty(_option.ImageIdUrl) ? _option.ImageIdUrl : "json/himawari/full_disk/geocolor/latest_times.json");
            var str = await t.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LastestTimes>(str).timestamps_int.First().ToString();
        }

        /// <summary>
        /// 持久化图片id
        /// </summary>
        /// <returns></returns>
        Task SetImageId()
        {
            return File.WriteAllTextAsync("imageId.txt", _imageId);
        }

        /// <summary>
        /// 读取图片id
        /// </summary>
        /// <returns></returns>
        string GetImageId()
        {
            CheckFile();
            _imageId = File.ReadLines("imageId.txt").FirstOrDefault();
            return _imageId;
        }

        void CheckFile()
        {
            if (!File.Exists("imageId.txt"))
                File.Create("imageId.txt");
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <returns></returns>
        private async Task SaveImageAsync(string imageId)
        {
            var size = (int)_option.Resolution;
            int total = 1 << size;
            List<(string, string)> images = new List<(string, string)>();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    string image = $"{i:000}_{j:000}.png";
                    string filePath = Path.Combine(_option.SavePath, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add(($"{_client.BaseAddress.AbsoluteUri}imagery/{imageId[0..8]}/himawari---full_disk/geocolor/{imageId}/{size:00}/{image}", image));
                    }
                }
            }

            if (images.Count == 0)
            {
                return;
            }

            var result = await Downloader.DownloadAsync(images, _option.SavePath);
        }


        /// <summary>
        /// 拼接图片
        /// </summary>
        /// <returns></returns>
        private string JoinImage()
        {
            var size = 1 << (int)_option.Resolution;
            using Bitmap bitmap = new Bitmap(baseRate * size, baseRate * size);
            Image[,] tile = new Image[size, size];
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        tile[i, j] = Image.FromFile(Path.Combine(_option.SavePath, $"{i}_{j}.png"));
                        g.DrawImage(tile[i, j], baseRate * j, baseRate * i);
                        tile[i, j].Dispose();
                    }
                }
                g.Save();
            }



            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (_option.Zoom == 100)
            {
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            else
            {
                int new_size = (int)(bitmap.Height * _option.Zoom * 1.0 / 100);
                using Bitmap zoom_bitmap = new Bitmap(new_size, new_size);
                using Graphics g_2 = Graphics.FromImage(zoom_bitmap);
                g_2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g_2.DrawImage(bitmap, 0, 0, new_size, new_size);
                g_2.Save();
                zoom_bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
            }

            //删除小文件
            foreach (var f in Directory.GetFiles(_option.SavePath).Where(f => f.Contains("_")))
            {
                File.Delete(f);
            }

            return path;
        }

        private void CreateDirectory()
        {
            if (!Directory.Exists(_option.SavePath))
            {
                Directory.CreateDirectory(_option.SavePath);
            }
        }

        public async Task<string> GetImagePath()
        {
            CreateDirectory();
            var imageId = await GetImageIdAsync();
            if (string.IsNullOrEmpty(imageId) || imageId == _imageId)
            {
                return path;
            }
            await SaveImageAsync(imageId);
            var wallpaper = JoinImage();
            _imageId = imageId;
            await SetImageId();
            return wallpaper;
        }

        public async Task ResetAsync()
        {
            if (Downloader != null)
            {
                await Downloader.ClearOssAsync(_client.BaseAddress.AbsoluteUri);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            Downloader.Dispose();
        }
    }

    public class LastestTimes
    {
        public long[] timestamps_int { get; set; }
    }

    public class DateResult
    {
        [JsonConverter(typeof(DateConverter))]
        public DateTime date { get; set; }
    }

    public class CDNOperationResult
    {
        public string error { get; set; }
    }

    public class DateConverter : JsonConverter<DateTime>
    {
        private readonly string formatString;

        public DateConverter()
        {
            formatString = "yyyy-MM-dd hh:mm:ss";
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Console.WriteLine(reader.GetString());
            return DateTime.ParseExact(reader.GetString(), formatString, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(formatString));
        }
    }
}
