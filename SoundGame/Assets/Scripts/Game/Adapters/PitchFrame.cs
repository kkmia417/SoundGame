namespace Game.Adapters
{
    public readonly struct PitchFrame
    {
        public readonly float Hz;
        public readonly float Confidence;
        public readonly float Height;
        public readonly double Time;

        public PitchFrame(float hz, float confidence, float height, double time)
        {
            Hz = hz;
            Confidence = confidence;
            Height = height;
            Time = time;
        }
    }
}