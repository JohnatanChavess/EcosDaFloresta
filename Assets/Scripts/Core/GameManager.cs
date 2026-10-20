using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecos
{
    /// <summary>
    /// Controla uma fase: vidas, cristais, pontuação, checkpoints, dano, pausa, Game Over e conclusão.
    /// Existe um GameManager por cena de fase.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Playing, Paused, Respawning, LevelComplete, GameOver }

        public static GameManager Instance { get; private set; }

        [Header("Fase")]
        [SerializeField] int levelNumber = 1;
        [SerializeField] string levelDisplayName = "Floresta Encantada";
        [SerializeField] string nextSceneName = GameConstants.SceneLevel2;
        [SerializeField] bool isFinalLevel;
        [SerializeField] MusicId music = MusicId.Forest;
        [SerializeField] float killHeight = -4f;

        [Header("Referências")]
        [SerializeField] PlayerController player;
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] HUDController hud;
        [SerializeField] LevelUI levelUI;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] Transform spawnPoint;

        Vector2 respawnPosition;

        public State CurrentState { get; private set; } = State.Playing;
        public int Lives { get; private set; }
        public int Crystals { get; private set; }
        public int TotalCrystals { get; private set; }
        public int LevelNumber => levelNumber;
        public string LevelDisplayName => levelDisplayName;
        public bool IsFinalLevel => isFinalLevel;
        public PlayerController Player => player;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Physics2D.IgnoreLayerCollision(GameConstants.LayerPlayer, GameConstants.LayerEnemy, true);
            Physics2D.IgnoreLayerCollision(GameConstants.LayerEnemy, GameConstants.LayerEnemy, true);
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (playerHealth == null && player != null) playerHealth = player.GetComponent<PlayerHealth>();
            if (hud == null) hud = FindAnyObjectByType<HUDController>();
            if (levelUI == null) levelUI = FindAnyObjectByType<LevelUI>();
            if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Lives = GameConstants.StartingLives;
            Crystals = 0;
            TotalCrystals = 0;
            foreach (var c in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
                if (c.Type == Collectible.Kind.Crystal) TotalCrystals++;

            GameSession.BeginLevel(levelNumber);
            SaveSystem.MarkStarted();
            respawnPosition = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)player.transform.position;

            if (hud != null)
            {
                hud.SetLevel(levelNumber, levelDisplayName);
                hud.SetLives(Lives, GameConstants.MaxLives);
                hud.SetCrystals(Crystals, TotalCrystals);
                hud.SetScore(GameSession.Score);
                hud.ShowBanner("FASE " + levelNumber, levelDisplayName);
            }
            AudioManager.PlayMusic(music);
        }

        void Update()
        {
            if (InputReader.PausePressed && !SceneLoader.IsLoading)
            {
                if (CurrentState == State.Playing) Pause();
                else if (CurrentState == State.Paused) Resume();
            }

            if (CurrentState == State.Playing && player != null && player.transform.position.y < killHeight)
                PlayerFell();
        }

        // ------------------------------------------------------------------ Pontos e itens

        public void AddCrystal(Vector3 worldPosition)
        {
            Crystals++;
            if (hud != null) hud.SetCrystals(Crystals, TotalCrystals, true);
            AddScore(GameConstants.CrystalScore, worldPosition);
        }

        public void AddScore(int amount, Vector3 worldPosition)
        {
            GameSession.Score += amount;
            if (hud != null)
            {
                hud.SetScore(GameSession.Score);
                hud.Popup("+" + amount, worldPosition);
            }
        }

        public void AddLife(Vector3 worldPosition)
        {
            if (Lives < GameConstants.MaxLives)
            {
                Lives++;
                if (hud != null)
                {
                    hud.SetLives(Lives, GameConstants.MaxLives);
                    hud.Popup("+1 VIDA", worldPosition);
                }
            }
            else
            {
                AddScore(GameConstants.HeartScore, worldPosition);
            }
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            respawnPosition = checkpoint.RespawnPoint;
            if (hud != null) hud.ShowMessage("Checkpoint ativado!");
        }

        // ------------------------------------------------------------------ Dano e morte

        public void DamagePlayer(Vector2 sourcePosition, bool respawn)
        {
            if (CurrentState != State.Playing || playerHealth == null || playerHealth.IsInvulnerable) return;
            LoseLife(sourcePosition, respawn);
        }

        void PlayerFell()
        {
            LoseLife(player.transform.position, true);
        }

        void LoseLife(Vector2 sourcePosition, bool respawn)
        {
            Lives = Mathf.Max(0, Lives - 1);
            if (hud != null)
            {
                hud.SetLives(Lives, GameConstants.MaxLives);
                hud.FlashDamage();
            }
            if (cameraFollow != null) cameraFollow.Shake(0.3f, 0.3f);

            if (Lives <= 0)
            {
                StartCoroutine(GameOverRoutine());
                return;
            }

            AudioManager.Play(SfxId.Hurt);
            if (respawn)
            {
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                player.ApplyKnockback(sourcePosition);
                playerHealth.StartInvulnerability();
            }
        }

        IEnumerator RespawnRoutine()
        {
            CurrentState = State.Respawning;
            playerHealth.SpawnDeathEffect();
            player.SetFrozen(true);
            player.Renderer.enabled = false;
            yield return new WaitForSeconds(0.8f);

            player.TeleportTo(respawnPosition);
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            player.Renderer.enabled = true;
            player.SetFrozen(false);
            playerHealth.StartInvulnerability();
            CurrentState = State.Playing;
        }

        IEnumerator GameOverRoutine()
        {
            CurrentState = State.GameOver;
            AudioManager.StopMusic(0.3f);
            AudioManager.Play(SfxId.Death);
            playerHealth.SpawnDeathEffect();
            player.SetFrozen(true);
            player.Renderer.enabled = false;
            yield return new WaitForSecondsRealtime(1.1f);
            AudioManager.Play(SfxId.GameOver, 1f, 0f);
            SaveSystem.SubmitScore(GameSession.Score);
            if (levelUI != null) levelUI.ShowGameOver(GameSession.Score, Crystals, TotalCrystals);
        }

        // ------------------------------------------------------------------ Conclusão

        public void CompleteLevel()
        {
            if (CurrentState != State.Playing) return;
            CurrentState = State.LevelComplete;
            player.SetFrozen(true);
            AudioManager.StopMusic(0.3f);

            int lifeBonus = Lives * GameConstants.LifeBonusScore;
            GameSession.Score += lifeBonus;
            if (hud != null) hud.SetScore(GameSession.Score);
            GameSession.RecordLevel(levelNumber, Crystals, TotalCrystals);
            SaveSystem.UnlockLevel(levelNumber + 1);
            SaveSystem.SubmitScore(GameSession.Score);

            if (isFinalLevel) StartCoroutine(FinalVictoryRoutine());
            else StartCoroutine(LevelCompleteRoutine(lifeBonus));
        }

        IEnumerator LevelCompleteRoutine(int lifeBonus)
        {
            AudioManager.Play(SfxId.LevelComplete, 1f, 0f);
            yield return new WaitForSecondsRealtime(1.2f);
            if (levelUI != null) levelUI.ShowLevelComplete(levelNumber, Crystals, TotalCrystals, lifeBonus, GameSession.Score);
        }

        IEnumerator FinalVictoryRoutine()
        {
            AudioManager.Play(SfxId.Victory, 1f, 0f);
            if (hud != null)
            {
                hud.FlashWhite();
                hud.ShowBanner("A FLORESTA FOI RESTAURADA!", "O Cristal Primordial voltou a brilhar");
            }
            yield return new WaitForSecondsRealtime(3.5f);
            SceneLoader.Load(GameConstants.SceneVictory);
        }

        // ------------------------------------------------------------------ Menus

        public void Pause()
        {
            if (CurrentState != State.Playing) return;
            CurrentState = State.Paused;
            Time.timeScale = 0f;
            AudioManager.Duck(true);
            if (levelUI != null) levelUI.ShowPause(true);
        }

        public void Resume()
        {
            if (CurrentState != State.Paused) return;
            CurrentState = State.Playing;
            Time.timeScale = 1f;
            AudioManager.Duck(false);
            if (levelUI != null) levelUI.ShowPause(false);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            AudioManager.Duck(false);
            GameSession.RestartLevel();
            SceneLoader.Load(SceneManager.GetActiveScene().name);
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            SceneLoader.Load(string.IsNullOrEmpty(nextSceneName) ? GameConstants.SceneVictory : nextSceneName);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            AudioManager.Duck(false);
            SceneLoader.Load(GameConstants.SceneMainMenu);
        }

        public void QuitGame()
        {
            SceneLoader.QuitGame();
        }
    }
}
