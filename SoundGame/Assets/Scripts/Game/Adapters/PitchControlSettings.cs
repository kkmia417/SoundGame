// WHY: パラメータをコードから分離 → デザイナブル（保守性UP）。SO差し替えで難易度調整も容易。
using UnityEngine;

namespace Game.Adapters
{
    [CreateAssetMenu(fileName = "PitchControlSettings", menuName = "Game/Audio/PitchControlSettings")]
    public class PitchControlSettings : ScriptableObject
    {
        [Header("Input")]
        [Range(0.01f, 0.2f)] public float frameSec = 0.046f;
        [Range(0f, 2f)] public float inputGain = 1f;
        public int sampleRate = 44100;
        public string deviceName = null;

        [Header("Detection")]
        public float minHz = 80f;
        public float maxHz = 800f;
        [Range(0f, 1f)] public float confidenceThreshold = 0.85f;

        [Header("Smoothing")]
        [Range(0f, 1f)] public float pitchLerp = 0.25f;

        [Header("Mapping")]
        // NOTE: ここは「Hz→高さ」を直接描く or 0..1正規化用カーブのどちらでもOK（ユーティリティ側で自動判別）
        public AnimationCurve hzToHeight = AnimationCurve.EaseInOut(100, 0.5f, 800, 6f);
        public float minHeight = 0.5f;
        public float maxHeight = 6f;

        [Header("Global Offset")]
        public float globalHeightOffset = 0f; // ← 全体の高さを持ち上げ/下げる（ワールドY単位）

        // 追加（Mapping Options）
        [Header("Mapping Options (Height Emphasis)")]
        public bool useLogMapping = true;                   // 半音刻みを等間隔にするlog2正規化
        [Range(0.1f, 3f)] public float heightGain = 1.4f;   // 高さのゲイン（>1で差拡大）
        [Range(0.1f, 3f)] public float heightPower = 0.8f;  // ガンマ補正（<1で中域を持ち上げ）
        public bool snapToSemitone = false;                 // 半音にスナップ（離散段差）

        [Header("Quantization")]
        public bool quantizeSemitoneHeights = true;         // 半音ごとに高さを段階化
        [Range(1, 48)] public int quantizeDivisions = 12;   // 何分割にするか（半音単位=12が基本）
    }
}