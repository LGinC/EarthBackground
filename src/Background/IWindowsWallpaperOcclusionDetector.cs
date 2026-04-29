using System;
using System.Collections.Generic;

namespace EarthBackground.Background
{
    public interface IWindowsWallpaperOcclusionDetector
    {
        IReadOnlySet<string> GetOccludedMonitorIds(
            IReadOnlyList<WallpaperMonitor> monitors,
            IReadOnlySet<IntPtr> excludedWindowHandles);
    }
}
