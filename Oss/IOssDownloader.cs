using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EarthBackground
{
    public interface IOssDownloader : IDisposable
    {
        /// <summary>
        /// 提供器名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 下载图片到指定目录
        /// </summary>
        /// <param name="images">需要下载的图片列表</param>
        /// <param name="directory">图片保存目录</param>
        /// <returns>下载结果</returns>
        Task<IEnumerable<(string url, string path)>> DownloadAsync(IEnumerable<(string url, string file)> images, string directory);

        /// <summary>
        /// 清除oss存储
        /// </summary>
        /// <returns></returns>
        Task ClearOssAsync();

        /// <summary>
        /// 设置下载总数
        /// </summary>
        event Action<int> SetTotal;

        /// <summary>
        /// 设置当前下载进度
        /// </summary>
        event Action<int> SetCurrentProgress;
    }
}
