// WHY: Updateループを1か所に集中 → 他は“読み取り専用”で疎結合。デバッグ容易。
using UnityEngine;
using Game.Core;

namespace Game.Adapters
{
    public sealed class PitchInputLoop : MonoBehaviour
    {
        [SerializeField] private RuntimeComposer _ctx;

        public float CurrentHz { get; private set; }
        public float CurrentConfidence { get; private set; }
        public float? CurrentHeight { get; private set; }

        private void Update()
        {
            if (_ctx == null || _ctx.Mic == null || _ctx.Frame == null || _ctx.Detector == null || _ctx.Mapper == null) return;
            if (_ctx.Mic.TryGetFrame(_ctx.Frame, out var _))
            {
                // min/maxHz はMapperがClampしますが、Detector探索範囲としてComposer設定を使うのが堅実
                var est = _ctx.Detector.Estimate(_ctx.Frame, _ctx.SampleRate, _ctx is null ? 80f : 80f, _ctx is null ? 800f : 800f);
                CurrentHz = est.Hz; CurrentConfidence = est.Confidence;
                if (_ctx.Mapper.TryMap(est, out var h)) CurrentHeight = h; else CurrentHeight = null;
            }
        }
    }
}