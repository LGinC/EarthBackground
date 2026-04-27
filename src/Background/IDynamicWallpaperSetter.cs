using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EarthBackground.Background
{
    public interface IDynamicWallpaperSetter
    {
        string Platform { get; }

        Task SetDynamicBackgroundAsync(
            IReadOnlyList<string> filePaths,
            int frameIntervalMs = 500,
            Action<int, int>? onProgress = null,
            CancellationToken token = default);

        void StopDynamicBackground();
    }
}
