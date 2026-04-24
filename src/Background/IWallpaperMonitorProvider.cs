using System.Collections.Generic;

namespace EarthBackground.Background
{
    public interface IWallpaperMonitorProvider
    {
        IReadOnlyList<WallpaperMonitor> GetMonitors();
    }
}
