using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ecos
{
    /// <summary>
    /// Tela de vitória final: mostra pontuação, cristais coletados e recorde.
    /// </summary>
    public class VictoryController : MonoBehaviour
    {
        [SerializeField] Text statsText;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button menuButton;
        [SerializeField] Button quitButton;

        void Awake()
        {
            Bind(playAgainButton, () =>
            {
                GameSession.Reset();
                SceneLoader.Load(GameConstants.SceneLevel1);
            });
            Bind(menuButton, () => SceneLoader.Load(GameConstants.SceneMainMenu));
            Bind(quitButton, SceneLoader.QuitGame);
        }

        void Start()
        {
            Time.timeScale = 1f;
            AudioManager.PlayMusic(MusicId.Victory);
            bool newRecord = SaveSystem.SubmitScore(GameSession.Score);
            if (statsText != null)
            {
                string crystals = GameSession.TotalCrystalsAvailable > 0
                    ? GameSession.TotalCrystalsCollected + " / " + GameSession.TotalCrystalsAvailable
                    : GameSession.TotalCrystalsCollected.ToString();
                statsText.text = "Pontuação final: " + GameSession.Score +
                                 "\nCristais de energia: " + crystals +
                                 "\nRecorde: " + SaveSystem.BestScore +
                                 (newRecord ? "\n<color=#FFE66D>NOVO RECORDE!</color>" : "");
            }
            if (playAgainButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(playAgainButton.gameObject);
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() => AudioManager.Play(SfxId.Click, 0.8f, 0f));
            button.onClick.AddListener(action);
        }
    }
}
