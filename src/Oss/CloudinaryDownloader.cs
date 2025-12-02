using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EarthBackground.Captors;
using Microsoft.Extensions.Options;

namespace EarthBackground.Oss
{
    public class CloudinaryDownloader : IOssDownloader
    {
        public string ProviderName => NameConsts.Cloudinary;
        private readonly OssOption _option;
        private readonly HttpClient _client;

        public event Action<int> SetTotal;
        public event Action SetCurrentProgress;

        public CloudinaryDownloader(IOptionsSnapshot<OssOption> option, IHttpClientFactory httpClientFactory)
        {
            _option = option.Value;
            _client = httpClientFactory.CreateClient(ProviderName);
        }

        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory, CancellationToken token = default)
        {
            var valueTuples = images as (string url, string file)[] ?? images.ToArray();
            if (valueTuples.IsNullOrEmpty())
            {
                return [];
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
                var path = Path.Combine(directory, file);
                if (files.Contains(path))
                {
                    SetCurrentProgress?.Invoke();
                    continue;
                }
                tasks.Add(DownLoadImageAsync(url, path, token));
                result.Add((url, path));
            }

            await Task.WhenAll(tasks);
            return result;
        }

        private async Task DownLoadImageAsync(string url, string path, CancellationToken token = default)
        {
            await using var stream = await _client.GetStreamAsync(url, token);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            await using var fileStream = new FileStream(path, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream, token);
            SetCurrentProgress?.Invoke();
        }

        public async Task ClearOssAsync(string domain, CancellationToken token = default)
        {
            if (_client.DefaultRequestHeaders.Any(h => h.Key == "Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");
            }
            _client.BaseAddress = null;
            _client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_option.ApiKey}:{_option.ApiSecret}"))}");
            var response = await _client.DeleteAsync($"https://api.cloudinary.com/v1_1/{_option.UserName}/resources/image/fetch?prefix={domain}", token);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException();
            }

            var reStr = await response.Content.ReadAsStringAsync(token);
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
