using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public interface IBackgroundSetter
    {
        string Platform { get; }

        /// <summary>
        /// 设置单张静态壁纸
        /// </summary>
        Task SetBackgroundAsync(string filePath, CancellationToken token = default);

        /// <summary>
        /// 设置动态壁纸（图片序列循环播放）
        /// </summary>
        Task SetDynamicBackgroundAsync(IReadOnlyList<string> filePaths, int frameIntervalMs = 500, CancellationToken token = default)
        {
            // 默认实现：只设置第一张
            return filePaths.Count > 0 ? SetBackgroundAsync(filePaths[0], token) : Task.CompletedTask;
        }

        /// <summary>
        /// 停止动态壁纸播放
        /// </summary>
        void StopDynamicBackground() { }
    }
}
