using System;
using System.Linq;
using UnityEngine;
using Game.Adapters.Audio;

namespace Game.Adapters
{
    /// <summary>
    /// Microphone input wrapper that pushes samples into a lock-free ring buffer
    /// and delivers fixed-size frames without GC allocations.
    /// </summary>
    public sealed class UnityMicrophoneInput
    {
        private readonly int _targetSampleRate;
        private readonly int _channels;
        private readonly RingBuffer _rb;
        private readonly float[] _pullTemp; // fixed reusable chunk for GetData
        private AudioClip _clip;
        private string _device;
        private int _clipSamples;
        private int _micPosPrev;

        public int SampleRate => _targetSampleRate;
        public int Channels => _channels;
        public int BufferedSamples => _rb.Count;

        public UnityMicrophoneInput(string deviceName, int requestedSampleRate, int channels, int ringBufferSeconds = 2)
        {
            _device = deviceName;
            // Prefer actual output sample rate in Unity runtime to avoid mismatch
            _targetSampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : requestedSampleRate;
            _channels = Math.Max(1, channels);
            _rb = new RingBuffer(Math.Max(1024, _targetSampleRate * ringBufferSeconds));
            _pullTemp = new float[Math.Max(256, _targetSampleRate / 10)]; // ~100ms
        }

        public void Start()
        {
            // device select fallback
            if (string.IsNullOrEmpty(_device) || !Microphone.devices.Contains(_device))
            {
                _device = Microphone.devices.FirstOrDefault();
            }

            _clip = Microphone.Start(_device, true, 1, _targetSampleRate);
            _clipSamples = _clip != null ? _clip.samples : 0;
            _micPosPrev = 0;
        }

        public void Stop()
        {
            if (_clip != null && !string.IsNullOrEmpty(_device))
            {
                Microphone.End(_device);
            }
            _clip = null;
        }

        /// <summary>
        /// Call this from Update to pump new microphone samples into the ring buffer.
        /// </summary>
        public void Pump()
        {
            if (_clip == null || string.IsNullOrEmpty(_device)) return;
            if (!Microphone.IsRecording(_device)) return;

            int pos = Microphone.GetPosition(_device);
            if (pos < 0 || pos > _clipSamples) return;

            int delta = pos - _micPosPrev;
            if (delta < 0) delta += _clipSamples; // wrapped

            // pull in chunks
            int pulled = 0;
            while (delta > 0)
            {
                int chunk = Math.Min(delta, _pullTemp.Length);
                int start = (_micPosPrev + pulled) % _clipSamples;
                _clip.GetData(_pullTemp, start);
                _rb.Enqueue(_pullTemp.AsSpan(0, chunk));
                pulled += chunk;
                delta -= chunk;
            }
            _micPosPrev = pos;
        }

        /// <summary>
        /// Try to dequeue exactly dst.Length samples. Returns false when not enough.
        /// </summary>
        public bool TryDequeueFrame(Span<float> dst)
        {
            if (_rb.Count < dst.Length) return false;
            _rb.Dequeue(dst);
            // Stereo->Mono, resampling etc. can be done here if needed.
            return true;
        }
    }
}
