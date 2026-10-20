using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ecos
{
    /// <summary>
    /// Painéis da fase: pausa, Game Over (derrota) e fase concluída.
    /// Os botões são ligados ao GameManager da cena.
    /// </summary>
    public class LevelUI : MonoBehaviour
    {
        [Header("Pausa")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button pauseRestartButton;
        [SerializeField] Button pauseSoundButton;
        [SerializeField] Button pauseMenuButton;
        [SerializeField] Button pauseQuitButton;

        [Header("Derrota")]
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] Text gameOverStats;
        [SerializeField] Button retryButton;
        [SerializeField] Button gameOverMenuButton;
        [SerializeField] Button gameOverQuitButton;

        [Header("Fase concluída")]
        [SerializeField] GameObject levelCompletePanel;
        [SerializeField] Text levelCompleteTitle;
        [SerializeField] Text levelCompleteStats;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button levelCompleteMenuButton;

        void Awake()
        {
            Bind(resumeButton, () => GameManager.Instance?.Resume());
            Bind(pauseRestartButton, () => GameManager.Instance?.RestartLevel());
            Bind(pauseSoundButton, ToggleSound);
            Bind(pauseMenuButton, () => GameManager.Instance?.GoToMainMenu());
            Bind(pauseQuitButton, () => GameManager.Instance?.QuitGame());
            Bind(retryButton, () => GameManager.Instance?.RestartLevel());
            Bind(gameOverMenuButton, () => GameManager.Instance?.GoToMainMenu());
            Bind(gameOverQuitButton, () => GameManager.Instance?.QuitGame());
            Bind(nextLevelButton, () => GameManager.Instance?.NextLevel());
            Bind(levelCompleteMenuButton, () => GameManager.Instance?.GoToMainMenu());

            SetActive(pausePanel, false);
            SetActive(gameOverPanel, false);
            SetActive(levelCompletePanel, false);
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() => AudioManager.Play(SfxId.Click, 0.8f, 0f));
            button.onClick.AddListener(action);
        }

        static void SetActive(GameObject go, bool value)
        {
            if (go != null) go.SetActive(value);
        }

        void ToggleSound()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
            UpdateSoundLabel();
        }

        void UpdateSoundLabel()
        {
            if (pauseSoundButton == null) return;
            var label = pauseSoundButton.GetComponentInChildren<Text>();
            bool muted = AudioManager.Instance != null && AudioManager.Instance.Muted;
            if (label != null) label.text = muted ? "SOM: DESLIGADO" : "SOM: LIGADO";
        }

        public void ShowPause(bool show)
        {
            SetActive(pausePanel, show);
            if (show)
            {
                UpdateSoundLabel();
                Select(resumeButton);
            }
        }

        public void ShowGameOver(int score, int crystals, int totalCrystals)
        {
            SetActive(gameOverPanel, true);
            if (gameOverStats != null)
                gameOverStats.text = "Pontuação: " + score + "\nCristais nesta fase: " + crystals + " / " + totalCrystals +
                                     "\nRecorde: " + SaveSystem.BestScore;
            Select(retryButton);
        }

        public void ShowLevelComplete(int level, int crystals, int totalCrystals, int lifeBonus, int score)
        {
            SetActive(levelCompletePanel, true);
            if (levelCompleteTitle != null) levelCompleteTitle.text = "FASE " + level + " CONCLUÍDA!";
            if (levelCompleteStats != null)
            {
                string perfect = crystals >= totalCrystals && totalCrystals > 0 ? "\n<color=#7CF2FF>Todos os cristais coletados!</color>" : "";
                levelCompleteStats.text = "Cristais: " + crystals + " / " + totalCrystals +
                                          "\nBônus de vidas: +" + lifeBonus +
                                          "\nPontuação total: " + score + perfect;
            }
            Select(nextLevelButton);
        }

        static void Select(Button button)
        {
            if (button != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
