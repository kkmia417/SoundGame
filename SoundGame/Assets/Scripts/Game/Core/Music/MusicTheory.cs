// WHY:
// - 音楽理論上の基準値(A4=440Hz, MIDI_A4=69)を一元管理し、マジックナンバーを排除。
// - 参照元はこの定数を使うため、将来A4を442Hzに変える等が容易（変更点が1箇所で済む）。

namespace Game.Core.Music
{
    public static class MusicTheory
    {
        public const float A4_Hz = 440f; // 基準周波数
        public const int MIDI_A4 = 69;   // A4のMIDIノート番号
    }
}