// WHY:
// - UnityEditor APIは使わず、SetParametersで明示的に値注入（Runtime/Editor両対応で安全）。
// - 音名→周波数→高さは HeightMappingUtility に委譲し、マジックナンバー(440, 69等)と重複ロジックを排除。
// - 入力系と同一の曲線・オプション（log2, ゲイン, ガンマ, スナップ）で座標系を統一し、見た目と操作の一致を担保。
// - 障害物は上下2枚の壁で“穴”を表現（軽量・堅牢）。中央の GateTrigger にスコア責務（GateScore）を委譲。

using UnityEngine;
using Game.Adapters;            // PitchControlSettings
using Game.Core.Music;         // NoteName, MusicNotes, MusicTheory
using Game.Core.Mapping;       // HeightMappingUtility

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
        [SerializeField] private float _wallWidth  = 1.2f;
        [SerializeField] private float _wallDepth  = 5f;
        [SerializeField] private float _holeSize   = 1.2f;
        [SerializeField] private float _wallHeight = 8f;

        private GameObject _built;

        // 外部からまとめて設定するための明示API（Editor/Runtime共通）
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

            // 音名→Hz（基準は MusicTheory.A4_Hz に集約）
            float hz = MusicNotes.Frequency(_note, _octave, MusicTheory.A4_Hz);

            // 同一ユーティリティで高さに変換（入力と完全一致のルール）
            float holeY = HeightMappingUtility.EvaluateHeight(_settings, hz);
            holeY = Mathf.Clamp(holeY, _settings.minHeight, _settings.maxHeight);

            // 既存を破棄
            if (_built != null)
            {
                if (Application.isPlaying) Destroy(_built);
                else DestroyImmediate(_built);
            }

            // 壁生成（上下二分割＋中央トリガー）
            var p = new ObstacleHoleWallBuilder.Params
            {
                wallWidth   = _wallWidth,
                wallDepth   = _wallDepth,
                wallHeight  = _wallHeight,
                holeCenterY = holeY,
                holeSize    = _holeSize,
                wallMaterial= _wallMaterial
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
            // エディタ上で値変更時は即見た目更新（再生中は負荷回避）
            if (!Application.isPlaying) Rebuild();
        }
#endif
    }
}
