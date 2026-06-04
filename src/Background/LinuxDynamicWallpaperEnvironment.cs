using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthBackground.Background
{
    internal static class LinuxDynamicWallpaperEnvironment
    {
        internal readonly record struct SessionInfo(
            bool IsWsl,
            string? DesktopSession,
            string? XdgCurrentDesktop,
            string? XdgSessionDesktop);

        public static SessionInfo ReadCurrentSession()
        {
            return new SessionInfo(
                IsWsl(),
                Environment.GetEnvironmentVariable("DESKTOP_SESSION"),
                Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"),
                Environment.GetEnvironmentVariable("XDG_SESSION_DESKTOP"));
        }

        public static bool ShouldBlockDynamicWallpaper(
            IReadOnlyList<WallpaperMonitor> monitors,
            SessionInfo session)
        {
            if (monitors.Count == 0)
            {
                return false;
            }

            if (monitors.Any(static monitor => IsRemoteDesktopMonitor(monitor.Id, monitor.DisplayName)))
            {
                return true;
            }

            return ContainsXrdp(session.DesktopSession) ||
                   ContainsXrdp(session.XdgCurrentDesktop) ||
                   ContainsXrdp(session.XdgSessionDesktop) ||
                   session.IsWsl;
        }

        private static bool IsWsl()
        {
            var distro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
            if (!string.IsNullOrWhiteSpace(distro))
            {
                return true;
            }

            var interop = Environment.GetEnvironmentVariable("WSL_INTEROP");
            return !string.IsNullOrWhiteSpace(interop);
        }

        private static bool ContainsXrdp(string? value)
            => !string.IsNullOrWhiteSpace(value) &&
               value.Contains("xrdp", StringComparison.OrdinalIgnoreCase);

        private static bool IsRemoteDesktopMonitor(string id, string displayName)
            => ContainsRdpToken(id) || ContainsRdpToken(displayName);

        private static bool ContainsRdpToken(string? value)
            => !string.IsNullOrWhiteSpace(value) &&
               value.Contains("rdp", StringComparison.OrdinalIgnoreCase);
    }
}
