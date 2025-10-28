// WHY:
// - ランタイムで安全に動くよう UnityEditor API は使わず、直接代入(SetParameters)で値を注入。
// - 音名→周波数→高さ変換は PitchControlSettings.hzToHeight と同一曲線で行い、入力系と座標系を統一。
// - 障害物は上下2枚の壁で“穴”を表現（軽量・堅牢）。中央にGateTriggerを置き、スコアは別コンポ(GateScore)に委譲。

using UnityEngine;
using Game.Adapters;           // PitchControlSettings
using Game.Core.Music;        // NoteName, MusicNotes

namespace Game.Gameplay.Course
{
    public sealed class NoteGate : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PitchControlSettings _settings;
        [SerializeField] private Material _wallMaterial;

        [Header("Note")]
        [SerializeField] private NoteName _note = NoteName.C;
        [SerializeField] private int _octave = 4;

        [Header("Wall Params")]
        [SerializeField] private float _wallWidth = 1.2f;
        [SerializeField] private float _wallDepth = 5f;
        [SerializeField] private float _holeSize  = 1.2f;
        [SerializeField] private float _wallHeight = 8f;

        private GameObject _built;

        // —— 外部からまとめて設定するための明示API（Editor/Runtime共通）——
        public void SetParameters(
            PitchControlSettings settings, Material wallMaterial,
            NoteName note, int octave,
            float wallWidth, float wallDepth, float holeSize, float wallHeight)
        {
            _settings     = settings;
            _wallMaterial = wallMaterial;
            _note         = note;
            _octave       = octave;
            _wallWidth    = wallWidth;
            _wallDepth    = wallDepth;
            _holeSize     = holeSize;
            _wallHeight   = wallHeight;
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (_settings == null)
            {
                Debug.LogWarning("NoteGate: PitchControlSettings not assigned.", this);
                return;
            }

            // 音名→Hz→高さ
            float hz    = MusicNotes.Frequency(_note, _octave, 440f);
            float holeY = Mathf.Clamp(_settings.hzToHeight.Evaluate(hz), _settings.minHeight, _settings.maxHeight);

            // 既存を破棄
            if (_built != null)
            {
                if (Application.isPlaying) Destroy(_built);
                else DestroyImmediate(_built);
            }

            // 壁生成（上下二分割＋中央トリガー）
            var p = new ObstacleHoleWallBuilder.Params{
                wallWidth = _wallWidth,
                wallDepth = _wallDepth,
                wallHeight = _wallHeight,
                holeCenterY = holeY,
                holeSize = _holeSize,
                wallMaterial = _wallMaterial
            };
            _built = ObstacleHoleWallBuilder.Build(gameObject, in p);
            _built.transform.localPosition = Vector3.zero;

            // スコア用トリガーに GateScore を付与（重複付与は避ける）
            var trig = _built.transform.Find("GateTrigger");
            if (trig != null && trig.GetComponent<GateScore>() == null)
                trig.gameObject.AddComponent<GateScore>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // エディタ上で値変更時は即見た目更新（再生中は負荷・実行順の都合で控える）
            if (!Application.isPlaying) Rebuild();
        }
#endif
    }
}
