using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EarthBackground.Oss
{
    /// <summary>
    /// 直接下载
    /// </summary>
    public class DirectDownloader : IOssDownloader
    {
        public string ProviderName => "DirectDownload";
        private readonly HttpClient _client;

        public DirectDownloader(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            if(images.IsNullOrEmpty())
            {
                return new (string, string)[0];
            }

            var result = new List<(string, string)> { Capacity = images.Count() };
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var files = Directory.GetFiles(directory);
            foreach (var (url, file) in images)
            {
                if (files.Contains(file))
                {
                    continue;
                }

                string filePath = Path.Combine(directory, file);
                await DownLoadImageAsync(url, Path.Combine(directory, file));
                result.Add((url, filePath));
            }

            return result;
        }

        private async Task DownLoadImageAsync(string url, string path)
        {
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var message = $"{response.StatusCode}:图片下载失败 {url}\n{await response.Content.ReadAsStringAsync()}";
                throw new InvalidOperationException(message);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using var fileStream = new FileStream(path, FileMode.CreateNew);
            stream.CopyTo(fileStream);
        }

        public Task ClearOssAsync() => Task.CompletedTask;
    }
}
