using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Salvamento simples de progresso com PlayerPrefs: fases liberadas, recorde e preferência de som.
    /// </summary>
    public static class SaveSystem
    {
        const string UnlockedKey = "ecos_unlocked_level";
        const string BestScoreKey = "ecos_best_score";
        const string MutedKey = "ecos_muted";
        const string HasSaveKey = "ecos_has_save";

        public static int UnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, GameConstants.LevelCount);
        public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public static bool HasSave => PlayerPrefs.GetInt(HasSaveKey, 0) == 1;

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void MarkStarted()
        {
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.Save();
        }

        public static void UnlockLevel(int level)
        {
            level = Mathf.Clamp(level, 1, GameConstants.LevelCount);
            if (level > UnlockedLevel) PlayerPrefs.SetInt(UnlockedKey, level);
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Grava o recorde se a pontuação for maior. Retorna true se for um novo recorde.</summary>
        public static bool SubmitScore(int score)
        {
            if (score <= BestScore) return false;
            PlayerPrefs.SetInt(BestScoreKey, score);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockedKey);
            PlayerPrefs.DeleteKey(HasSaveKey);
            PlayerPrefs.Save();
        }
    }
}
