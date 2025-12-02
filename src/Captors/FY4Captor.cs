using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
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
        private const string Url =
            // "http://rsapp.nsmc.org.cn/swapQuery/public/tileServer/getTile/fy-4b/reg_china/NatureColor/20211109010000/jpg/3/{i}/{j}.png";
            $$"""http://rsapp.nsmc.org.cn/swapQuery/public/tileServer/getTile/{{NameConsts.Fy4}}/full_disk/NatureColor_NoLit/{0}/jpg/{1}/{2}/{3}.png""";


        public override string ProviderName => NameConsts.Fy4;

        private async Task<string?> GetImageIdAsync(CancellationToken token = default)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"start", ""},
                {"end", ""},
                {"sat", NameConsts.Fy4},
                {"obsType", "full_disk"},
                {"interval", "1"},
                {"duration", "5"},
                {"intervalCell", "10"},
                {"queryProduct", "NatureColor_NoLit"}
            });
            var re = await Client.PostAsync("/swapQuery/public/DataQuery/playList", content, token);
            return (await re.Content.ReadFromJsonAsync<string[]>(cancellationToken: token))?.FirstOrDefault();
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
            await SaveImageAsync(token);
            var wallpaper = JoinImage();
            await SetImageId(token);
            return wallpaper;
        }
        private async Task SaveImageAsync(CancellationToken token = default)
        {
            if (CurrentImageId == null)
            {
                throw new InvalidOperationException("未获取到imageId");
            }
            
            var size = (int)Options.Resolution;
            var total = 1 << size;
            var images = new List<(string, string)>();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    var image = $"{i:000}_{j:000}.png";
                    var filePath = Path.Combine(Options.SavePath, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add((string.Format(Url, CurrentImageId, size, i, j), image));
                    }
                }
            }

            if (images.Count == 0)
            {
                return;
            }

            await Downloader.DownloadAsync(images, Options.SavePath, token);
        }



        public FY4Captor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider) : base(options, factory, downloaderProvider)
        {
            Client.BaseAddress = new Uri("http://rsapp.nsmc.org.cn");
            BaseRate = 687;
        }
    }
}