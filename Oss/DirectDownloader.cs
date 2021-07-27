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
        public event Action SetCurrentProgress;

        public DirectDownloader(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient(NameConsts.DirectDownload);
        }

        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            var valueTuples = images as (string url, string file)[] ?? images.ToArray();
            if (valueTuples.IsNullOrEmpty())
            {
                return new (string, string)[0];
            }

            SetTotal?.Invoke(valueTuples.Length);
            var result = new List<(string, string)> { Capacity = valueTuples.Length };
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var files = Directory.GetFiles(directory);
            List<Task> tasks = new(valueTuples.Length);
            foreach (var (url, file) in valueTuples)
            {
                string path = Path.Combine(directory, file);
                if (files.Contains(path))
                {
                    SetCurrentProgress?.Invoke();
                    continue;
                }

                tasks.Add(DownLoadImageAsync(url, path));
                result.Add((url, path));
            }

            await Task.WhenAll(tasks);
            // SetCurrentProgress?.Invoke(result.Count);
            return result;
        }

        private async Task DownLoadImageAsync(string url,string path)
        {
            await using var stream = await  _client.GetStreamAsync(url);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            await using var fileStream = new FileStream(path, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
            SetCurrentProgress?.Invoke();
        }

        public Task ClearOssAsync(string domain) => Task.CompletedTask;

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
