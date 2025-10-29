using UnityEngine;
using Game.Adapters;
using Game.Core.Music;

namespace Game.Core.Mapping
{
    public static class HeightMappingUtility
    {
        /// <summary>
        /// Hz → 高さ（中間値も返す）
        /// ・X軸は 0..1 / Hz を自動判別
        /// ・0..1 カーブの Y 出力が狭い場合はキーの最小/最大で正規化
        /// ・量子化/ガンマ/ゲイン適用後、globalHeightOffset を加算して最終高さを返す
        /// </summary>
        public static float EvaluateHeight(PitchControlSettings s, float hz,
            out float t01, out float yCurve, out float yNorm)
        {
            t01 = 0f; yCurve = 0f; yNorm = 0f;
            if (s == null) return 0f;

            // --- 1) 入力レンジ防御 ---
            float minHz = Mathf.Max(1f, s.minHz);
            float maxHz = Mathf.Max(minHz + 1f, s.maxHz);
            hz = Mathf.Clamp(hz, minHz, maxHz);

            // --- 2) t: 0..1 正規化（log2 or 線形、半音スナップ対応）---
            if (s.useLogMapping)
            {
                float midi    = MusicTheory.MIDI_A4 + 12f * Mathf.Log(hz / MusicTheory.A4_Hz, 2f);
                float midiMin = MusicTheory.MIDI_A4 + 12f * Mathf.Log(minHz / MusicTheory.A4_Hz, 2f);
                float midiMax = MusicTheory.MIDI_A4 + 12f * Mathf.Log(maxHz / MusicTheory.A4_Hz, 2f);
                if (s.snapToSemitone) midi = Mathf.Round(midi);
                t01 = SafeInverseLerp(midiMin, midiMax, midi);
            }
            else
            {
                t01 = SafeInverseLerp(minHz, maxHz, hz);
            }
            t01 = Mathf.Clamp01(t01);

            // --- 2.5) 量子化（任意）：0..1 を N 分割で段差化 ---
            if (s.quantizeSemitoneHeights)
            {
                int n = Mathf.Max(1, s.quantizeDivisions);
                t01 = Mathf.Round(t01 * n) / n;
            }

            // --- 3) カーブ評価（X軸 0..1 or Hz を自動判別）---
            float yRaw = EvaluateCurveRaw(
                s.hzToHeight, t01, hz, minHz, maxHz,
                out bool xIs01, out float keyYMin, out float keyYMax
            );

            float minH = Mathf.Min(s.minHeight, s.maxHeight);
            float maxH = Mathf.Max(s.minHeight, s.maxHeight);

            if (xIs01)
            {
                // 0..1 カーブのY出力が狭い場合も 0..1 へ正規化してコントラスト確保
                if (keyYMax - keyYMin > 1e-6f)
                    yCurve = Mathf.Clamp01((yRaw - keyYMin) / (keyYMax - keyYMin));
                else
                    yCurve = Mathf.Clamp01(yRaw);
            }
            else
            {
                // Hz→高さカーブ：minHeight..maxHeight で 0..1 化
                yCurve = SafeInverseLerp(minH, maxH, yRaw);
            }

            // --- 4) ガンマ → ゲイン（飽和しにくい順）---
            float power = Mathf.Max(0.001f, s.heightPower);
            float gain  = Mathf.Max(0.001f, s.heightGain);
            yNorm = Mathf.Pow(Mathf.Clamp01(yCurve), power);
            yNorm = Mathf.Clamp01(yNorm * gain);

            // --- 5) 最終高さ（オフセット適用 & クランプ）---
            float y = Mathf.Lerp(minH, maxH, yNorm);
            y += s.globalHeightOffset;                 // ← 全体オフセットで“上寄せ”
            y = Mathf.Clamp(y, minH, maxH);            // 範囲外に出ないよう安全側に

            return y;
        }

        public static float EvaluateHeight(PitchControlSettings s, float hz)
            => EvaluateHeight(s, hz, out _, out _, out _);

        /// <summary>
        /// カーブの“生”評価値 + 付帯情報
        /// </summary>
        private static float EvaluateCurveRaw(
            AnimationCurve curve, float t01, float hz, float minHz, float maxHz,
            out bool xIs01, out float keyYMin, out float keyYMax)
        {
            xIs01   = true;
            keyYMin = 0f;
            keyYMax = 1f;

            if (curve == null || curve.keys == null || curve.keys.Length == 0)
                return t01;

            float x0 = curve.keys[0].time;
            float x1 = curve.keys[curve.keys.Length - 1].time;
            float span = Mathf.Abs(x1 - x0);

            // Yレンジ（0..1カーブでも 0..1 とは限らないため取得）
            keyYMin = float.PositiveInfinity;
            keyYMax = float.NegativeInfinity;
            for (int i = 0; i < curve.keys.Length; i++)
            {
                float v = curve.keys[i].value;
                if (v < keyYMin) keyYMin = v;
                if (v > keyYMax) keyYMax = v;
            }

            // 0..1 カーブの典型判定
            xIs01 = (span <= 5f && x0 >= -0.5f && x1 <= 1.5f);

            if (xIs01)
                return curve.Evaluate(t01); // 0..1
            else
            {
                float hzEval = Mathf.Lerp(minHz, maxHz, t01);
                return curve.Evaluate(hzEval); // 高さ(絶対)の可能性あり
            }
        }

        private static float SafeInverseLerp(float a, float b, float v)
        {
            if (Mathf.Approximately(a, b)) return 0f;
            return Mathf.InverseLerp(a, b, v);
        }
    }
}
