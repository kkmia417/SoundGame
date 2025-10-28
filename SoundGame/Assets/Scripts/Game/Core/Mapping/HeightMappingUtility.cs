// WHY:
// - Hz→高さの“正規化/カーブ/ガンマ/ゲイン/スナップ”を1箇所に集約し、重複と矛盾を排除。
// - 呼び出し側はこの関数だけ使えばOK。仕様変更はここ1ファイルの修正で全体が更新。

using UnityEngine;
using Game.Adapters;       // PitchControlSettings
using Game.Core.Music;     // MusicTheory

namespace Game.Core.Mapping
{
    public static class HeightMappingUtility
    {
        /// <summary>
        /// Hz を 0..1 に正規化（log2 or 線形、半音スナップ対応）してから
        /// カーブ→ガンマ→ゲイン→min/maxHeight へマップする。
        /// </summary>
        public static float EvaluateHeight(PitchControlSettings s, float hz)
        {
            if (s == null || hz <= 0f) return s?.minHeight ?? 0f;

            // --- 1) 正規化 t: 0..1 ---
            float t;
            if (s.useLogMapping)
            {
                // MIDIスケール上で等間隔化（log2基準）
                float midi    = MusicTheory.MIDI_A4 + 12f * Mathf.Log(hz / MusicTheory.A4_Hz, 2f);
                float midiMin = MusicTheory.MIDI_A4 + 12f * Mathf.Log(s.minHz / MusicTheory.A4_Hz, 2f);
                float midiMax = MusicTheory.MIDI_A4 + 12f * Mathf.Log(s.maxHz / MusicTheory.A4_Hz, 2f);
                if (s.snapToSemitone) midi = Mathf.Round(midi);
                t = Mathf.InverseLerp(midiMin, midiMax, midi);
            }
            else
            {
                t = Mathf.InverseLerp(s.minHz, s.maxHz, hz);
            }
            t = Mathf.Clamp01(t);

            // --- 2) カーブ→ガンマ補正→ゲイン ---
            float yNorm = s.hzToHeight != null ? s.hzToHeight.Evaluate(t) : t;      // カーブ
            yNorm = Mathf.Pow(Mathf.Clamp01(yNorm), s.heightPower);                 // ガンマ
            yNorm = Mathf.Clamp01(yNorm * s.heightGain);                            // ゲイン

            // --- 3) 高さレンジへ ---
            return Mathf.Lerp(s.minHeight, s.maxHeight, yNorm);
        }
    }
}