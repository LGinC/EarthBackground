using System;
using System.Buffers;

namespace EarthBackground.Imaging
{
    internal sealed class SharedPixelBuffer : IDisposable
    {
        private readonly byte[] _buffer;
        private readonly int _requestedSize;
        private readonly int _width;
        private readonly int _height;
        private readonly object _lock = new();
        private int _referenceCount;
        private bool _disposed;

        public int Width => _width;
        public int Height => _height;
        public int Stride => _width * 4;
        public int SizeInBytes => _requestedSize;

        public SharedPixelBuffer(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _width = width;
            _height = height;
            _requestedSize = checked(width * height * 4);
            _buffer = ArrayPool<byte>.Shared.Rent(_requestedSize);
            _referenceCount = 1;
        }

        public SharedPixelBufferHandle Acquire()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SharedPixelBuffer));
                
                _referenceCount++;
                return new SharedPixelBufferHandle(this);
            }
        }

        internal void Release()
        {
            bool shouldDispose = false;
            lock (_lock)
            {
                _referenceCount--;
                if (_referenceCount == 0 && !_disposed)
                {
                    _disposed = true;
                    shouldDispose = true;
                }
            }
            
            if (shouldDispose)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }
        }

        public byte[] GetBuffer()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SharedPixelBuffer));
            
            return _buffer;
        }

        public void Dispose()
        {
            bool shouldReturn = false;
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    shouldReturn = true;
                }
            }
            
            if (shouldReturn)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }
        }
    }

    internal sealed class SharedPixelBufferHandle : IDisposable
    {
        private SharedPixelBuffer? _buffer;

        public SharedPixelBufferHandle(SharedPixelBuffer buffer)
        {
            _buffer = buffer;
        }

        public SharedPixelBuffer? Buffer => _buffer;

        public void Dispose()
        {
            _buffer?.Release();
            _buffer = null;
        }
    }
}