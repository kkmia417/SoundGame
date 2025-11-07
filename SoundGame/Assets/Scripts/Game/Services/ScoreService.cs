using System;
using Game.Core;

namespace Game.Services
{
    public sealed class ScoreService : IScoreService
    {
        public int CurrentScore { get; private set; }
        public event Action<int> OnScoreChanged;

        public void Reset() => Set(0);
        public void Add(int pts) => Set(CurrentScore + pts);

        private void Set(int value)
        {
            if (value == CurrentScore) return;
            CurrentScore = value;
            OnScoreChanged?.Invoke(CurrentScore);
        }
    }
}