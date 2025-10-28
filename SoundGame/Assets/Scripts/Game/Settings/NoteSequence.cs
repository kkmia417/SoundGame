// WHY:
// - コースを「Hz」ではなく「音名」で設計したい。SOで配列化してGUIから並べ替え可能に。
// - レベルデザインをデータ駆動にし、コードを触らずに曲や難易度を差し替えられる。
using UnityEngine;
using Game.Core.Music;

namespace Game.Settings
{
    [CreateAssetMenu(fileName="NoteSequence", menuName="Game/Music/NoteSequence")]
    public class NoteSequence : ScriptableObject
    {
        [System.Serializable]
        public struct NoteSpec
        {
            public NoteName note;
            public int octave; // 例: 4=中央付近。ドレミをC4始まりなどに。
        }

        [Tooltip("左から順に配置される音階ゲート")]
        public NoteSpec[] sequence = new NoteSpec[]
        {
            new NoteSpec{ note=NoteName.C,  octave=4 }, // ド
            new NoteSpec{ note=NoteName.D,  octave=4 }, // レ
            new NoteSpec{ note=NoteName.E,  octave=4 }, // ミ
            new NoteSpec{ note=NoteName.F,  octave=4 }, // ファ
            new NoteSpec{ note=NoteName.G,  octave=4 }, // ソ
            new NoteSpec{ note=NoteName.A,  octave=4 }, // ラ
            new NoteSpec{ note=NoteName.B,  octave=4 }, // シ
            new NoteSpec{ note=NoteName.C,  octave=5 }, // ド(上)
        };
    }
}