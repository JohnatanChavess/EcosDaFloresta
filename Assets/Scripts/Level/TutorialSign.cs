using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Placa de dica: mostra uma mensagem no HUD enquanto o jogador está perto.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TutorialSign : MonoBehaviour
    {
        [SerializeField, TextArea] string message = "Dica";

        public string Message { get => message; set => message = value; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(GameConstants.PlayerTag)) return;
            var hud = HUDController.Instance;
            if (hud != null) hud.ShowHint(message, this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(GameConstants.PlayerTag)) return;
            var hud = HUDController.Instance;
            if (hud != null) hud.HideHint(this);
        }
    }
}
