using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthBackground.Background
{
    internal static class WallpaperMonitorSelection
    {
        public static IReadOnlyList<WallpaperMonitor> SelectTargetMonitors(
            IReadOnlyList<WallpaperMonitor> monitors,
            IReadOnlyList<string>? selectedMonitorIds)
        {
            if (selectedMonitorIds == null || selectedMonitorIds.Count == 0)
            {
                return monitors;
            }

            var selectedIds = new HashSet<string>(selectedMonitorIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
            if (selectedIds.Count == 0)
            {
                return monitors;
            }

            var selectedMonitors = monitors
                .Where(monitor => selectedIds.Contains(monitor.Id))
                .ToArray();

            return selectedMonitors.Length > 0 ? selectedMonitors : monitors;
        }
    }
}
