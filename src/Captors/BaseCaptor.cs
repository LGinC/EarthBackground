using EarthBackground.Oss;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
            return File.Exists(framePath);
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
                null,
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
            using Bitmap bitmap = new Bitmap(BaseRate * size, BaseRate * size);
            Bitmap[,] images = new Bitmap[size, size];
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        images[i, j] = new Bitmap(Path.Combine(sourceDir, $"{i:000}_{j:000}.png"));
                        g.DrawImage(images[i, j], BaseRate * j, BaseRate * i);
                        images[i, j].Dispose();
                    }
                }
                g.Save();
            }
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (Options.Zoom == 100)
            {
                bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            else
            {
                var newSize = (int)(bitmap.Height * Options.Zoom * 1.0 / 100);
                using var zoomBitmap = new Bitmap(newSize, newSize);
                using var g2 = Graphics.FromImage(zoomBitmap);
                g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g2.DrawImage(bitmap, 0, 0, newSize, newSize);
                g2.Save();
                zoomBitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
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
