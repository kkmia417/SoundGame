// WHY: スコアの単一責任。明示的シングルトンで使い勝手と安全性のバランスを取る。
using UnityEngine;
using System;

namespace Game.Gameplay
{
    public sealed class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }
        public int Score { get; private set; }
        public event Action<int> OnScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Score = 0;
        }
        public void Add(int value){ Score += value; OnScoreChanged?.Invoke(Score); }
        public void ResetScore(){ Score = 0; OnScoreChanged?.Invoke(Score); }
    }
}