using UnityEngine;
using Game.Core;
using Game.Pitch;

namespace Game.Adapters
{
    /// <summary>Simple runtime composer (wires settings, mic, detector, mapper).</summary>
    public sealed class RuntimeComposer : MonoBehaviour
    {
        [SerializeField] private PitchControlSettings _settings;

        public UnityMicrophoneInput Mic { get; private set; }
        public IPitchDetector Detector { get; private set; }
        public IPitchToHeightMapper Mapper { get; private set; }

        public int SampleRate => _settings != null ? _settings.sampleRate : 48000;
        public float MinHz => _settings != null ? _settings.minHz : 60f;
        public float MaxHz => _settings != null ? _settings.maxHz : 1500f;

        private float[] _frame;
        public float[] Frame => _frame;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("RuntimeComposer: PitchControlSettings is not assigned.", this);
                enabled = false;
                return;
            }

            // Input
            Mic = new UnityMicrophoneInput(_settings.deviceName, _settings.sampleRate, _settings.frameSec);

            // Detector (YINなど、あなたの実装に合わせて)
            Detector = new YinPitchDetector();

            // NEW: hzToHeight デリゲートは不要。TryMap 実装のマッパーをそのまま使う
            Mapper = new PitchToHeightMapper(_settings);

            // Fixed-size frame buffer
            _frame = new float[Mathf.CeilToInt(_settings.sampleRate * Mathf.Max(0.005f, _settings.frameSec))];
        }
    }
}