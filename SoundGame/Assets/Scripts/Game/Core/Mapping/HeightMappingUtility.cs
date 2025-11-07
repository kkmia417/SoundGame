using System;
using UnityEngine;
using Game.Adapters; // PitchControlSettings

namespace Game.Core.Mapping
{
    /// <summary>
    /// Hz -> Height の写像ユーティリティ（設定に依存）。
    /// 旧実装のデリゲート `hzToHeight` は廃止し、直接この静的メソッドを呼ぶ。
    /// </summary>
    public static class HeightMappingUtility
    {
        /// <summary>
        /// 周波数(Hz)を 0..1 に正規化したトーン位置に変換（半音ベース）。
        /// s.minSemitone..s.maxSemitone をレンジとして使用。
        /// </summary>
        public static float HzToNormalizedTone(PitchControlSettings s, float hz)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (hz <= 0f) return 0f;

            // Hz -> MIDI風セミトーン: A4=440Hz -> 69
            float semitone = 12f * (float)Math.Log(hz / 440f, 2.0) + 69f;
            float t = (semitone - s.minSemitone) / Mathf.Max(1e-5f, (s.maxSemitone - s.minSemitone));
            return Mathf.Clamp01(t);
        }

        /// <summary>
        /// 設定に基づいて Hz をゲーム内の高さにマッピング。
        /// - ログ/べき乗マッピング
        /// - ゲイン/オフセット
        /// - min/maxHeight でクランプ
        /// </summary>
        public static float EvaluateHeight(PitchControlSettings s, float hz)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            float t = HzToNormalizedTone(s, hz);

            if (s.useLogMapping)
                t = Mathf.Pow(t, Mathf.Max(1e-3f, s.heightPower));

            float height = Mathf.Lerp(s.minHeight, s.maxHeight, t);
            height = height * s.heightGain + s.globalHeightOffset;
            return Mathf.Clamp(height, Mathf.Min(s.minHeight, s.maxHeight), Mathf.Max(s.minHeight, s.maxHeight));
        }

        /// <summary>
        /// 便利関数: 直接セミトーンに変換（MIDI相当値）。
        /// </summary>
        public static float HzToSemitone(float hz)
        {
            if (hz <= 0f) return 0f;
            return 12f * (float)Math.Log(hz / 440f, 2.0) + 69f;
        }
    }
}