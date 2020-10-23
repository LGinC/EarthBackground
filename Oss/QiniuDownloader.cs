using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly QiqiuAuth auth;
        private readonly Mac mac;
        private const string prifix = "eb_";

        public QiniuDownloader(IHttpClientFactory httpClientFactory, IOptionsSnapshot<OssOption> options)
        {
            client = httpClientFactory.CreateClient(ProviderName);
            this.options = options;
            mac = new Mac(options.Value.ApiKey, options.Value.ApiSecret);
            auth = new QiqiuAuth(mac);
        }


        public async Task ClearOssAsync(string domain)
        {
            IEnumerable<string> keys = await GetKeys();
            await BatchDeleteAsync(keys);
        }

        private async Task BatchDeleteAsync(IEnumerable<string> keys)
        {
            if (keys.IsNullOrEmpty())
            {
                // Task.CompletedTask;
                return;
            }


            string bucket = options.Value.Bucket;
            var ops = keys.Select(k => $"/delete/{QiniuBase64.UrlSafeBase64Encode($"{bucket}:{k}")}");
            string opsStr = string.Join('&', ops.Select(k => $"op={k}"));
            string url = $"https://rs.qiniu.com/batch";
            var data = Encoding.UTF8.GetBytes(opsStr);
            AddAuthorization(url, data);
            var content = new FormUrlEncodedContent(ops.Select(k => new KeyValuePair<string, string>("op", k)));
            var response = await client.PostAsync(url, new FormUrlEncodedContent(new [] { new KeyValuePair<string,string>("", opsStr) }));
            Trace.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private  Task DeleteAsync(string key)
        {
            string url = $"https://rs-{options.Value.Zone}.qiniu.com/delete/{QiniuBase64.UrlSafeBase64Encode($"{options.Value.Bucket}:{key}")}";
            AddAuthorization(url);
            return client.PostAsync(url, new ByteArrayContent(Array.Empty<byte>()));
        }

        private async Task<IEnumerable<string>> GetKeys()
        {
            client.DefaultRequestHeaders.Host = "rsf.qbox.me";
            string url = $"/list?bucket={options.Value.Bucket}&prefix=0";
            AddAuthorization(url, null);
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

        public void AddAuthorization(string url, byte[] body = null)
        {
            if (client.DefaultRequestHeaders.Any(c => c.Key == "Authorization"))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
            }

            string key = auth.CreateManageToken(url, body);
            Trace.WriteLine(key);
            client.DefaultRequestHeaders.Add("Authorization", key);
        }

        private async Task FetchAsync(IEnumerable<(string url, string path)> urls)
        {
            var baseurl = $"https://iovip-{options.Value.Zone}.qbox.me/";
            foreach ((string url, string path) in urls)
            {
                var urlBase64 = $"{baseurl}fetch/{QiniuBase64.UrlSafeBase64Encode(url)}/to/{QiniuBase64.UrlSafeBase64Encode(options.Value.Bucket, path)}";
                var data = Encoding.UTF8.GetBytes(urlBase64);
                AddAuthorization(urlBase64);
                var response = await client.PostAsync(urlBase64, new ByteArrayContent(Array.Empty<byte>()));
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidDataException(await response.Content.ReadAsStringAsync());
                }
            }
           
        }


        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            if (images.IsNullOrEmpty())
            {
                return null;
            }

            await FetchAsync(images.Select(i => (i.url, $"{prifix}{i.file}")));

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

                await DownLoadImageAsync($"{options.Value.Domain}/{prifix}{file}", path);
                result.Add((url, path));
                SetCurrentProgress(++i);
            }

            //下载完后立即删除oss文件
            //await BatchDeleteAsync(images.Select(i => i.file));
            //foreach (var item in images.Select(i => i.file))
            //{
            //    await DeleteAsync($"{prifix}{item}");
            //}
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
