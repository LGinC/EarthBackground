using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthBackground
{
    public interface ICaptor : IDisposable
    {
        IOssDownloader Downloader { get; set; }
        /// <summary>
        /// 提供器名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 获取图片路径
        /// </summary>
        /// <returns></returns>
        Task<string> GetImagePath();

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="downloader">下载器</param>
        void SetDownloader(IOssDownloader downloader)
        {
            Downloader = downloader;
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        Task ResetAsync();
    }
}
