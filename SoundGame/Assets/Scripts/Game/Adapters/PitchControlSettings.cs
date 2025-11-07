using UnityEngine;

namespace Game.Adapters
{
    [CreateAssetMenu(menuName = "Game/PitchControlSettings")]
    public sealed class PitchControlSettings : ScriptableObject
    {
        [Header("Input")]
        public string deviceName = null;
        public int sampleRate = 48000;
        public float frameSec = 0.05f;
        public float inputGain = 1.0f;

        [Header("Detection")]
        public float minHz = 60f;
        public float maxHz = 1500f;
        public float confidenceThreshold = 0.3f;

        [Header("Mapping (height output)")]
        public float minHeight = -2f;
        public float maxHeight =  2f;
        public float globalHeightOffset = 0f;
        public bool  useLogMapping = true;
        public float heightGain = 1.0f;
        public float heightPower = 1.2f;

        [Header("Semitone range")]
        public float minSemitone = 48f; // C3
        public float maxSemitone = 84f; // C6

        [Header("Snap & Stability")]
        public bool  snapToSemitone = true;
        public bool  quantizeSemitoneHeights = false;
        public int   quantizeDivisions = 1;
        public float snapHysteresis = 0.25f;   // half-semitone band
        public float maxSemitonePerSec = 24f;  // rate limit (semitones/sec)

        [Header("Smoothing")]
        public float minLerp = 0.08f;
        public float maxLerp = 0.35f;

        // --- Backward-compat (legacy fields) ---
        // 一部の古いスクリプトが参照している可能性があるため残す（新ロジックでは未使用）
        [Tooltip("Deprecated: use minLerp/maxLerp instead")]
        public float pitchLerp = 0.2f;
    }
}