using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace EarthBackground.Background
{
    [SupportedOSPlatform("windows")]
    public class WindowsBackgroudSetter : IBackgroundSetter
    {
        public string Platform => nameof(OSPlatform.Windows);

        private static readonly string WallpaperBmpPath =
            Path.Combine(AppContext.BaseDirectory, "images", "wallpaper_static.bmp");

        public Task SetBackgroundAsync(string filePath, CancellationToken token = default)
        {
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                Wallpaper.Set(fileInfo.FullName);
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(WallpaperBmpPath)!);
            using (var input = File.OpenRead(fileInfo.FullName))
            using (var output = File.Create(WallpaperBmpPath))
                ConvertToBmp(input, output);
            Wallpaper.Set(WallpaperBmpPath);
            return Task.CompletedTask;
        }

        internal static void ConvertToBmp(Stream input, Stream output)
        {
            using var codec = SKCodec.Create(input) ?? throw new InvalidDataException("Cannot decode wallpaper image.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            var rowBytes = checked(info.Width * 4);
            var imageSize = checked(rowBytes * info.Height);
            var fileSize = checked(54 + imageSize);
            using var image = new SKBitmap(info);
            var result = codec.GetPixels(info, image.GetPixels(), image.RowBytes, SKCodecOptions.Default);
            if (result != SKCodecResult.Success)
                throw new InvalidDataException($"Cannot decode wallpaper image: {result}.");

            Span<byte> header = stackalloc byte[54];
            header.Clear();
            header[0] = (byte)'B';
            header[1] = (byte)'M';
            BinaryPrimitives.WriteInt32LittleEndian(header[2..], fileSize);
            BinaryPrimitives.WriteInt32LittleEndian(header[10..], 54);
            BinaryPrimitives.WriteInt32LittleEndian(header[14..], 40);
            BinaryPrimitives.WriteInt32LittleEndian(header[18..], info.Width);
            BinaryPrimitives.WriteInt32LittleEndian(header[22..], -info.Height);
            BinaryPrimitives.WriteInt16LittleEndian(header[26..], 1);
            BinaryPrimitives.WriteInt16LittleEndian(header[28..], 32);
            BinaryPrimitives.WriteInt32LittleEndian(header[34..], imageSize);
            output.Write(header);

            var pixels = image.GetPixelSpan();
            for (var y = 0; y < info.Height; y++)
            {
                var row = pixels.Slice(y * image.RowBytes, rowBytes);
                // BI_RGB ignores alpha, matching the previous non-transparent BMP encoder.
                for (var x = 3; x < row.Length; x += 4)
                    row[x] = 0;
                output.Write(row);
            }
        }
    }
}
