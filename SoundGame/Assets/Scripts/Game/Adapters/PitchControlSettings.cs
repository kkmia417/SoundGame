// WHY: パラメータをコードから分離 → デザイナブル（保守性UP）。SO差し替えで難易度調整も容易。
using UnityEngine;

namespace Game.Adapters
{
    [CreateAssetMenu(fileName="PitchControlSettings", menuName="Game/Audio/PitchControlSettings")]
    public class PitchControlSettings : ScriptableObject
    {
        [Header("Input")]
        [Range(0.01f,0.2f)] public float frameSec = 0.046f;
        [Range(0f,2f)] public float inputGain = 1f;
        public int sampleRate = 44100;
        public string deviceName = null;

        [Header("Detection")]
        public float minHz = 80f;
        public float maxHz = 800f;
        [Range(0f,1f)] public float confidenceThreshold = 0.85f;

        [Header("Smoothing")]
        [Range(0f,1f)] public float pitchLerp = 0.25f;

        [Header("Mapping")]
        public AnimationCurve hzToHeight = AnimationCurve.EaseInOut(100,0.5f, 800,6f);
        public float minHeight = 0.5f;
        public float maxHeight = 6f;
    }
}