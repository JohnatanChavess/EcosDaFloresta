namespace Ecos
{
    /// <summary>
    /// Dados da partida atual que precisam sobreviver à troca de cenas (pontuação e cristais por fase).
    /// </summary>
    public static class GameSession
    {
        public static int Score;
        public static int ScoreAtLevelStart;
        public static int CurrentLevel = 1;
        public static readonly int[] CrystalsCollected = new int[GameConstants.LevelCount + 1];
        public static readonly int[] CrystalsAvailable = new int[GameConstants.LevelCount + 1];

        public static void Reset()
        {
            Score = 0;
            ScoreAtLevelStart = 0;
            CurrentLevel = 1;
            for (int i = 0; i < CrystalsCollected.Length; i++)
            {
                CrystalsCollected[i] = 0;
                CrystalsAvailable[i] = 0;
            }
        }

        public static void BeginLevel(int level)
        {
            CurrentLevel = level;
            ScoreAtLevelStart = Score;
        }

        public static void RestartLevel()
        {
            Score = ScoreAtLevelStart;
        }

        public static void RecordLevel(int level, int collected, int available)
        {
            if (level < 1 || level >= CrystalsCollected.Length) return;
            CrystalsCollected[level] = collected;
            CrystalsAvailable[level] = available;
        }

        public static int TotalCrystalsCollected
        {
            get
            {
                int total = 0;
                foreach (int c in CrystalsCollected) total += c;
                return total;
            }
        }

        public static int TotalCrystalsAvailable
        {
            get
            {
                int total = 0;
                foreach (int c in CrystalsAvailable) total += c;
                return total;
            }
        }
    }
}
