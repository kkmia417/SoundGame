using System;
using UnityEngine;
using Game.Core;        // IPitchToHeightMapper, PitchEstimate
using Game.Adapters;    // PitchControlSettings

namespace Game.Pitch
{
    /// <summary>
    /// Maps (Hz, Confidence) to a stable height value with octave jump suppression,
    /// semitone snapping (with hysteresis) and confidence-driven smoothing.
    /// </summary>
    public sealed class PitchToHeightMapper : IPitchToHeightMapper
    {
        private readonly PitchControlSettings _s;

        private float _lastHeight;
        private float _lastSemitone;
        private bool _hasLast;

        public PitchToHeightMapper(PitchControlSettings settings)
        {
            _s = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// IPitchToHeightMapper contract:
        /// Convert a pitch estimate into a height. Returns false when no valid value can be produced yet.
        /// </summary>
        public bool TryMap(PitchEstimate est, out float height)
        {
            // 初期値
            if (!_hasLast)
            {
                // 最初のフレームで値が不十分な場合、minHeightを返して false にする
                if (est.Hz <= 0f || est.Confidence < _s.confidenceThreshold)
                {
                    height = _s.minHeight;
                    return false;
                }
            }

            // Confidence gating
            if (est.Confidence < _s.confidenceThreshold || est.Hz <= 0f)
            {
                // 既に有効値を持っていればそれを保持
                height = _hasLast ? _lastHeight : _s.minHeight;
                return _hasLast;
            }

            // Hz -> semitone (MIDI-like). A4=440Hz -> 69
            float semitone = 12f * (float)Math.Log(est.Hz / 440f, 2.0) + 69f;

            // Octave jump suppression（Δセミトーン/秒を制限）
            if (_hasLast && _s.maxSemitonePerSec > 0f)
            {
                float maxDelta = _s.maxSemitonePerSec * Time.deltaTime;
                float delta = Mathf.Clamp(semitone - _lastSemitone, -maxDelta, maxDelta);
                semitone = _lastSemitone + delta;
            }

            // 半音スナップ + ヒステリシス
            if (_s.snapToSemitone)
            {
                float q = _s.quantizeSemitoneHeights ? Mathf.Max(1, _s.quantizeDivisions) : 1f;
                float target = Mathf.Round(semitone * q) / q;

                if (_hasLast && _s.snapHysteresis > 0f)
                {
                    float band = _s.snapHysteresis; // 例: 0.25 = 半音の1/4
                    if (Mathf.Abs(target - _lastSemitone) < band)
                        target = _lastSemitone;
                }
                semitone = target;
            }

            // [minSemitone, maxSemitone] -> [0,1]
            float t = (semitone - _s.minSemitone) / Mathf.Max(1e-5f, (_s.maxSemitone - _s.minSemitone));
            t = Mathf.Clamp01(t);

            if (_s.useLogMapping)
                t = Mathf.Pow(t, Mathf.Max(1e-3f, _s.heightPower));

            float h = Mathf.Lerp(_s.minHeight, _s.maxHeight, t);
            h = h * _s.heightGain + _s.globalHeightOffset;

            // 信頼度連動スムージング
            float lerp = Mathf.Lerp(_s.minLerp, _s.maxLerp, Mathf.Clamp01(est.Confidence));
            h = Mathf.Lerp(_hasLast ? _lastHeight : h, h, lerp);

            // 状態更新
            _lastHeight = h;
            _lastSemitone = semitone;
            _hasLast = true;

            height = h;
            return true;
            // 契約上「高さを出せたか」をboolで返す
        }
    }
}
