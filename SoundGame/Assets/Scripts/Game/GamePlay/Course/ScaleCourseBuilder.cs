// WHY:
// - コース全体を「NoteSequence（ドレミ…）」で自動生成し、X方向に等間隔で並べる。
// - 各ゲートは NoteGate に任せるため、ここは“並べるだけ”に責務を限定（SRP）。
using UnityEngine;
using UnityEditor;
using Game.Settings;
using Game.Adapters;

namespace Game.Gameplay.Course
{
    public sealed class ScaleCourseBuilder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private NoteSequence _sequence;
        [SerializeField] private PitchControlSettings _settings;
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private GameObject _playerBall;

        [Header("Layout")]
        [SerializeField] private int _startOffset = 8;    // スタートから最初のゲートまでの距離
        [SerializeField] private float _spacing = 6f;     // ゲート間隔
        [SerializeField] private float _z = 0f;

        [Header("Gate Params")]
        [SerializeField] private float _wallWidth = 1.2f;
        [SerializeField] private float _wallDepth = 5f;
        [SerializeField] private float _holeSize = 1.2f;
        [SerializeField] private float _wallHeight = 8f;

        [ContextMenu("Build")]
        public void Build()
        {
            if (_sequence == null || _settings == null) { Debug.LogError("ScaleCourseBuilder: assign sequence/settings."); return; }

            // 既存クリア
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            float x = _startOffset;
            foreach (var s in _sequence.sequence)
            {
                var go = new GameObject($"Gate_{s.note}{s.octave}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x, 0f, _z);

                var gate = go.AddComponent<NoteGate>();
                // 直接フィールドにセット
                var so = new SerializedObject(gate);
                so.FindProperty("_settings").objectReferenceValue = _settings;
                so.FindProperty("_wallMaterial").objectReferenceValue = _wallMaterial;
                so.FindProperty("_note").enumValueIndex = (int)s.note;
                so.FindProperty("_octave").intValue = s.octave;
                so.FindProperty("_wallWidth").floatValue = _wallWidth;
                so.FindProperty("_wallDepth").floatValue = _wallDepth;
                so.FindProperty("_holeSize").floatValue = _holeSize;
                so.FindProperty("_wallHeight").floatValue = _wallHeight;
                so.ApplyModifiedPropertiesWithoutUndo();

                gate.Rebuild();
                x += _spacing;
            }
        }
    }
}
