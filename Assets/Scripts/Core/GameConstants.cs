namespace Ecos
{
    /// <summary>
    /// Valores fixos compartilhados por todo o jogo: nomes de cenas, camadas, pontuação e ordem de desenho.
    /// </summary>
    public static class GameConstants
    {
        public const string SceneMainMenu = "MainMenu";
        public const string SceneLevel1 = "Level1";
        public const string SceneLevel2 = "Level2";
        public const string SceneLevel3 = "Level3";
        public const string SceneVictory = "Victory";

        public static readonly string[] LevelScenes = { SceneLevel1, SceneLevel2, SceneLevel3 };
        public static readonly string[] LevelNames = { "Floresta Encantada", "Cavernas Luminosas", "Ruínas da Energia" };
        public static readonly string[] LevelDifficulty = { "Fácil", "Média", "Difícil" };
        public const int LevelCount = 3;

        public const int LayerGround = 6;
        public const int LayerPlayer = 7;
        public const int LayerEnemy = 8;

        public const string PlayerTag = "Player";

        public const int StartingLives = 3;
        public const int MaxLives = 3;
        public const int CrystalScore = 100;
        public const int EnemyScore = 150;
        public const int HeartScore = 50;
        public const int FinalCrystalScore = 1000;
        public const int LifeBonusScore = 250;
    }

    /// <summary>Ordem de desenho (sortingOrder) dos elementos 2D.</summary>
    public static class SortOrder
    {
        public const int Sky = -100;
        public const int FarBackground = -90;
        public const int MidBackground = -80;
        public const int DecorationBack = -20;
        public const int Ladders = -5;
        public const int Ground = 0;
        public const int Platforms = 1;
        public const int Hazards = 4;
        public const int Items = 5;
        public const int Enemies = 8;
        public const int Player = 10;
        public const int DecorationFront = 15;
        public const int Effects = 20;
    }
}
