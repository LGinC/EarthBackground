using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace EarthBackground.Oss
{
    public class CloudinaryDownloader : IOssDownloader
    {
        public string ProviderName => NameConsts.Cloudinary;
        private readonly OssOption _option;
        private readonly HttpClient _client;

        public event Action<int> SetTotal;
        public event Action<int> SetCurrentProgress;

        public CloudinaryDownloader(IOptionsSnapshot<OssOption> option, IHttpClientFactory httpClientFactory)
        {
            _option = option.Value;
            _client = httpClientFactory.CreateClient(ProviderName);
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

            int i = 0;
            foreach (var (url, file) in images)
            {
                string filePath = Path.Combine(directory, file);
                await DownLoadImageAsync(url, Path.Combine(directory, file));
                result.Add((url, filePath));
                SetCurrentProgress(++i);
            }

            return result;
        }

        private async Task DownLoadImageAsync(string url, string path)
        {
            var response = await _client.GetAsync($"{_client.BaseAddress}{url}");
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

        public async Task ClearOssAsync()
        {
            if (_client.DefaultRequestHeaders.Any(h => h.Key == "Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");
            }
            _client.BaseAddress = null;
            _client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_option.ApiKey}:{_option.ApiSecret}"))}");
            var response = await _client.DeleteAsync($"https://api.cloudinary.com/v1_1/{_option.UserName}/resources/image/fetch?prefix=http://himawari8-dl");
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException();
            }

            var reStr = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<CDNOperationResult>(reStr);
            if (!string.IsNullOrEmpty(json.error))
            {
                throw new InvalidOperationException(json.error);
            }

            _client.DefaultRequestHeaders.Remove("Authorization");
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
