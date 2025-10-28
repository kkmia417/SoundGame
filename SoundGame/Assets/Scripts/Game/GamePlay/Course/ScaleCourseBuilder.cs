// WHY:
// - 「Buildボタン押し忘れ」を防ぐため、Play開始時に子が無ければ自動生成。
// - 参照不足は明確なエラーログで早期発見。責務は“並べるだけ”に限定しNoteGateに委譲。

using UnityEngine;
using Game.Settings;   // NoteSequence
using Game.Adapters;   // PitchControlSettings
using Game.Core.Music; // NoteName

namespace Game.Gameplay.Course
{
    public sealed class ScaleCourseBuilder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private NoteSequence _sequence;
        [SerializeField] private PitchControlSettings _settings;
        [SerializeField] private Material _wallMaterial;

        [Header("Layout")]
        [SerializeField] private float _startOffset = 8f;
        [SerializeField] private float _spacing = 6f;
        [SerializeField] private float _z = 0f;

        [Header("Gate Params")]
        [SerializeField] private float _wallWidth = 1.2f;
        [SerializeField] private float _wallDepth = 5f;
        [SerializeField] private float _holeSize  = 1.2f;
        [SerializeField] private float _wallHeight = 8f;

        [SerializeField] private bool _autoBuildOnPlay = true;

        [ContextMenu("Build")]
        public void Build()
        {
            if (_sequence == null || _settings == null)
            {
                Debug.LogError("ScaleCourseBuilder: assign NoteSequence and PitchControlSettings.", this);
                return;
            }

            // 既存クリア
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(c);
                else Destroy(c);
#else
                Destroy(c);
#endif
            }

            float x = _startOffset;
            foreach (var s in _sequence.sequence)
            {
                var go = new GameObject($"Gate_{s.note}{s.octave}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(x, 0f, _z);

                var gate = go.AddComponent<NoteGate>();
                gate.SetParameters(_settings, _wallMaterial, s.note, s.octave,
                                   _wallWidth, _wallDepth, _holeSize, _wallHeight);
                gate.Rebuild();

                x += _spacing;
            }

            Debug.Log($"ScaleCourseBuilder: built {_sequence.sequence.Length} gates.", this);
        }

        private void Start()
        {
            if (_autoBuildOnPlay && transform.childCount == 0)
                Build();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && _sequence != null && _settings != null && transform.childCount > 0)
                Build();
        }
#endif
    }
}
