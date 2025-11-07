using System;
using UnityEngine;

namespace Game.Core
{
    public sealed class PitchToHeightMapper : IPitchToHeightMapper
    {
        private readonly PitchControlSettings _s;
        private float _lastHeight;
        private float _lastSemitone;
        private bool _hasLast;

        public PitchToHeightMapper(PitchControlSettings settings)
        {
            _s = settings;
        }

        public float Map(float hz, float confidence, double time)
        {
            // Confidence gating
            if (confidence < _s.confidenceThreshold || hz <= 0f)
                return _hasLast ? _lastHeight : _s.minHeight;

            // Hz -> semitone (MIDI-like)
            float semitone = 12f * (float)Math.Log(hz / 440f, 2.0) + 69f;

            // Clamp rate of change (octave jump suppression)
            if (_hasLast)
            {
                float maxDelta = _s.maxSemitonePerSec * Time.deltaTime;
                float delta = Mathf.Clamp(semitone - _lastSemitone, -maxDelta, maxDelta);
                semitone = _lastSemitone + delta;
            }

            // Snap & hysteresis
            if (_s.snapToSemitone)
            {
                float q = _s.quantizeSemitoneHeights ? Mathf.Max(1, _s.quantizeDivisions) : 1f;
                float target = Mathf.Round(semitone * q) / q;

                if (_hasLast)
                {
                    float band = Mathf.Max(0f, _s.snapHysteresis);
                    if (Mathf.Abs(target - _lastSemitone) < band)
                        target = _lastSemitone;
                }
                semitone = target;
            }

            // Normalize to [0,1] over semitone range
            float t = (semitone - _s.minSemitone) / Mathf.Max(1e-5f, (_s.maxSemitone - _s.minSemitone));
            t = Mathf.Clamp01(t);

            if (_s.useLogMapping)
                t = Mathf.Pow(t, Mathf.Max(1e-3f, _s.heightPower));

            float height = Mathf.Lerp(_s.minHeight, _s.maxHeight, t);
            height = height * _s.heightGain + _s.globalHeightOffset;

            // Confidence-driven smoothing
            float lerp = Mathf.Lerp(_s.minLerp, _s.maxLerp, Mathf.Clamp01(confidence));
            height = Mathf.Lerp(_hasLast ? _lastHeight : height, height, lerp);

            _lastHeight = height;
            _lastSemitone = semitone;
            _hasLast = true;

            return height;
        }
    }
}
