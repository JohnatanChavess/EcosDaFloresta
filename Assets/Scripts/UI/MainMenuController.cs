using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ecos
{
    /// <summary>
    /// Menu principal: novo jogo, continuar, seleção de fases, instruções, som e sair.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Painéis")]
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject levelSelectPanel;
        [SerializeField] GameObject howToPlayPanel;

        [Header("Menu principal")]
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button levelSelectButton;
        [SerializeField] Button howToPlayButton;
        [SerializeField] Button soundButton;
        [SerializeField] Button quitButton;
        [SerializeField] Text bestScoreText;

        [Header("Seleção de fases")]
        [SerializeField] Button[] levelButtons;
        [SerializeField] Button levelSelectBackButton;

        [Header("Como jogar")]
        [SerializeField] Button howToPlayBackButton;

        void Awake()
        {
            Bind(newGameButton, NewGame);
            Bind(continueButton, Continue);
            Bind(levelSelectButton, () => ShowPanel(levelSelectPanel));
            Bind(howToPlayButton, () => ShowPanel(howToPlayPanel));
            Bind(soundButton, ToggleSound);
            Bind(quitButton, SceneLoader.QuitGame);
            Bind(levelSelectBackButton, () => ShowPanel(mainPanel));
            Bind(howToPlayBackButton, () => ShowPanel(mainPanel));

            if (levelButtons != null)
            {
                for (int i = 0; i < levelButtons.Length; i++)
                {
                    int level = i + 1;
                    Bind(levelButtons[i], () => StartLevel(level));
                }
            }
        }

        void Start()
        {
            Time.timeScale = 1f;
            AudioManager.PlayMusic(MusicId.Menu);
            RefreshLabels();
            ShowPanel(mainPanel);
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() => AudioManager.Play(SfxId.Click, 0.8f, 0f));
            button.onClick.AddListener(action);
        }

        void RefreshLabels()
        {
            if (continueButton != null) continueButton.interactable = SaveSystem.HasSave;
            if (bestScoreText != null) bestScoreText.text = "RECORDE: " + SaveSystem.BestScore.ToString("000000");
            UpdateSoundLabel();

            if (levelButtons == null) return;
            int unlocked = SaveSystem.UnlockedLevel;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                bool open = i + 1 <= unlocked;
                levelButtons[i].interactable = open;
                var label = levelButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    string name = i < GameConstants.LevelNames.Length ? GameConstants.LevelNames[i] : "";
                    string difficulty = i < GameConstants.LevelDifficulty.Length ? GameConstants.LevelDifficulty[i] : "";
                    label.text = open
                        ? "FASE " + (i + 1) + " - " + name.ToUpperInvariant() + "\n<size=18>Dificuldade: " + difficulty + "</size>"
                        : "FASE " + (i + 1) + " - BLOQUEADA\n<size=18>Conclua a fase anterior</size>";
                }
            }
        }

        void ShowPanel(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(panel == levelSelectPanel);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(panel == howToPlayPanel);
            RefreshLabels();

            Button first = null;
            if (panel == mainPanel) first = newGameButton;
            else if (panel == levelSelectPanel && levelButtons != null && levelButtons.Length > 0) first = levelButtons[0];
            else if (panel == howToPlayPanel) first = howToPlayBackButton;
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void NewGame()
        {
            GameSession.Reset();
            SceneLoader.Load(GameConstants.SceneLevel1);
        }

        void Continue()
        {
            StartLevel(SaveSystem.UnlockedLevel);
        }

        void StartLevel(int level)
        {
            level = Mathf.Clamp(level, 1, GameConstants.LevelCount);
            GameSession.Reset();
            SceneLoader.Load(GameConstants.LevelScenes[level - 1]);
        }

        void ToggleSound()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
            UpdateSoundLabel();
        }

        void UpdateSoundLabel()
        {
            if (soundButton == null) return;
            var label = soundButton.GetComponentInChildren<Text>();
            bool muted = AudioManager.Instance != null && AudioManager.Instance.Muted;
            if (label != null) label.text = muted ? "SOM: DESLIGADO" : "SOM: LIGADO";
        }
    }
}
