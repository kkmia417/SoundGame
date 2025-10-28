// WHY:
// - デザイナーが個別に「この地点はミ（E4）」のように置けるパーツ。
// - PitchControlSettingsの hzToHeight を使い、音→Hz→高さ→穴配置 へ一貫変換。
// - ランタイム/OnValidate両対応で、値を変えれば即見た目が更新される。
using UnityEngine;
using Game.Adapters;
using Game.Core.Music;

namespace Game.Gameplay.Course
{
    [ExecuteAlways]
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
        [SerializeField] private float _holeSize = 1.2f;   // 穴の縦サイズ
        [SerializeField] private float _wallHeight = 8f;   // 「上下壁の総高さ」(min/maxHeightを十分包含)

        private GameObject _built;

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (_settings == null) { Debug.LogWarning("NoteGate: PitchControlSettings not set."); return; }
            float hz = MusicNotes.Frequency(_note, _octave, 440f);
            float holeY = _settings.hzToHeight.Evaluate(hz);
            // 制限内にクランプ
            holeY = Mathf.Clamp(holeY, _settings.minHeight, _settings.maxHeight);

            // 既存を破棄して再構築
            if (_built != null)
            {
                if (Application.isPlaying) Destroy(_built);
                else DestroyImmediate(_built);
            }

            var p = new ObstacleHoleWallBuilder.Params{
                wallWidth=_wallWidth, wallDepth=_wallDepth, wallHeight=_wallHeight,
                holeCenterY=holeY, holeSize=_holeSize, wallMaterial=_wallMaterial
            };
            _built = ObstacleHoleWallBuilder.Build(gameObject, p);
            _built.transform.localPosition = Vector3.zero;

            // スコア判定を付ける（後述の GateScore をアタッチ）
            var trig = _built.transform.Find("GateTrigger");
            if (trig != null && trig.GetComponent<GateScore>() == null)
                trig.gameObject.AddComponent<GateScore>();
        }

        private void OnValidate() { if (enabled) Rebuild(); }
    }
}
