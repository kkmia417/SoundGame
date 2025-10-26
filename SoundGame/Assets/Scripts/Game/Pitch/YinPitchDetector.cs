// RATIONALE（なぜこうするか）
// - YINは声・モノフォニック信号のピッチ推定で安定しやすく、MVPに向く。
// - 純C#（System.Math）で実装することで、EditModeテストが可能（UnityEngine非依存）。
// - 代替案：FFTでピーク検出→オクターブ誤認や倍音で不安定になりやすい。
// - 代替案：AutoCorrelationのみ→YINより雑音耐性・重なりで弱い場合がある。
// - 将来：別Detectorを追加してasmdef参照だけで差し替え可能（拡張性）。

using Game.Core;
using System;

namespace Game.Pitch
{
    public sealed class YinPitchDetector : IPitchDetector
    {
        public PitchEstimate Estimate(float[] x, int fs, float minHz, float maxHz)
        {
            // 防御的プログラミング（異常値でのコスト暴走を防ぐ）
            if (x == null || x.Length < 8 || fs <= 0 || minHz <= 0 || maxHz <= minHz)
                return new PitchEstimate(0, 0);

            int tauMin = (int)Math.Floor(fs / Math.Max(maxHz, 1f));
            int tauMax = (int)Math.Ceiling(fs / Math.Max(minHz, 1f));
            int N = x.Length;
            if (tauMin < 1 || tauMax >= N) return new PitchEstimate(0, 0);

            // 差分関数 d(tau)
            var d = new float[tauMax + 1];
            for (int tau = tauMin; tau <= tauMax; tau++)
            {
                double sum = 0;
                int limit = N - tau;
                for (int i = 0; i < limit; i++)
                {
                    float diff = x[i] - x[i + tau];
                    sum += diff * diff;
                }
                d[tau] = (float)sum;
            }

            // 累積平均正規化差分関数（CMND）
            var cmnd = new float[tauMax + 1];
            double running = 0.0;
            for (int tau = tauMin; tau <= tauMax; tau++)
            {
                running += d[tau];
                cmnd[tau] = (float)(d[tau] * tau / Math.Max(running, 1e-12));
            }

            // しきい値以下の最初の極小を探す（YINのコア）
            const float thresh = 0.1f;
            int bestTau = -1;
            for (int tau = tauMin + 1; tau <= tauMax - 1; tau++)
            {
                if (cmnd[tau] < thresh && cmnd[tau] <= cmnd[tau - 1] && cmnd[tau] <= cmnd[tau + 1])
                {
                    bestTau = tau;
                    break;
                }
            }
            if (bestTau < 0) return new PitchEstimate(0, 0);

            float hz = fs / (float)bestTau;

            // 信頼度：CMNDが低いほど高信頼。0..1へクリップ。
            float conf = (float)Math.Clamp(1.0 - cmnd[bestTau], 0.0, 1.0);
            return new PitchEstimate(hz, conf);
        }
    }
}
