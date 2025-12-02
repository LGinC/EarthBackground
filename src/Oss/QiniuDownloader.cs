using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace EarthBackground.Oss
{
    public class QiniuDownloader : IOssDownloader
    {
        public string ProviderName => NameConsts.Qiqiuyun;

        public event Action<int>? SetTotal;
        public event Action? SetCurrentProgress;
        private readonly IOptionsSnapshot<OssOption> _options;
        private readonly HttpClient _client;
        private readonly QiqiuAuth _auth;
        private const string Prefix = "eb_";

        public QiniuDownloader(IHttpClientFactory httpClientFactory, IOptionsSnapshot<OssOption> options)
        {
            _client = httpClientFactory.CreateClient(ProviderName);
            this._options = options;
            var mac = new Mac(options.Value.ApiKey, options.Value.ApiSecret);
            _auth = new QiqiuAuth(mac);
        }


        public  Task ClearOssAsync(string domain, CancellationToken token = default)
        {
            //IEnumerable<string> keys = await GetKeys();
            //await BatchDeleteAsync(keys);
            return Task.CompletedTask;
        }

        private async Task BatchDeleteAsync(IEnumerable<string> keys, CancellationToken token = default)
        {
            var keyArray = keys as string[] ?? keys.ToArray();
            if (keyArray.IsNullOrEmpty())
            {
                // Task.CompletedTask;
                return;
            }


            var bucket = _options.Value.Bucket;
            var ops = keyArray.Select(k => $"/delete/{QiniuBase64.UrlSafeBase64Encode($"{bucket}:{k}")}");
            // ReSharper disable once PossibleMultipleEnumeration
            var opsStr = string.Join('&', ops.Select(k => $"op={k}"));
            const string url = "https://rs.qiniu.com/batch";
            var data = Encoding.UTF8.GetBytes(opsStr);
            AddAuthorization(url, data);
            // ReSharper disable once PossibleMultipleEnumeration
            var content = new FormUrlEncodedContent(ops.Select(k => new KeyValuePair<string, string>("op", k)));
            var response = await _client.PostAsync(url, content, token);
            Trace.WriteLine(await response.Content.ReadAsStringAsync(token));
        }

        private  Task DeleteAsync(string key, CancellationToken token = default)
        {
            var url = $"https://rs.qbox.me/delete/{QiniuBase64.UrlSafeBase64Encode($"{_options.Value.Bucket}:{key}")}";
            AddAuthorization(url);
            return _client.PostAsync(url, new ByteArrayContent([]), token);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public void AddAuthorization(string url, byte[]? body = null)
        {
            if (_client.DefaultRequestHeaders.Any(c => c.Key == "Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");
            }

            var key = _auth.CreateManageToken(url, body ?? []);
            Trace.WriteLine(key);
            _client.DefaultRequestHeaders.Add("Authorization", key);
        }

        private async Task FetchAsync(IEnumerable<(string url, string path)> urls, CancellationToken token = default)
        {
            var baseurl = $"https://iovip-{_options.Value.Zone}.qbox.me/";
            foreach (var (url, path) in urls)
            {
                var urlBase64 = $"{baseurl}fetch/{QiniuBase64.UrlSafeBase64Encode(url)}/to/{QiniuBase64.UrlSafeBase64Encode(_options.Value.Bucket, path)}";
                AddAuthorization(urlBase64);
                var response = await _client.PostAsync(urlBase64, new ByteArrayContent(Array.Empty<byte>()), token);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidDataException(await response.Content.ReadAsStringAsync(token));
                }
            }
           
        }


        public async Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory, CancellationToken token = default)
        {
            var imageTuples = images as (string url, string file)[] ?? images.ToArray();
            if (imageTuples.IsNullOrEmpty())
            {
                return [];
            }

            await FetchAsync(imageTuples.Select(b => (b.url, $"{Prefix}{b.file}")), token);
            SetTotal?.Invoke(imageTuples.Length);
            var result = new List<(string, string)> { Capacity = imageTuples.Length };
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var files = Directory.GetFiles(directory);
            List<Task> tasks = new(imageTuples.Length);
            foreach (var (url, file) in imageTuples)
            {
                var path = Path.Combine(directory, file);
                if (files.Contains(path))
                {
                    SetCurrentProgress?.Invoke();
                    continue;
                }
                tasks.Add(DownLoadImageAsync($"{_options.Value.Domain}/{Prefix}{file}", path));
                result.Add((url, path));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
            //下载完后立即删除oss文件
            //await BatchDeleteAsync(images.Select(i => i.file));
            tasks.AddRange(imageTuples.Select(f => f.file).Select(item => DeleteAsync($"{Prefix}{item}", token)));
            await Task.WhenAll(tasks);
            return result;
        }



        private async Task DownLoadImageAsync(string url, string path)
        {
            await using var stream = await _client.GetStreamAsync(url);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            await using var fileStream = new FileStream(path, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
            SetCurrentProgress?.Invoke();
        }
    }
}
