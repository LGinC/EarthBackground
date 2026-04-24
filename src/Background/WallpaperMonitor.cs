namespace EarthBackground.Background
{
    public readonly record struct WallpaperMonitor(
        string Id,
        string DisplayName,
        int X,
        int Y,
        int Width,
        int Height);
}
