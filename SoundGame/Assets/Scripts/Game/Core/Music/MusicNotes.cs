// WHY:
// - レベルを“音階で”記述したい。A4=440Hzの12平均律から任意音の周波数を確定し、
//   既存の "Hz→高さ" マッピング（AnimationCurve）に流し込むことで、
//   ゲームの難所を「ド・レ・ミ…」で直感的に設計できる。
// - Coreに置くのはUnity依存を避け、テストしやすくするため。
namespace Game.Core.Music
{
    public enum NoteName { C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B }

    public static class MusicNotes
    {
        // A4=440Hz（可変にしたい場合は引数で渡す）
        public static float Frequency(NoteName note, int octave, float a4 = 440f)
        {
            // 半音オフセットをA4基準で計算：Cからの距離を通してA4へ換算
            int noteIndexFromC = note switch {
                NoteName.C => 0, NoteName.Cs => 1, NoteName.D => 2, NoteName.Ds => 3,
                NoteName.E => 4, NoteName.F => 5, NoteName.Fs => 6, NoteName.G => 7,
                NoteName.Gs => 8, NoteName.A => 9, NoteName.As => 10, NoteName.B => 11, _ => 0
            };
            // C4=261.625... だが、ここでは A4 からの相対半音数nを出す：
            // n = (octave差*12) + (noteIndexFromC - AのindexFromC=9)
            int n = (octave - 4) * 12 + (noteIndexFromC - 9);
            // 12平均律：f = 440 * 2^(n/12)
            return a4 * (float)System.Math.Pow(2.0, n / 12.0);
        }
    }
}