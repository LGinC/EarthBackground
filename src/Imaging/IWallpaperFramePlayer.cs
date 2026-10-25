using System;
using Avalonia;
using Avalonia.Media.Imaging;

namespace EarthBackground.Imaging
{
    internal interface IWallpaperFramePlayer : IDisposable
    {
        int FrameCount { get; }
        PixelSize PixelSize { get; }
        FrameRenderResult RenderNextFrame(WriteableBitmap bitmap);
    }

    internal readonly record struct FrameRenderResult(int DelayMilliseconds, bool IsLastFrame, int FrameIndex);
}
