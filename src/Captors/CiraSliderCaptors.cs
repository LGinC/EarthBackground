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
    public abstract class CiraSliderCaptor : BaseCaptor
    {
        protected abstract string JsonSatelliteName { get; }
        protected abstract string ImagerySatelliteName { get; }
        protected virtual string Sector => "full_disk";
        protected virtual string Product => "geocolor";

        protected CiraSliderCaptor(
            IOptionsSnapshot<CaptureOption> options,
            IHttpClientFactory factory,
            IOssProvider downloaderProvider)
            : base(options, factory, downloaderProvider)
        {
        }

        private async Task<string[]> GetImageIdsAsync(int recentHours = 24, CancellationToken token = default)
        {
            var latest = await Client.GetFromJsonAsync<LastestTimes>(
                $"json/{JsonSatelliteName}/{Sector}/{Product}/latest_times.json",
                cancellationToken: token);
            if (latest == null) return [];

            return FilterImageIdsByClientLocalTime(
                latest.Timestamps.Select(static t => t.ToString()),
                recentHours);
        }

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
                        images.Add((BuildTileUrl(imageId, size, image), image));
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
            if (imageIds.Length == 0) return [];

            var latestId = imageIds[^1];
            if (latestId == CurrentImageId && Directory.GetFiles(Options.SavePath, "frame_*.png").Length >= imageIds.Length)
            {
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

        private string BuildTileUrl(string imageId, int size, string image)
        {
            return $"{Client.BaseAddress?.AbsoluteUri}imagery/{FormatImageDatePath(imageId)}/{ImagerySatelliteName}---{Sector}/{Product}/{imageId}/{size:00}/{image}";
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

    public class HimawariCaptor : CiraSliderCaptor
    {
        public override string ProviderName => NameConsts.Himawari;
        protected override string JsonSatelliteName => "himawari";
        protected override string ImagerySatelliteName => "himawari";

        public HimawariCaptor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider)
            : base(options, factory, downloaderProvider)
        {
        }
    }

    public class GoesCaptor : CiraSliderCaptor
    {
        public override string ProviderName => NameConsts.Goes;
        protected override string JsonSatelliteName => "goes-19";
        protected override string ImagerySatelliteName => "goes-19";

        public GoesCaptor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider)
            : base(options, factory, downloaderProvider)
        {
            BaseRate = 678;
        }
    }

    public class GeoKompsatCaptor : CiraSliderCaptor
    {
        public override string ProviderName => NameConsts.GeoKompsat;
        protected override string JsonSatelliteName => "gk2a";
        protected override string ImagerySatelliteName => "gk2a";

        public GeoKompsatCaptor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider)
            : base(options, factory, downloaderProvider)
        {
        }
    }

    public class MeteosatCaptor : CiraSliderCaptor
    {
        public override string ProviderName => NameConsts.Meteosat;
        protected override string JsonSatelliteName => "meteosat-12";
        protected override string ImagerySatelliteName => "meteosat-12";

        public MeteosatCaptor(IOptionsSnapshot<CaptureOption> options, IHttpClientFactory factory, IOssProvider downloaderProvider)
            : base(options, factory, downloaderProvider)
        {
        }
    }

    public class LastestTimes
    {
        [JsonPropertyName("timestamps_int")]
        public long[] Timestamps { get; set; } = [];
    }

    public class DateResult
    {
        [JsonPropertyName("date")]
        [JsonConverter(typeof(DateConverter))]
        public DateTime Date { get; set; }
    }

    public class CDNOperationResult
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
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
