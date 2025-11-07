namespace Game.Core
{
    public interface IScoreService
    {
        int CurrentScore { get; }
        void Reset();
        void Add(int pts);
        event System.Action<int> OnScoreChanged;
    }
}