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
    public class HimawariCaptor : BaseCaptor
    {
        //const string jsonUrl = "https://himawari8-dl.nict.go.jp/himawari8/img/FULL_24h/latest.json";
        public override string ProviderName => NameConsts.Himawari;
        public HimawariCaptor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider) : base(options, factory, downloaderProvider)
        {
        }

        /// <summary>
        /// 根据返回时间戳获取最近一天的图片时间戳列表
        /// </summary>
        private async Task<string[]> GetImageIdsAsync(int recentHours = 24, CancellationToken token = default)
        {
            var latest = await Client.GetFromJsonAsync<LastestTimes>(
                !string.IsNullOrEmpty(Options.ImageIdUrl) ? Options.ImageIdUrl : "json/himawari/full_disk/geocolor/latest_times.json",
                cancellationToken: token);
            if (latest == null) return Array.Empty<string>();

            var ordered = latest.timestamps_int
                .Select(static t => (Raw: t, Time: ParseTimestamp(t.ToString())))
                .OrderByDescending(static t => t.Time)
                .ToArray();

            if (ordered.Length == 0)
            {
                return Array.Empty<string>();
            }

            var newest = ordered[0].Time;
            var cutoff = newest.AddHours(-Math.Max(recentHours, 1));

            return ordered
                .Where(t => t.Time >= cutoff)
                .OrderBy(t => t.Time)
                .Select(t => t.Raw.ToString())
                .ToArray();
        }

        /// <summary>
        /// 保存指定时间戳的图片到子目录
        /// </summary>
        private async Task SaveImageAsync(string imageId, string saveDir, CancellationToken token = default)
        {
            var size = (int)Options.Resolution;
            int total = 1 << size;
            List<(string, string)> images = new();
            for (int i = 0; i < total; i++)
            {
                for (int j = 0; j < total; j++)
                {
                    var image = $"{i:000}_{j:000}.png";
                    var filePath = Path.Combine(saveDir, image);
                    if (!File.Exists(filePath))
                    {
                        images.Add(($"{Client.BaseAddress?.AbsoluteUri}imagery/{FormatImageDatePath(imageId)}/himawari-9---full_disk/geocolor/{imageId}/{size:00}/{image}", image));
                    }
                }
            }

            if (images.Count == 0) return;
            await Downloader.DownloadAsync(images, saveDir, token);
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

            // 最新的id用于判断是否有更新
            var latestId = imageIds[0];
            if (latestId == CurrentImageId && Directory.GetFiles(Options.SavePath, "frame_*.png").Length >= imageIds.Length)
            {
                // 无更新，返回已有帧
                var existing = GetExistingFramePaths(imageIds);
                onFrameComplete?.Invoke(existing.Count, existing.Count);
                return existing;
            }

            CurrentImageId = latestId;
            var result = await BuildFrameSequenceAsync(imageIds, GetOrCreateFrameAsync, onFrameComplete, token);

            await SetImageId(token);
            CleanupFramesOlderThan(imageIds[0]);
            if (result.Count > 0) ImagePath = result[0];
            return result;
        }

        private IReadOnlyList<string> GetExistingFramePaths(string[] imageIds)
        {
            var result = new List<string>();
            foreach (var imageId in imageIds)
            {
                if (TryGetExistingFrameImagePath(imageId, out var framePath)) result.Add(framePath);
            }
            return result;
        }

        /// <summary>
        /// 将指定目录的分块图片拼接为单张png，返回路径
        /// </summary>
        private string JoinImageToPath(string frameDir, string imageId)
        {
            var outputPath = GetFrameImagePath(imageId);
            if (File.Exists(outputPath)) return outputPath;
            return JoinImageFromDir(frameDir, outputPath);
        }

        private async Task<string> GetOrCreateFrameAsync(string imageId, CancellationToken token)
        {
            if (TryGetExistingFrameImagePath(imageId, out var existingFramePath))
            {
                return existingFramePath;
            }

            var frameDir = Path.Combine(Options.SavePath, $"frame_{imageId}");
            if (!Directory.Exists(frameDir))
            {
                Directory.CreateDirectory(frameDir);
            }

            try
            {
                await SaveImageAsync(imageId, frameDir, token);
                return JoinImageToPath(frameDir, imageId);
            }
            catch
            {
                if (!File.Exists(GetFrameImagePath(imageId)))
                {
                    ForceDeleteDirectory(frameDir);
                }

                throw;
            }
        }

        private static DateTime ParseTimestamp(string value)
        {
            return DateTime.ParseExact(value, "yyyyMMddHHmmss", null);
        }

        private static string FormatImageDatePath(string imageId)
        {
            return ParseTimestamp(imageId).ToString("yyyy/MM/dd");
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
