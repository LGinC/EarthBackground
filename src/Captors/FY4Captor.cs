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

        /// <summary>
        /// 获取最近N个图片时间戳列表
        /// </summary>
        private async Task<string[]> GetImageIdsAsync(int count = 20, CancellationToken token = default)
        {
            // duration=200 约可覆盖200帧，intervalCell=10分钟，取最近count张
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"start", ""},
                {"end", ""},
                {"sat", NameConsts.Fy4},
                {"obsType", "full_disk"},
                {"interval", "1"},
                {"duration", Math.Max(count * 2, 40).ToString()},
                {"intervalCell", "10"},
                {"queryProduct", "NatureColor_NoLit"}
            });
            var re = await Client.PostAsync("/swapQuery/public/DataQuery/playList", content, token);
            var ids = await re.Content.ReadFromJsonAsync<string[]>(cancellationToken: token);
            if (ids == null) return Array.Empty<string>();
            return ids.Take(count).ToArray();
        }

        public override async Task<string> GetImagePath(CancellationToken token = default)
        {
            var paths = await GetImagePaths(1, null, token);
            return paths.Count > 0 ? paths[0] : ImagePath;
        }

        public override async Task<IReadOnlyList<string>> GetImagePaths(int count = 20, Action<int, int>? onFrameComplete = null, CancellationToken token = default)
        {
            CreateDirectory();
            var imageIds = await GetImageIdsAsync(count, token);
            if (imageIds.Length == 0) return Array.Empty<string>();

            var latestId = imageIds[0];
            if (latestId == CurrentImageId && Directory.GetFiles(Options.SavePath, "frame_*.bmp").Length >= imageIds.Length)
            {
                var existing = GetExistingFramePaths(imageIds);
                onFrameComplete?.Invoke(existing.Count, existing.Count);
                return existing;
            }

            CurrentImageId = latestId;
            var result = new List<string>();
            var total = imageIds.Length;

            for (int fi = 0; fi < total; fi++)
            {
                var imageId = imageIds[fi];
                var frameDir = Path.Combine(Options.SavePath, $"frame_{imageId}");
                if (!Directory.Exists(frameDir)) Directory.CreateDirectory(frameDir);

                await SaveImageAsync(imageId, frameDir, token);
                var framePath = JoinImageToPath(frameDir, imageId);
                result.Add(framePath);
                onFrameComplete?.Invoke(fi + 1, total);
            }

            await SetImageId(token);
            if (result.Count > 0) ImagePath = result[0];
            return result;
        }

        private IReadOnlyList<string> GetExistingFramePaths(string[] imageIds)
        {
            var result = new List<string>();
            foreach (var imageId in imageIds)
            {
                var framePath = Path.Combine(Options.SavePath, $"frame_{imageId}.bmp");
                if (File.Exists(framePath)) result.Add(framePath);
            }
            return result;
        }

        private string JoinImageToPath(string frameDir, string imageId)
        {
            var outputPath = Path.Combine(Options.SavePath, $"frame_{imageId}.bmp");
            if (File.Exists(outputPath)) return outputPath;
            return JoinImageFromDir(frameDir, outputPath);
        }

        private async Task SaveImageAsync(string imageId, string saveDir, CancellationToken token = default)
        {
            var size = (int)Options.Resolution;
            var total = 1 << size;
            var images = new List<(string, string)>();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    var image = $"{i:000}_{j:000}.png";
                    var filePath = Path.Combine(saveDir, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add((string.Format(Url, imageId, size, i, j), image));
                    }
                }
            }

            if (images.Count == 0) return;
            await Downloader.DownloadAsync(images, saveDir, token);
        }

        public FY4Captor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider) : base(options, factory, downloaderProvider)
        {
            Client.BaseAddress = new Uri("http://rsapp.nsmc.org.cn");
            BaseRate = 687;
        }
    }
}