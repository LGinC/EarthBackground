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
    public class QiniuDownloader : IOssDownloader
    {
        public string ProviderName => NameConsts.Qiqiuyun;

        public event Action<int> SetTotal;
        public event Action<int> SetCurrentProgress;
        private readonly IOptionsSnapshot<OssOption> options;
        private readonly HttpClient client;
        private readonly Mac mac;

        public QiniuDownloader(IHttpClientFactory httpClientFactory, IOptionsSnapshot<OssOption> options)
        {
            client = httpClientFactory.CreateClient(ProviderName);
            this.options = options;
            mac = new Mac(options.Value.ApiKey, options.Value.ApiSecret);
        }


        public async Task ClearOssAsync(string domain)
        {
            IEnumerable<string> keys = await GetKeys();
            await DeleteAsync(keys);
        }

        private  Task DeleteAsync(IEnumerable<string> keys)
        {
            if (keys.IsNullOrEmpty())
            {
                return Task.CompletedTask;
            }

            client.DefaultRequestHeaders.Host = "rs.qbox.me";
            if (!client.DefaultRequestHeaders.Any(h => h.Key == "Content-Type" && h.Value.Contains("application/x-www-form-urlencoded")))
            {
                client.DefaultRequestHeaders.Remove("Content-Type");
                client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            }

            string bucket = options.Value.Bucket;
            string body = string.Join('&', keys.Select(k => $"op=/delete/{QiniuBase64.UrlSafeBase64Encode($"{bucket}:{k}")}"));
            AddAuthorization("/batch", Encoding.UTF8.GetBytes(body));
            return client.PostAsync("/batch", new StringContent(body));
        }

        private async Task<IEnumerable<string>> GetKeys()
        {
            client.DefaultRequestHeaders.Host = "rsf.qbox.me";
            string url = $"/list?bucket={options.Value.Bucket}&prefix=0";
            AddAuthorization(url, null);
            if (!client.DefaultRequestHeaders.Any(h => h.Key == "Content-Type" && h.Value.Contains("application/x-www-form-urlencoded")))
            {
                client.DefaultRequestHeaders.Remove("Content-Type");
                client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            }

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var str = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(str))
                {
                    return null;
                }
                var result = JsonSerializer.Deserialize<QiniuFileResult>(str);
                return result.items?.Select(i => i.key);
            }
            return null;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public void AddAuthorization(string url, byte[] body)
        {
            if(client.DefaultRequestHeaders.Any(c => c.Key == "Authorization"))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
            }

            client.DefaultRequestHeaders.Add("Authorization", body.IsNullOrEmpty() ? QiqiuAuth.CreateManageToken(mac, url) :  QiqiuAuth.CreateManageToken(mac, url, body));
        }

        private async Task SetFetchUrlAsync(string url)
        {
            client.DefaultRequestHeaders.Host = "uc.qbox.me";
            if(!client.DefaultRequestHeaders.Any(h => h.Key == "Content-Type" && h.Value.Contains("application/x-www-form-urlencoded")))
            {
                client.DefaultRequestHeaders.Remove("Content-Type");
                client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            }
            await client.PostAsync($"/image/{options.Value.Bucket}/from/{QiniuBase64.UrlSafeBase64Encode(Encoding.UTF8.GetBytes(url))}", new StringContent(string.Empty));
        }

        public async  Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            if (images.IsNullOrEmpty())
            {
                return null;
            }
            await SetFetchUrlAsync(images.First().url.Replace(images.First().file, string.Empty));

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

                await DownLoadImageAsync($"{options.Value.Domain}/{file}", path);
                result.Add((url, path));
                SetCurrentProgress(++i);
            }

            //下载完后立即删除oss文件
            await DeleteAsync(images.Select(i => i.file));
            return result;
        }

      

        private async Task DownLoadImageAsync(string url, string path)
        {
            var response = await client.GetAsync(url);
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
    }
}
