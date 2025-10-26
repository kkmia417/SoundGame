// WHY: UnityのMicrophone API依存をAdaptersに隔離。Coreは純ロジックのまま。
// リングバッファから直近フレームを安全に切り出す（ラップ対応）。
using UnityEngine;
using Game.Core;

namespace Game.Adapters
{
    public sealed class UnityMicrophoneInput : IMicrophoneInput
    {
        private readonly string _device; private readonly int _sampleRate; private readonly int _frameSize;
        private AudioClip _clip;

        public UnityMicrophoneInput(string device, int sampleRate, float frameSec)
        {
            _device = device; _sampleRate = sampleRate; _frameSize = Mathf.Max(128, Mathf.CeilToInt(sampleRate * frameSec));
            _clip = Microphone.Start(_device, true, 1, _sampleRate); // 1秒リングバッファ
        }

        public bool TryGetFrame(float[] buffer, out int samples)
        {
            samples = 0;
            if (_clip == null || !Microphone.IsRecording(_device)) return false;
            int pos = Microphone.GetPosition(_device); if (pos <= 0) return false;

            int start = pos - _frameSize; if (start < 0) start += _clip.samples;
            if (start + _frameSize <= _clip.samples) _clip.GetData(buffer, start);
            else { var temp = new float[_frameSize]; _clip.GetData(temp, start); _clip.GetData(temp, 0); System.Array.Copy(temp, 0, buffer, 0, _frameSize); }
            samples = _frameSize; return true;
        }
    }
}