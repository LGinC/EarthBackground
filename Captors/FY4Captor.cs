using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EarthBackground.Oss;
using Microsoft.Extensions.Options;

namespace EarthBackground.Captors
{
    /// <summary>
    /// 风云4A
    /// </summary>
    public class FY4Captor: BaseCaptor
    {
        private const string url =
            // "http://rsapp.nsmc.org.cn/swapQuery/public/tileServer/getTile/fy-4a/reg_china/NatureColor/20211109010000/jpg/3/{i}/{j}.png";
            "http://rsapp.nsmc.org.cn/swapQuery/public/tileServer/getTile/fy-4a/full_disk/NatureColor/{0}/jpg/{1}/{2}/{3}.png";


        public override string ProviderName => NameConsts.FY4;

        private async Task<string> GetImageIdAsync()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"start", ""},
                {"end", ""},
                {"sat", "fy-4a"},
                {"obsType", "full_disk"},
                {"interval", "1"},
                {"duration", "5"},
                {"intervalCell", "10"},
                {"queryProduct", "NatureColor"}
            });
            var re = await Client.PostAsync("/swapQuery/public/DataQuery/playList", content);
            return (await re.Content.ReadFromJsonAsync<string[]>())?.First();
        }
        public override async Task<string> GetImagePath()
        {
            CreateDirectory();
            var imageId = await GetImageIdAsync();
            if (string.IsNullOrEmpty(imageId) || imageId == CurrentImageId)
            {
                return ImagePath;
            }
            CurrentImageId = imageId;
            await SaveImageAsync();
            var wallpaper = JoinImage();
            await SetImageId();
            return wallpaper;
        }
        private async Task SaveImageAsync()
        {
            if (CurrentImageId == null)
            {
                throw new InvalidOperationException("未获取到imageId");
            }
            
            var size = (int)Options.Resolution;
            int total = 1 << size;
            List<(string, string)> images = new List<(string, string)>();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    string image = $"{i:000}_{j:000}.png";
                    string filePath = Path.Combine(Options.SavePath, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add((string.Format(url, CurrentImageId, size, i, j), image));
                    }
                }
            }

            if (images.Count == 0)
            {
                return;
            }

            await Downloader.DownloadAsync(images, Options.SavePath);
        }



        public FY4Captor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider) : base(options, factory, downloaderProvider)
        {
            Client.BaseAddress = new Uri("http://rsapp.nsmc.org.cn");
            BaseRate = 687;
        }
    }
}