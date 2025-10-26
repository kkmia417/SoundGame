// RATIONALE（なぜこうするか）
// - Coreは「ゲームの言語」を定義する層であり、Unityに依存しない純C#にする。
// - 依存性逆転の原則（DIP）により、上位レイヤ（Domain/UI/Adapters）はこの「契約」に依存する。
// - 代替案：UnityのMonoBehaviourやScriptableObjectを直接参照→テスト困難・結合が強くなるため不採用。
// - 拡張性：IPitchDetectorの差し替え（YIN/自相関/将来の深層学習モデル）をコンパイル時依存だけで行える。

namespace Game.Core
{
    /// <summary>
    /// ピッチ推定の結果（Unity API非依存の値オブジェクト）。
    /// 値オブジェクトにすることで、テストとデバッグを容易にする。
    /// </summary>
    public readonly struct PitchEstimate
    {
        public readonly float Hz;
        public readonly float Confidence;

        public PitchEstimate(float hz, float confidence)
        {
            Hz = hz;
            Confidence = confidence;
        }

        public bool IsValid => Hz > 0f && Confidence > 0f;
    }

    /// <summary>
    /// マイクからモノラルPCMフレームを取得する契約。
    /// </summary>
    public interface IMicrophoneInput
    {
        bool TryGetFrame(float[] buffer, out int samples);
    }

    /// <summary>
    /// PCM → ピッチ(Hz)へ変換する契約。
    /// 実装（YIN/自相関など）はCoreに閉じた純C#で提供可能。
    /// </summary>
    public interface IPitchDetector
    {
        PitchEstimate Estimate(float[] buffer, int sampleRate, float minHz, float maxHz);
    }

    /// <summary>
    /// ピッチ(Hz) → 高さへ写像する契約。UIや物理に依存しない。
    /// </summary>
    public interface IPitchToHeightMapper
    {
        bool TryMap(PitchEstimate estimate, out float height);
    }
}