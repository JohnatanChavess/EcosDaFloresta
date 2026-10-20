using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ecos
{
    /// <summary>
    /// HUD da fase: vidas (corações), cristais, pontuação, nome da fase, dicas, faixa de título,
    /// textos flutuantes de pontos e flash de dano.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Vidas")]
        [SerializeField] Image[] hearts;
        [SerializeField] Sprite heartFull;
        [SerializeField] Sprite heartEmpty;

        [Header("Textos")]
        [SerializeField] Text crystalText;
        [SerializeField] RectTransform crystalIcon;
        [SerializeField] Text scoreText;
        [SerializeField] Text levelText;

        [Header("Dicas e mensagens")]
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField] Text hintText;
        [SerializeField] CanvasGroup bannerGroup;
        [SerializeField] Text bannerTitle;
        [SerializeField] Text bannerSubtitle;

        [Header("Feedback")]
        [SerializeField] Image damageFlash;
        [SerializeField] Text popupTemplate;
        [SerializeField] RectTransform popupContainer;

        Object hintOwner;
        Coroutine bannerRoutine;
        Coroutine flashRoutine;
        Coroutine punchRoutine;
        Coroutine messageRoutine;
        int displayedScore;
        int targetScore;
        Canvas canvas;

        void Awake()
        {
            Instance = this;
            canvas = GetComponentInParent<Canvas>();
            if (hintGroup != null) hintGroup.alpha = 0f;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
            if (damageFlash != null) damageFlash.color = new Color(1f, 0.15f, 0.2f, 0f);
            if (popupTemplate != null) popupTemplate.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (displayedScore != targetScore)
            {
                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(targetScore - displayedScore) * 10f * Time.unscaledDeltaTime));
                displayedScore = (int)Mathf.MoveTowards(displayedScore, targetScore, step);
                if (scoreText != null) scoreText.text = displayedScore.ToString("000000");
            }
        }

        public void SetLevel(int number, string name)
        {
            if (levelText != null) levelText.text = "FASE " + number + "  •  " + name.ToUpperInvariant();
        }

        public void SetLives(int lives, int max)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < lives ? heartFull : heartEmpty;
            }
        }

        public void SetCrystals(int collected, int total, bool animate = false)
        {
            if (crystalText != null) crystalText.text = collected + " / " + total;
            if (animate && crystalIcon != null)
            {
                if (punchRoutine != null) StopCoroutine(punchRoutine);
                punchRoutine = StartCoroutine(Punch(crystalIcon));
            }
        }

        public void SetScore(int score)
        {
            targetScore = score;
            if (displayedScore > score || Time.timeSinceLevelLoad < 0.1f)
            {
                displayedScore = score;
                if (scoreText != null) scoreText.text = score.ToString("000000");
            }
        }

        // ------------------------------------------------------------------ Dicas

        public void ShowHint(string message, Object owner)
        {
            hintOwner = owner;
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
                messageRoutine = null;
            }
            if (hintText != null) hintText.text = message;
            StartCoroutine(FadeGroup(hintGroup, 1f, 0.2f));
        }

        public void HideHint(Object owner)
        {
            if (owner != hintOwner) return;
            hintOwner = null;
            StartCoroutine(FadeGroup(hintGroup, 0f, 0.3f));
        }

        /// <summary>Mensagem curta temporária na área de dicas.</summary>
        public void ShowMessage(string message, float duration = 1.8f)
        {
            if (hintOwner != null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        IEnumerator MessageRoutine(string message, float duration)
        {
            if (hintText != null) hintText.text = message;
            yield return FadeGroup(hintGroup, 1f, 0.15f);
            yield return new WaitForSeconds(duration);
            yield return FadeGroup(hintGroup, 0f, 0.3f);
            messageRoutine = null;
        }

        // ------------------------------------------------------------------ Faixa de título

        public void ShowBanner(string title, string subtitle)
        {
            if (bannerGroup == null) return;
            if (bannerTitle != null) bannerTitle.text = title;
            if (bannerSubtitle != null) bannerSubtitle.text = subtitle;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine());
        }

        IEnumerator BannerRoutine()
        {
            yield return FadeGroup(bannerGroup, 1f, 0.35f);
            yield return new WaitForSecondsRealtime(2.2f);
            yield return FadeGroup(bannerGroup, 0f, 0.6f);
            bannerRoutine = null;
        }

        // ------------------------------------------------------------------ Feedback

        public void FlashDamage() => Flash(new Color(1f, 0.15f, 0.2f, 0.45f), 0.45f);

        public void FlashWhite() => Flash(new Color(1f, 1f, 0.9f, 0.85f), 1.2f);

        void Flash(Color color, float duration)
        {
            if (damageFlash == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        IEnumerator FlashRoutine(Color color, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var c = color;
                c.a = Mathf.Lerp(color.a, 0f, t / duration);
                damageFlash.color = c;
                yield return null;
            }
            damageFlash.color = new Color(color.r, color.g, color.b, 0f);
            flashRoutine = null;
        }

        /// <summary>Texto flutuante (ex.: "+100") na posição do mundo informada.</summary>
        public void Popup(string text, Vector3 worldPosition)
        {
            if (popupTemplate == null || popupContainer == null || Camera.main == null) return;
            Vector2 screen = Camera.main.WorldToScreenPoint(worldPosition);
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(popupContainer, screen, cam, out Vector2 local)) return;
            var popup = Instantiate(popupTemplate, popupContainer);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.rectTransform.anchoredPosition = local;
            StartCoroutine(PopupRoutine(popup));
        }

        IEnumerator PopupRoutine(Text popup)
        {
            var rt = popup.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Color c = popup.color;
            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                float k = t / 0.9f;
                rt.anchoredPosition = start + new Vector2(0f, 50f * k);
                c.a = 1f - k * k;
                popup.color = c;
                rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(Mathf.Min(1f, k * 4f) * Mathf.PI));
                yield return null;
            }
            Destroy(popup.gameObject);
        }

        IEnumerator Punch(RectTransform target)
        {
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.45f * Mathf.Sin(t / 0.25f * Mathf.PI);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
            punchRoutine = null;
        }

        static IEnumerator FadeGroup(CanvasGroup group, float to, float duration)
        {
            if (group == null) yield break;
            float from = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
