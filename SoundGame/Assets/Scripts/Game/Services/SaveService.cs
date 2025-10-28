// WHY: セーブをUI/Goalから分離。MVPはPlayerPrefsで十分。APIを固定し将来差し替え容易に。
using UnityEngine;

namespace Game.Services
{
    public static class SaveService
    {
        private const string KeyBest = "BEST_SCORE";
        public static int LoadBest() => PlayerPrefs.GetInt(KeyBest, 0);
        public static void SaveBest(int score)
        {
            var best = LoadBest();
            if (score > best){ PlayerPrefs.SetInt(KeyBest, score); PlayerPrefs.Save(); }
        }
    }
}