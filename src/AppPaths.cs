using System;
using System.IO;

namespace EarthBackground
{
    internal static class AppPaths
    {
        private static readonly string ExecutableDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        private static readonly bool IsMacBundle = OperatingSystem.IsMacOS()
            && Path.GetFileName(ExecutableDirectory) == "MacOS"
            && Path.GetFileName(Path.GetDirectoryName(ExecutableDirectory)) == "Contents";

        public static string BaseDirectory { get; } = IsMacBundle
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EarthBackground")
            : AppContext.BaseDirectory;

        public static string ResourceDirectory { get; } = IsMacBundle
            ? Path.GetFullPath(Path.Combine(ExecutableDirectory, "..", "Resources"))
            : AppContext.BaseDirectory;

        public static string AppSettingsPath { get; } = Path.Combine(BaseDirectory, "appsettings.json");

        public static string ImageIdPath => Path.Combine(BaseDirectory, "ImageId.txt");

        public static string DefaultImagesDirectory { get; } = Path.Combine(BaseDirectory, "images");

        static AppPaths()
        {
            if (!IsMacBundle)
                return;

            Directory.CreateDirectory(BaseDirectory);
            var defaults = Path.Combine(ResourceDirectory, "appsettings.json");
            if (!File.Exists(AppSettingsPath) && File.Exists(defaults))
                File.Copy(defaults, AppSettingsPath);
        }

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
