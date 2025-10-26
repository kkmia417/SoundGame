// RATIONALE（なぜこうするか）
// - マッピングはゲームデザイン依存の“パラメトリックな関数”であり、Unity APIに依存させない。
// - 関数型インジェクション(Func<float,float>)で曲線を差し替え可能（SOや外部設定から適用）。
// - 代替案：UnityEngine.AnimationCurveを直接参照→CoreがUnity依存になりテスト性低下。
// - Lerp/Clampも自前：Coreを.NET標準のみにするため。

using System;

namespace Game.Core
{
    public sealed class PitchToHeightMapper : IPitchToHeightMapper
    {
        private readonly Func<float, float> _hzToHeight;
        private readonly float _minHz, _maxHz, _minH, _maxH, _lerp;
        private readonly float _confidenceThresh;
        private float _smoothHz = 0f;

        public PitchToHeightMapper(
            Func<float, float> hzToHeight,
            float minHz, float maxHz,
            float minHeight, float maxHeight,
            float confidenceThreshold,
            float pitchLerp)
        {
            _hzToHeight = hzToHeight ?? (hz => hz); // デフォルト：恒等
            _minHz = minHz; _maxHz = maxHz;
            _minH = minHeight; _maxH = maxHeight;
            _confidenceThresh = confidenceThreshold;
            _lerp = pitchLerp;
        }

        public bool TryMap(PitchEstimate e, out float height)
        {
            height = 0f;
            if (!e.IsValid || e.Confidence < _confidenceThresh) return false;

            _smoothHz = _smoothHz <= 0 ? e.Hz : Lerp(_smoothHz, e.Hz, 1f - _lerp);
            float clampedHz = Clamp(_smoothHz, _minHz, _maxHz);
            float mapped = _hzToHeight(clampedHz);
            height = Clamp(mapped, _minH, _maxH);
            return true;
        }

        private static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}