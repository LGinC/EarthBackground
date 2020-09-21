using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EarthBackground.Oss
{
    public class QiniuDownloader : IOssDownloader
    {
        public string ProviderName => NameConsts.Qiqiuyun;

        public event Action<int> SetTotal;
        public event Action<int> SetCurrentProgress;

        public Task ClearOssAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<(string url, string path)> DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            
            return null;
        }

        Task<IEnumerable<(string url, string path)>> IOssDownloader.DownloadAsync(IEnumerable<(string url, string file)> images, string directory)
        {
            SetTotal(0);
            SetCurrentProgress(0);
            throw new NotImplementedException();
        }
    }
}
