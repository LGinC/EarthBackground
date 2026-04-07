using System;
using System.IO;

namespace EarthBackground
{
    internal static class AppPaths
    {
        public static string BaseDirectory => AppContext.BaseDirectory;

        public static string AppSettingsPath => Path.Combine(BaseDirectory, "appsettings.json");

        public static string ImageIdPath => Path.Combine(BaseDirectory, "ImageId.txt");

        public static string DefaultImagesDirectory => Path.Combine(BaseDirectory, "images");

        public static string ResolveInAppDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return DefaultImagesDirectory;
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(BaseDirectory, path);
        }
    }
}
