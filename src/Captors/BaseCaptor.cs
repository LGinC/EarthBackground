using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        protected virtual TimeSpan SatelliteTimeZoneOffset => TimeSpan.Zero;
        protected virtual DateTimeOffset ClientLocalNow => DateTimeOffset.Now;

        protected string CurrentImageId { get; set; } = string.Empty;
        public virtual Task<string> GetImagePath(CancellationToken token = default)
        {
            throw new System.NotImplementedException();
        }

        public virtual async Task<IReadOnlyList<string>> GetImagePaths(int count = 20, Action<int, int>? onFrameComplete = null, CancellationToken token = default)
        {
            var path = await GetImagePath(token);
            onFrameComplete?.Invoke(1, 1);
            return new[] { path };
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

        protected Task SetImageId(CancellationToken token = default) => File.WriteAllTextAsync(NameConsts.ImageIdPath, CurrentImageId, token);

        protected string GetFrameImagePath(string imageId)
        {
            return Path.Combine(Options.SavePath, $"frame_{imageId}.png");
        }

        protected bool TryGetExistingFrameImagePath(string imageId, out string framePath)
        {
            framePath = GetFrameImagePath(imageId);
            if (!File.Exists(framePath))
            {
                return false;
            }

            if (IsExpectedFrameImageSize(framePath))
            {
                return true;
            }

            File.Delete(framePath);
            return false;
        }

        private bool IsExpectedFrameImageSize(string framePath)
        {
            var size = BaseRate * (1 << (int)Options.Resolution);
            if (Options.Zoom != 100)
            {
                size = (int)(size * Options.Zoom * 1.0 / 100);
            }

            var metadata = Image.Identify(framePath);
            return metadata != null && metadata.Width == size && metadata.Height == size;
        }

        protected async Task<IReadOnlyList<string>> BuildFrameSequenceAsync(
            IReadOnlyList<string> imageIds,
            Func<string, CancellationToken, Task<string>> frameFactory,
            Action<int, int>? onFrameComplete = null,
            CancellationToken token = default)
        {
            if (imageIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = new string[imageIds.Count];
            var completedCount = 0;
            var parallelism = GetFrameProcessingParallelism();
            using var semaphore = new SemaphoreSlim(parallelism, parallelism);
            using var batchCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            var batchToken = batchCancellationTokenSource.Token;
            var tasks = new Task[imageIds.Count];

            for (int i = 0; i < imageIds.Count; i++)
            {
                var index = i;
                var imageId = imageIds[index];
                tasks[index] = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(batchToken);
                    try
                    {
                        result[index] = await frameFactory(imageId, batchToken);
                        var done = Interlocked.Increment(ref completedCount);
                        onFrameComplete?.Invoke(done, imageIds.Count);
                    }
                    catch
                    {
                        batchCancellationTokenSource.Cancel();
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, batchToken);
            }

            await Task.WhenAll(tasks);
            return result;
        }

        protected virtual int GetFrameProcessingParallelism()
        {
            return Math.Clamp(Environment.ProcessorCount / 4, 2, 4);
        }

        protected string[] FilterImageIdsByClientLocalTime(IEnumerable<string> imageIds, int recentHours)
        {
            var ordered = imageIds
                .Select(id => (Raw: id, Time: ParseSatelliteTimestamp(id)))
                .OrderBy(static t => t.Time)
                .ToArray();

            if (ordered.Length == 0)
            {
                return Array.Empty<string>();
            }

            var cutoff = ordered[^1].Time.AddHours(-Math.Max(recentHours, 1));

            return ordered
                .Where(t => t.Time >= cutoff)
                .Select(t => t.Raw)
                .ToArray();
        }

        protected string[] ExpandAndFilterImageIdsByRecentAvailableTime(IEnumerable<string> imageIds, int recentHours)
        {
            var ordered = imageIds
                .Select(id => (Raw: id, Time: ParseSatelliteTimestamp(id)))
                .OrderBy(static t => t.Time)
                .ToArray();

            if (ordered.Length == 0)
            {
                return [];
            }

            var cutoff = ordered[^1].Time.AddHours(-Math.Max(recentHours, 1));
            var interval = TimeSpan.FromMinutes(NormalizeFrameIntervalMinutes(Options.FrameIntervalMinutes, recentHours));
            if (interval <= TimeSpan.Zero)
            {
                return ordered
                    .Where(t => t.Time >= cutoff)
                    .Select(t => t.Raw)
                    .ToArray();
            }

            var known = ordered
                .GroupBy(static t => t.Time)
                .ToDictionary(static g => g.Key, static g => g.First().Raw);
            var result = new List<string>();

            for (var timestamp = ordered[^1].Time; timestamp >= cutoff; timestamp = timestamp.Subtract(interval))
            {
                result.Add(known.TryGetValue(timestamp, out var raw)
                    ? raw
                    : FormatSatelliteTimestamp(timestamp));
            }

            result.Reverse();
            return result.ToArray();
        }

        protected DateTimeOffset ParseSatelliteTimestamp(string value)
        {
            var timestamp = DateTime.ParseExact(
                value,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            return new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified), SatelliteTimeZoneOffset);
        }

        protected string FormatSatelliteTimestamp(DateTimeOffset value)
        {
            return value.ToOffset(SatelliteTimeZoneOffset).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }

        protected static int NormalizeFrameIntervalMinutes(int value, int recentHours)
        {
            var maximum = Math.Min(360, Math.Max(1, recentHours) * 60);
            return Math.Clamp(value, 10, maximum);
        }

        
        protected void CleanupFramesOlderThan(string minImageId)
        {
            if (string.IsNullOrWhiteSpace(minImageId) || !Directory.Exists(Options.SavePath))
            {
                return;
            }

            if (!TryParseImageId(minImageId, out var minTimestamp))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(Options.SavePath, "frame_*.png"))
            {
                var imageId = Path.GetFileNameWithoutExtension(filePath)?["frame_".Length..];
                if (string.IsNullOrWhiteSpace(imageId))
                {
                    continue;
                }

                if (TryParseImageId(imageId, out var fileTimestamp) && fileTimestamp < minTimestamp)
                {
                    File.Delete(filePath);
                }
            }

            foreach (var dirPath in Directory.GetDirectories(Options.SavePath, "frame_*"))
            {
                ForceDeleteDirectory(dirPath);
            }
        }

        protected static void ForceDeleteDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(dirPath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            foreach (var childDirPath in Directory.GetDirectories(dirPath))
            {
                ForceDeleteDirectory(childDirPath);
            }

            File.SetAttributes(dirPath, FileAttributes.Normal);
            Directory.Delete(dirPath, recursive: false);
        }

        private static bool TryParseImageId(string imageId, out DateTime timestamp)
        {
            return DateTime.TryParseExact(
                imageId,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out timestamp);
        }

        public BaseCaptor(IOptionsSnapshot<CaptureOption> options,IHttpClientFactory factory, IOssProvider downloaderProvider)
        {
            Client = factory.CreateClient(ProviderName);
            Options = options.Value;
            Options.SavePath = AppPaths.ResolveInAppDirectory(Options.SavePath);
            Options.WallpaperFolder = AppPaths.ResolveInAppDirectory(Options.WallpaperFolder);
            ImagePath = Path.Combine(Options.SavePath, "wallpaper.png");
            Downloader = downloaderProvider.GetDownloader();
            CurrentImageId = !File.Exists(NameConsts.ImageIdPath) ? string.Empty : File.ReadLines(NameConsts.ImageIdPath).FirstOrDefault() ?? string.Empty;
        }

        protected virtual string JoinImage()
        {
            return JoinImageFromDir(Options.SavePath, ImagePath);
        }

        protected string JoinImageFromDir(string sourceDir, string outputPath)
        {
            var size = 1 << (int)Options.Resolution;
            using var image = new Image<Rgba32>(BaseRate * size, BaseRate * size);
            image.Mutate(context =>
            {
                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        using var tile = Image.Load<Rgba32>(Path.Combine(sourceDir, $"{i:000}_{j:000}.png"));
                        context.DrawImage(tile, new Point(BaseRate * j, BaseRate * i), 1f);
                    }
                }
            });

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (Options.Zoom == 100)
            {
                image.SaveAsPng(outputPath);
            }
            else
            {
                var newSize = (int)(image.Height * Options.Zoom * 1.0 / 100);
                using var zoomImage = image.Clone(context => context.Resize(newSize, newSize));
                zoomImage.SaveAsPng(outputPath);
            }

            foreach (var f in Directory.GetFiles(sourceDir).Where(f => f.Contains("_")))
            {
                File.Delete(f);
            }

            return outputPath;
        }

        public void Dispose()
        {
            Downloader.Dispose();
        }
    }
}
