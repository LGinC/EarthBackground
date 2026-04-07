using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EarthBackground.Imaging
{
    internal static class ApngParser
    {
        public static ApngStreamPlayer Open(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return ApngStreamPlayer.Create(filePath, stream);
        }
    }

    internal sealed class ApngStreamPlayer : IWallpaperFramePlayer
    {
        private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] IendChunk = ApngChunkCodec.CreateChunkBytes("IEND", ReadOnlySpan<byte>.Empty);

        private readonly List<byte[]> _sharedChunks;
        private readonly List<ApngFrameDescriptor> _frames;
        private readonly byte[] _compositedPixels;
        private readonly int _canvasWidth;
        private readonly int _canvasHeight;
        private readonly FileStream _fileStream;
        private int _currentFrameIndex = -1;

        private ApngStreamPlayer(
            string filePath,
            int canvasWidth,
            int canvasHeight,
            List<byte[]> sharedChunks,
            List<ApngFrameDescriptor> frames)
        {
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _sharedChunks = sharedChunks;
            _frames = frames;
            _compositedPixels = new byte[checked(canvasWidth * canvasHeight * 4)];
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
        }

        public int FrameCount => _frames.Count;

        public PixelSize PixelSize => new(_canvasWidth, _canvasHeight);

        public FrameRenderResult RenderNextFrame(WriteableBitmap bitmap)
        {
            if (_frames.Count == 0)
            {
                return new FrameRenderResult(100, true, 0);
            }

            _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Count;
            if (_currentFrameIndex == 0)
            {
                Array.Clear(_compositedPixels, 0, _compositedPixels.Length);
            }

            var frame = _frames[_currentFrameIndex];
            using var frameStream = new FramePngReadStream(_fileStream, frame, _sharedChunks);
            using var frameImage = SixLabors.ImageSharp.Image.Load<Rgba32>(frameStream);
            ComposeFrame(frameImage, frame);
            CopyPixelsToBitmap(bitmap);

            return new FrameRenderResult(frame.DelayMilliseconds, _currentFrameIndex == _frames.Count - 1, _currentFrameIndex);
        }

        public static ApngStreamPlayer Create(string filePath, Stream stream)
        {
            Span<byte> signature = stackalloc byte[PngSignature.Length];
            ReadExactly(stream, signature);
            if (!signature.SequenceEqual(PngSignature))
            {
                throw new InvalidDataException("输入文件不是有效的 PNG/APNG。");
            }

            byte[]? ihdrData = null;
            var sharedChunks = new List<byte[]>();
            var frames = new List<ApngFrameDescriptor>();
            ApngFrameBuilder? currentFrame = null;
            var hasAnimationControl = false;

            while (TryReadChunk(stream, out var chunk))
            {
                switch (chunk.Type)
                {
                    case "IHDR":
                        ihdrData = chunk.Data;
                        break;
                    case "acTL":
                        hasAnimationControl = true;
                        break;
                    case "fcTL":
                        if (ihdrData == null)
                        {
                            throw new InvalidDataException("APNG 缺少 IHDR。");
                        }

                        if (currentFrame != null)
                        {
                            frames.Add(currentFrame.Build());
                        }

                        currentFrame = ApngFrameBuilder.Create(chunk.Data, ihdrData);
                        break;
                    case "IDAT":
                        currentFrame ??= ApngFrameBuilder.CreateDefault(ihdrData ?? throw new InvalidDataException("PNG 缺少 IHDR。"));
                        currentFrame.DataChunks.Add(FrameDataChunk.FromIdat(chunk.DataOffset, chunk.DataLength, chunk.Crc));
                        break;
                    case "fdAT":
                        if (currentFrame == null)
                        {
                            throw new InvalidDataException("fdAT 在 fcTL 之前出现。");
                        }

                        currentFrame.DataChunks.Add(FrameDataChunk.FromFrameData(chunk.DataOffset + 4, chunk.Data.AsSpan(4)));
                        break;
                    case "IEND":
                        if (currentFrame != null)
                        {
                            frames.Add(currentFrame.Build());
                            currentFrame = null;
                        }
                        break;
                    default:
                        if (!hasAnimationControl && chunk.Type == "IDAT")
                        {
                            break;
                        }

                        if (!IsAnimationSpecificChunk(chunk.Type) && currentFrame == null)
                        {
                            sharedChunks.Add(chunk.EncodedChunk);
                        }
                        break;
                }

                if (chunk.Type == "IEND")
                {
                    break;
                }
            }

            if (ihdrData == null)
            {
                throw new InvalidDataException("PNG 缺少 IHDR。");
            }

            if (frames.Count == 0)
            {
                throw new InvalidDataException("APNG 未包含任何帧。");
            }

            return new ApngStreamPlayer(
                filePath,
                ApngChunkCodec.ReadInt32BigEndian(ihdrData, 0),
                ApngChunkCodec.ReadInt32BigEndian(ihdrData, 4),
                sharedChunks,
                frames);
        }

        private void ComposeFrame(SixLabors.ImageSharp.Image<Rgba32> frameImage, ApngFrameDescriptor frame)
        {
            if (frame.DisposeOp != 0)
            {
                throw new NotSupportedException($"暂不支持 DisposalOp={frame.DisposeOp} 的 APNG。");
            }

            frameImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var targetRow = (frame.YOffset + y) * _canvasWidth * 4 + (frame.XOffset * 4);
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var source = row[x];
                        var targetIndex = targetRow + (x * 4);

                        if (frame.BlendOp == 0)
                        {
                            _compositedPixels[targetIndex] = source.R;
                            _compositedPixels[targetIndex + 1] = source.G;
                            _compositedPixels[targetIndex + 2] = source.B;
                            _compositedPixels[targetIndex + 3] = source.A;
                            continue;
                        }

                        AlphaBlendOver(targetIndex, source);
                    }
                }
            });
        }

        private void AlphaBlendOver(int targetIndex, Rgba32 source)
        {
            if (source.A == 0)
            {
                return;
            }

            if (source.A == byte.MaxValue)
            {
                _compositedPixels[targetIndex] = source.R;
                _compositedPixels[targetIndex + 1] = source.G;
                _compositedPixels[targetIndex + 2] = source.B;
                _compositedPixels[targetIndex + 3] = source.A;
                return;
            }

            var dstR = _compositedPixels[targetIndex];
            var dstG = _compositedPixels[targetIndex + 1];
            var dstB = _compositedPixels[targetIndex + 2];
            var dstA = _compositedPixels[targetIndex + 3];

            var srcA = source.A;
            var invSrcA = 255 - srcA;
            var outA = srcA + ((dstA * invSrcA + 127) / 255);

            if (outA == 0)
            {
                _compositedPixels[targetIndex] = 0;
                _compositedPixels[targetIndex + 1] = 0;
                _compositedPixels[targetIndex + 2] = 0;
                _compositedPixels[targetIndex + 3] = 0;
                return;
            }

            _compositedPixels[targetIndex] = BlendChannel(source.R, srcA, dstR, dstA, invSrcA, outA);
            _compositedPixels[targetIndex + 1] = BlendChannel(source.G, srcA, dstG, dstA, invSrcA, outA);
            _compositedPixels[targetIndex + 2] = BlendChannel(source.B, srcA, dstB, dstA, invSrcA, outA);
            _compositedPixels[targetIndex + 3] = (byte)outA;
        }

        private static byte BlendChannel(byte src, int srcA, byte dst, byte dstA, int invSrcA, int outA)
        {
            var srcTerm = src * srcA * 255;
            var dstTerm = dst * dstA * invSrcA;
            return (byte)((srcTerm + dstTerm + (outA * 127)) / (outA * 255));
        }

        private void CopyPixelsToBitmap(WriteableBitmap bitmap)
        {
            using var framebuffer = bitmap.Lock();
            var sourceRowBytes = _canvasWidth * 4;
            for (int y = 0; y < _canvasHeight; y++)
            {
                Marshal.Copy(
                    _compositedPixels,
                    y * sourceRowBytes,
                    framebuffer.Address + (y * framebuffer.RowBytes),
                    sourceRowBytes);
            }
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        private static bool TryReadChunk(Stream stream, out PngChunkReadResult chunk)
        {
            Span<byte> header = stackalloc byte[8];
            var bytesRead = stream.Read(header);
            if (bytesRead == 0)
            {
                chunk = default;
                return false;
            }

            if (bytesRead != header.Length)
            {
                throw new EndOfStreamException();
            }

            var length = BinaryPrimitives.ReadInt32BigEndian(header[..4]);
            var type = System.Text.Encoding.ASCII.GetString(header[4..8]);
            var data = new byte[length];
            ReadExactly(stream, data);
            Span<byte> crcBytes = stackalloc byte[4];
            ReadExactly(stream, crcBytes);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(crcBytes);

            var encodedChunk = new byte[header.Length + length + crcBytes.Length];
            header.CopyTo(encodedChunk.AsSpan(0, header.Length));
            data.CopyTo(encodedChunk.AsSpan(header.Length, length));
            crcBytes.CopyTo(encodedChunk.AsSpan(header.Length + length, crcBytes.Length));

            chunk = new PngChunkReadResult(type, data, stream.Position - length - 4, length, crc, encodedChunk);
            return true;
        }

        private static void ReadExactly(Stream stream, Span<byte> buffer)
        {
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = stream.Read(buffer[totalRead..]);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                totalRead += read;
            }
        }

        private static bool IsAnimationSpecificChunk(string chunkType) => chunkType is "acTL" or "fcTL" or "fdAT";

        private sealed class FramePngReadStream : Stream
        {
            private readonly FileStream _sourceStream;
            private readonly ApngFrameDescriptor _frame;
            private readonly IReadOnlyList<byte[]> _sharedChunks;
            private int _stage;
            private int _sharedChunkIndex;
            private int _frameChunkIndex;
            private int _segmentOffset;
            private long _remainingFileBytes;
            private byte[]? _currentSegment;
            private byte[]? _currentFrameChunkPrefix;
            private byte[]? _currentFrameChunkSuffix;

            public FramePngReadStream(FileStream sourceStream, ApngFrameDescriptor frame, IReadOnlyList<byte[]> sharedChunks)
            {
                _sourceStream = sourceStream;
                _frame = frame;
                _sharedChunks = sharedChunks;
                _sourceStream.Position = 0;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer.AsSpan(offset, count));
            }

            public override int Read(Span<byte> destination)
            {
                var totalRead = 0;
                while (!destination.IsEmpty)
                {
                    if (_remainingFileBytes > 0)
                    {
                        var bytesToRead = (int)Math.Min(destination.Length, _remainingFileBytes);
                        var read = _sourceStream.Read(destination[..bytesToRead]);
                        if (read <= 0)
                        {
                            throw new EndOfStreamException();
                        }

                        totalRead += read;
                        destination = destination[read..];
                        _remainingFileBytes -= read;
                        continue;
                    }

                    var segment = GetOrAdvanceSegment();
                    if (segment == null)
                    {
                        break;
                    }

                    var segmentSpan = segment.AsSpan(_segmentOffset);
                    var bytesToCopy = Math.Min(segmentSpan.Length, destination.Length);
                    segmentSpan[..bytesToCopy].CopyTo(destination);
                    totalRead += bytesToCopy;
                    destination = destination[bytesToCopy..];
                    _segmentOffset += bytesToCopy;
                }

                return totalRead;
            }

            public override int ReadByte()
            {
                Span<byte> buffer = stackalloc byte[1];
                return Read(buffer) == 1 ? buffer[0] : -1;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            private byte[]? GetOrAdvanceSegment()
            {
                while (true)
                {
                    if (_currentSegment != null && _segmentOffset < _currentSegment.Length)
                    {
                        return _currentSegment;
                    }

                    _currentSegment = null;
                    _segmentOffset = 0;

                    switch (_stage)
                    {
                        case 0:
                            _currentSegment = PngSignature;
                            _stage = 1;
                            return _currentSegment;
                        case 1:
                            _currentSegment = _frame.IhdrChunk;
                            _stage = 2;
                            return _currentSegment;
                        case 2:
                            if (_sharedChunkIndex < _sharedChunks.Count)
                            {
                                _currentSegment = _sharedChunks[_sharedChunkIndex++];
                                return _currentSegment;
                            }

                            _stage = 3;
                            continue;
                        case 3:
                            if (_frameChunkIndex >= _frame.DataChunks.Count)
                            {
                                _stage = 4;
                                continue;
                            }

                            var dataChunk = _frame.DataChunks[_frameChunkIndex++];
                            _currentFrameChunkPrefix = dataChunk.Prefix;
                            _currentFrameChunkSuffix = dataChunk.Suffix;
                            _currentSegment = _currentFrameChunkPrefix;
                            _sourceStream.Position = dataChunk.DataOffset;
                            _remainingFileBytes = dataChunk.Length;
                            _stage = 31;
                            return _currentSegment;
                        case 31:
                            _currentSegment = _currentFrameChunkSuffix;
                            _stage = 3;
                            return _currentSegment;
                        case 4:
                            _currentSegment = IendChunk;
                            _stage = 5;
                            return _currentSegment;
                        default:
                            return null;
                    }
                }
            }
        }

        private readonly record struct PngChunkReadResult(string Type, byte[] Data, long DataOffset, int DataLength, uint Crc, byte[] EncodedChunk);
    }

    internal readonly record struct FrameRenderResult(int DelayMilliseconds, bool IsLastFrame, int FrameIndex);

    internal sealed class ApngFrameDescriptor
    {
        public ApngFrameDescriptor(
            byte[] ihdrChunk,
            int width,
            int height,
            int xOffset,
            int yOffset,
            int delayMilliseconds,
            byte disposeOp,
            byte blendOp,
            IReadOnlyList<FrameDataChunk> dataChunks)
        {
            IhdrChunk = ihdrChunk;
            Width = width;
            Height = height;
            XOffset = xOffset;
            YOffset = yOffset;
            DelayMilliseconds = delayMilliseconds;
            DisposeOp = disposeOp;
            BlendOp = blendOp;
            DataChunks = dataChunks;
        }

        public byte[] IhdrChunk { get; }
        public int Width { get; }
        public int Height { get; }
        public int XOffset { get; }
        public int YOffset { get; }
        public int DelayMilliseconds { get; }
        public byte DisposeOp { get; }
        public byte BlendOp { get; }
        public IReadOnlyList<FrameDataChunk> DataChunks { get; }
    }

    internal sealed class ApngFrameBuilder
    {
        private ApngFrameBuilder(byte[] ihdrChunk, int width, int height, int xOffset, int yOffset, int delayMilliseconds, byte disposeOp, byte blendOp)
        {
            IhdrChunk = ihdrChunk;
            Width = width;
            Height = height;
            XOffset = xOffset;
            YOffset = yOffset;
            DelayMilliseconds = delayMilliseconds;
            DisposeOp = disposeOp;
            BlendOp = blendOp;
        }

        public byte[] IhdrChunk { get; }
        public int Width { get; }
        public int Height { get; }
        public int XOffset { get; }
        public int YOffset { get; }
        public int DelayMilliseconds { get; }
        public byte DisposeOp { get; }
        public byte BlendOp { get; }
        public List<FrameDataChunk> DataChunks { get; } = new();

        public static ApngFrameBuilder Create(byte[] fcTlData, byte[] baseIhdr)
        {
            if (fcTlData.Length < 26)
            {
                throw new InvalidDataException("fcTL 长度无效。");
            }

            var width = ApngChunkCodec.ReadInt32BigEndian(fcTlData, 4);
            var height = ApngChunkCodec.ReadInt32BigEndian(fcTlData, 8);
            var xOffset = ApngChunkCodec.ReadInt32BigEndian(fcTlData, 12);
            var yOffset = ApngChunkCodec.ReadInt32BigEndian(fcTlData, 16);
            var delayNumerator = BinaryPrimitives.ReadUInt16BigEndian(fcTlData.AsSpan(20, 2));
            var delayDenominator = BinaryPrimitives.ReadUInt16BigEndian(fcTlData.AsSpan(22, 2));
            var delayMilliseconds = delayDenominator == 0
                ? 100
                : (int)Math.Round(delayNumerator * 1000d / delayDenominator);

            if (delayMilliseconds <= 0)
            {
                delayMilliseconds = 100;
            }

            var ihdrData = (byte[])baseIhdr.Clone();
            BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(0, 4), width);
            BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(4, 4), height);
            var ihdrChunk = ApngChunkCodec.CreateChunkBytes("IHDR", ihdrData);

            return new ApngFrameBuilder(
                ihdrChunk,
                width,
                height,
                xOffset,
                yOffset,
                delayMilliseconds,
                fcTlData[24],
                fcTlData[25]);
        }

        public static ApngFrameBuilder CreateDefault(byte[] ihdrData)
        {
            return new ApngFrameBuilder(
                ApngChunkCodec.CreateChunkBytes("IHDR", ihdrData),
                ApngChunkCodec.ReadInt32BigEndian(ihdrData, 0),
                ApngChunkCodec.ReadInt32BigEndian(ihdrData, 4),
                0,
                0,
                100,
                0,
                0);
        }

        public ApngFrameDescriptor Build()
        {
            return new ApngFrameDescriptor(
                IhdrChunk,
                Width,
                Height,
                XOffset,
                YOffset,
                DelayMilliseconds,
                DisposeOp,
                BlendOp,
                DataChunks.ToArray());
        }
    }

    internal readonly record struct FrameDataChunk(long DataOffset, int Length, byte[] Prefix, byte[] Suffix)
    {
        public static FrameDataChunk FromIdat(long dataOffset, int dataLength, uint crc)
        {
            Span<byte> prefix = stackalloc byte[8];
            BinaryPrimitives.WriteInt32BigEndian(prefix[..4], dataLength);
            "IDAT"u8.CopyTo(prefix[4..]);

            Span<byte> suffix = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(suffix, crc);
            return new FrameDataChunk(dataOffset, dataLength, prefix.ToArray(), suffix.ToArray());
        }

        public static FrameDataChunk FromFrameData(long dataOffset, ReadOnlySpan<byte> data)
        {
            Span<byte> prefix = stackalloc byte[8];
            BinaryPrimitives.WriteInt32BigEndian(prefix[..4], data.Length);
            "IDAT"u8.CopyTo(prefix[4..]);

            Span<byte> suffix = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(suffix, ApngChunkCodec.ComputeIdatCrc(data));
            return new FrameDataChunk(dataOffset, data.Length, prefix.ToArray(), suffix.ToArray());
        }
    }

    internal static class ApngChunkCodec
    {
        private static readonly uint[] CrcTable = BuildCrcTable();

        public static int ReadInt32BigEndian(byte[] buffer, int offset)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset, 4));
        }

        public static byte[] CreateChunkBytes(string type, ReadOnlySpan<byte> data)
        {
            Span<byte> typeBytes = stackalloc byte[4];
            System.Text.Encoding.ASCII.GetBytes(type, typeBytes);
            return CreateChunkBytes(typeBytes, data, ComputeCrc32(typeBytes, data));
        }

        public static uint ComputeIdatCrc(ReadOnlySpan<byte> data)
        {
            Span<byte> typeBytes = stackalloc byte[4];
            "IDAT"u8.CopyTo(typeBytes);
            return ComputeCrc32(typeBytes, data);
        }

        private static byte[] CreateChunkBytes(ReadOnlySpan<byte> typeBytes, ReadOnlySpan<byte> data, uint crc)
        {
            var buffer = new byte[12 + data.Length];
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), data.Length);
            typeBytes.CopyTo(buffer.AsSpan(4, 4));
            data.CopyTo(buffer.AsSpan(8));
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(8 + data.Length, 4), crc);
            return buffer;
        }

        private static uint ComputeCrc32(ReadOnlySpan<byte> typeBytes, ReadOnlySpan<byte> data)
        {
            uint crc = 0xFFFFFFFF;
            crc = UpdateCrc32(crc, typeBytes);
            crc = UpdateCrc32(crc, data);
            return ~crc;
        }

        private static uint UpdateCrc32(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var value in data)
            {
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            }

            return crc;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                var crc = i;
                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) == 1 ? 0xEDB88320U ^ (crc >> 1) : crc >> 1;
                }

                table[i] = crc;
            }

            return table;
        }
    }
}
