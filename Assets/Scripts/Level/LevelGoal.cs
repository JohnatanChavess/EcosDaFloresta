using System.Collections;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Portal mágico no fim das fases 1 e 2. Ao entrar, o Lumo é absorvido e a fase é concluída.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        bool used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || !other.CompareTag(GameConstants.PlayerTag)) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
            used = true;
            gm.CompleteLevel();
            var player = other.GetComponent<PlayerController>();
            if (player != null) StartCoroutine(Absorb(player));
        }

        IEnumerator Absorb(PlayerController player)
        {
            var visual = player.Renderer != null ? player.Renderer.transform : player.transform;
            Vector3 startScale = visual.localScale;
            Vector3 startPos = player.transform.position;
            Vector3 target = transform.position + Vector3.up * 0.9f;
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.7f);
                player.transform.position = Vector3.Lerp(startPos, target, k);
                visual.localScale = startScale * (1f - k);
                visual.Rotate(0f, 0f, 720f * Time.unscaledDeltaTime);
                yield return null;
            }
            visual.localScale = Vector3.zero;
        }
    }
}
