using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;

namespace EarthBackground.Captors
{
    public class Himawari8Captor : BaseCaptor
    {
        //const string jsonUrl = "https://himawari8-dl.nict.go.jp/himawari8/img/FULL_24h/latest.json";
        public override string ProviderName => NameConsts.Himawari8;
        public Himawari8Captor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider) : base(options, factory, downloaderProvider)
        {
        }
        

        /// <summary>
        /// 获取图片id
        /// </summary>
        /// <returns></returns>
        private async Task<string?> GetImageIdAsync(CancellationToken token=default)
        {
            //var response = await Client.GetAsync(Options.ImageIdUrl);
            //if (!response.IsSuccessStatusCode)
            //{
            //    throw new InvalidOperationException($"{response.StatusCode.ToString()}:{response.Content}");
            //}
            ////20200915005000

            //var reStr = await response.Content.ReadAsStringAsync();
            //var json = JsonSerializer.Deserialize<DateResult>(reStr);
            //return json.date.ToString("yyyy/MM/dd/hhmmss");
            
            return (await Client.GetFromJsonAsync<LastestTimes>(!string.IsNullOrEmpty(Options.ImageIdUrl) ? Options.ImageIdUrl : "json/himawari/full_disk/geocolor/latest_times.json", cancellationToken: token))?.timestamps_int.First().ToString();
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <returns></returns>
        private async Task SaveImageAsync(string imageId, CancellationToken token = default)
        {
            var size = (int)Options.Resolution;
            int total = 1 << size;
            List<(string, string)> images = new();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    var image = $"{i:000}_{j:000}.png";
                    var filePath = Path.Combine(Options.SavePath, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add(($"{Client.BaseAddress?.AbsoluteUri}imagery/{imageId[..8]}/himawari---full_disk/geocolor/{imageId}/{size:00}/{image}", image));
                    }
                }
            }

            if (images.Count == 0)
            {
                return;
            }

            await Downloader.DownloadAsync(images, Options.SavePath, token);
        }
        
        
        public override async Task<string> GetImagePath(CancellationToken token = default)
        {
            CreateDirectory();
            var imageId = await GetImageIdAsync(token);
            if (string.IsNullOrEmpty(imageId) || imageId == CurrentImageId)
            {
                return ImagePath;
            }
            CurrentImageId = imageId;
            await SaveImageAsync(imageId, token);
            var wallpaper = JoinImage();
            await SetImageId(token);
            return wallpaper;
        }
    }

    public class LastestTimes
    {
        public long[] timestamps_int { get; set; } = Array.Empty<long>();
    }

    public class DateResult
    {
        [JsonConverter(typeof(DateConverter))]
        public DateTime date { get; set; }
    }

    public class CDNOperationResult
    {
        public string error { get; set; } = string.Empty;
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
            var dateString = reader.GetString();
            Console.WriteLine(dateString);
            return DateTime.ParseExact(dateString ?? string.Empty, formatString, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString(formatString));
        }
    }
}
