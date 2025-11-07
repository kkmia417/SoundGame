using System;
using System.Threading;

namespace Game.Adapters.Audio
{
    /// <summary>
    /// Single-producer / single-consumer lock-free ring buffer for float samples.
    /// </summary>
    public sealed class RingBuffer
    {
        private readonly float[] _buf;
        private int _write;
        private int _read;
        private int _count;

        public int Capacity { get; }
        public int Count => Volatile.Read(ref _count);

        public RingBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _buf = new float[capacity];
        }

        public int Enqueue(ReadOnlySpan<float> src)
        {
            int toWrite = Math.Min(src.Length, Capacity - Count);
            int w = _write;
            for (int i = 0; i < toWrite; i++)
            {
                _buf[w] = src[i];
                if (++w == Capacity) w = 0;
            }
            _write = w;
            Interlocked.Add(ref _count, toWrite);
            return toWrite;
        }

        public int Dequeue(Span<float> dst)
        {
            int toRead = Math.Min(dst.Length, Count);
            int r = _read;
            for (int i = 0; i < toRead; i++)
            {
                dst[i] = _buf[r];
                if (++r == Capacity) r = 0;
            }
            _read = r;
            Interlocked.Add(ref _count, -toRead);
            return toRead;
        }
    }
}