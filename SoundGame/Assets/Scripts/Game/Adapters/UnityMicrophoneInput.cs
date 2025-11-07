// WHY: UnityのMicrophone API依存をAdaptersに隔離しつつ、GCゼロで安全に固定長フレームを供給する。
// - lock-free RingBufferにサンプルを貯め、要求時にぴったりframeSize分を取り出す（不足ならfalse）。
// - ラップ/差分処理を行い、AudioClip.GetDataの一時バッファも再利用する。
// - 既存の IMicrophoneInput 契約 (TryGetFrame(float[], out int)) を維持。

using System;
using System.Linq;
using UnityEngine;
using Game.Core;
using Game.Adapters.Audio;

namespace Game.Adapters
{
    public sealed class UnityMicrophoneInput : IMicrophoneInput
    {
        private readonly string _device;
        private readonly int _sampleRate;
        private readonly int _frameSize;

        private AudioClip _clip;
        private int _clipSamples;
        private int _micPosPrev;

        private readonly RingBuffer _rb;
        private readonly float[] _pullTemp; // GetData用の再利用バッファ

        public UnityMicrophoneInput(string device, int sampleRate, float frameSec)
        {
            _device = SelectDevice(device);
            _sampleRate = sampleRate > 0 ? sampleRate : AudioSettings.outputSampleRate;
            _frameSize = Mathf.Max(128, Mathf.CeilToInt(_sampleRate * Mathf.Max(0.005f, frameSec)));

            _rb = new RingBuffer(Mathf.Max(_sampleRate * 2, 4096)); // 約2秒分
            _pullTemp = new float[Mathf.Max(256, _sampleRate / 10)]; // ~100ms

            _clip = Microphone.Start(_device, true, 1, _sampleRate);
            _clipSamples = _clip != null ? _clip.samples : 0;
            _micPosPrev = 0;
        }

        public bool TryGetFrame(float[] buffer, out int samples)
        {
            samples = 0;
            if (buffer == null || buffer.Length < _frameSize) return false;
            if (_clip == null || string.IsNullOrEmpty(_device)) return false;
            if (!Microphone.IsRecording(_device)) return false;

            Pump(); // 新規サンプルをRBへ取り込み

            if (_rb.Count < _frameSize) return false;

            // きっちりframeSize分を取り出す
            int read = _rb.Dequeue(buffer.AsSpan(0, _frameSize));
            samples = read;
            return read == _frameSize;
        }

        private void Pump()
        {
            int pos = Microphone.GetPosition(_device);
            if (pos < 0 || pos > _clipSamples) return;

            int delta = pos - _micPosPrev;
            if (delta < 0) delta += _clipSamples; // ラップ

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

        private static string SelectDevice(string preferred)
        {
            if (!string.IsNullOrEmpty(preferred) && Microphone.devices.Contains(preferred)) return preferred;
            return Microphone.devices.FirstOrDefault();
        }
    }
}
