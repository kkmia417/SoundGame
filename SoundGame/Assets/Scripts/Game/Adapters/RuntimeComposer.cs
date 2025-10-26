// WHY: 依存のnew/組み立てを一箇所に集約（簡易DI）。ServiceLocatorは避け、明示依存で安全。
// AnimationCurve→Func<float,float> に変換してCoreマッパーへ注入。
using UnityEngine;
using Game.Core;
using Game.Pitch;

namespace Game.Adapters
{
    public sealed class RuntimeComposer : MonoBehaviour
    {
        [SerializeField] private PitchControlSettings _settings;
        [SerializeField] private bool _useYin = true;

        public IMicrophoneInput Mic { get; private set; }
        public IPitchDetector Detector { get; private set; }
        public IPitchToHeightMapper Mapper { get; private set; }
        public int SampleRate => _settings != null ? _settings.sampleRate : 44100;

        private float[] _frame; public float[] Frame => _frame;

        private void Awake()
        {
            if (_settings == null){ Debug.LogError("RuntimeComposer: settings not assigned."); enabled = false; return; }

            Detector = _useYin ? (IPitchDetector)new YinPitchDetector() : null; // 代替検出器を後で追加可
            System.Func<float,float> f = hz => _settings.hzToHeight.Evaluate(hz);
            Mapper = new PitchToHeightMapper(f, _settings.minHz, _settings.maxHz, _settings.minHeight, _settings.maxHeight,
                _settings.confidenceThreshold, _settings.pitchLerp);

            Mic = new UnityMicrophoneInput(_settings.deviceName, _settings.sampleRate, _settings.frameSec);
            _frame = new float[Mathf.CeilToInt(_settings.sampleRate * _settings.frameSec)];
        }
    }
}