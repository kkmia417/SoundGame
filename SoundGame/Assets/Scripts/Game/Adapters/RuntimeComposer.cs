// WHY:
// - 依存のnew/組み立てを一箇所に集約（簡易DI）。ServiceLocatorは避け、明示依存で安全。
// - Hz→高さの変換は HeightMappingUtility に委譲し、マジックナンバー（440, 69 等）や重複ロジックを排除。
// - AnimationCurve は Utility 内で 0..1 正規化後に評価され、ガンマ/ゲイン/スナップ等も SO の設定に従う。

using UnityEngine;
using Game.Core;
using Game.Pitch;
using Game.Core.Mapping; // HeightMappingUtility

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
            if (_settings == null)
            {
                Debug.LogError("RuntimeComposer: settings not assigned.");
                enabled = false;
                return;
            }

            // ピッチ検出器の選択（将来差し替え可能）
            Detector = _useYin ? (IPitchDetector)new YinPitchDetector() : null;

            // Hz→高さのマッピングはユーティリティに集約
            System.Func<float, float> f = hz => HeightMappingUtility.EvaluateHeight(_settings, hz);

            // Core側マッパー（スムージング/Confidenceしきい値/Clamp はここで実施）
            Mapper = new PitchToHeightMapper(
                f,
                _settings.minHz, _settings.maxHz,
                _settings.minHeight, _settings.maxHeight,
                _settings.confidenceThreshold,
                _settings.pitchLerp
            );

            // マイク入力とフレームバッファ
            Mic   = new UnityMicrophoneInput(_settings.deviceName, _settings.sampleRate, _settings.frameSec);
            _frame = new float[Mathf.CeilToInt(_settings.sampleRate * _settings.frameSec)];
        }
    }
}