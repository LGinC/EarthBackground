using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// 获取图片路径（单张，兼容旧接口）
        /// </summary>
        /// <returns></returns>
        Task<string> GetImagePath(CancellationToken token = default);

        /// <summary>
        /// 获取最近多张图片路径列表（用于动态壁纸）
        /// </summary>
        /// <param name="count">获取数量，默认20</param>
        /// <param name="onFrameComplete">每完成一帧时回调 (completedFrames, totalFrames)</param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IReadOnlyList<string>> GetImagePaths(int count = 20, Action<int, int>? onFrameComplete = null, CancellationToken token = default);

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
        Task ResetAsync(CancellationToken token = default);
    }
}
