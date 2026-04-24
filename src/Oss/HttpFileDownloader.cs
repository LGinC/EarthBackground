using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Oss
{
    internal static class HttpFileDownloader
    {
        private const int MaxDownloadAttempts = 3;

        public static async Task DownloadAsync(HttpClient client, string url, string path, CancellationToken token = default)
        {
            var tempPath = $"{path}.download";
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await DownloadOnceAsync(client, url, path, tempPath, token);
                    return;
                }
                catch (Exception) when (attempt < MaxDownloadAttempts && !token.IsCancellationRequested)
                {
                    DeleteIfExists(tempPath);
                    await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), token);
                }
            }
        }

        private static async Task DownloadOnceAsync(HttpClient client, string url, string path, string tempPath, CancellationToken token)
        {
            DeleteIfExists(tempPath);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            await using (var stream = await response.Content.ReadAsStreamAsync(token))
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream, token);
            }

            File.Move(tempPath, path, overwrite: true);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
