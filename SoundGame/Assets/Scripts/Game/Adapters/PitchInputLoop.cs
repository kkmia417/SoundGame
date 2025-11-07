using UnityEngine;
using Game.Core;

namespace Game.Adapters
{
    /// <summary>
    /// Pulls a fixed-size audio frame, runs pitch detection and mapping in Update,
    /// then exposes an atomic snapshot for consumers (physics/UI).
    /// </summary>
    public sealed class PitchInputLoop : MonoBehaviour
    {
        [SerializeField] private RuntimeComposer _composer;
        [SerializeField] private int _frameSamples = 2048; // tune with latency budget

        private float[] _frame;
        private volatile PitchFrame? _latest;

        private void Awake()
        {
            _frame = new float[_frameSamples];
            _composer.Mic.Start();
        }

        private void OnDestroy()
        {
            _composer.Mic.Stop();
        }

        private void Update()
        {
            _composer.Mic.Pump();

            if (_composer.Mic.TryDequeueFrame(_frame))
            {
                var est = _composer.Detector.Estimate(_frame);
                float height = _composer.Mapper.Map(est.Hz, est.Confidence, Time.timeAsDouble);
                _latest = new PitchFrame(est.Hz, est.Confidence, height, Time.timeAsDouble);
            }
        }

        public bool TryGetSnapshot(out PitchFrame frame)
        {
            var v = _latest;
            if (v.HasValue) { frame = v.Value; return true; }
            frame = default;
            return false;
        }
    }
}