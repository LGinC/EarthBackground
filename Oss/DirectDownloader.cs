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
        public string ProviderName => NameConsts.DirectDownload;
        private readonly HttpClient _client;

        public event Action<int> SetTotal;
        public event Action<int> SetCurrentProgress;

        public DirectDownloader(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient(NameConsts.DirectDownload);
        }

        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            if (images.IsNullOrEmpty())
            {
                return new (string, string)[0];
            }

            SetTotal(images.Count());
            var result = new List<(string, string)> { Capacity = images.Count() };
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var files = Directory.GetFiles(directory);
            int i = 0;
            foreach (var (url, file) in images)
            {
                string path = Path.Combine(directory, file);
                if (files.Contains(path))
                {
                    SetCurrentProgress(++i);
                    continue;
                }

                await DownLoadImageAsync(url, path);
                result.Add((url, path));
                SetCurrentProgress(++i);
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

        public Task ClearOssAsync(string domain) => Task.CompletedTask;

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
