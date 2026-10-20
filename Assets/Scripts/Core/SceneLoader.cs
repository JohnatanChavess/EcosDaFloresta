using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ecos
{
    /// <summary>
    /// Troca de cenas com transição em fade. Cria automaticamente um canvas persistente para o efeito.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        static SceneLoader instance;

        Image fadeImage;
        bool loading;

        public static bool IsLoading => instance != null && instance.loading;

        public static void Load(string sceneName)
        {
            EnsureInstance();
            if (instance.loading) return;
            instance.StartCoroutine(instance.LoadRoutine(sceneName));
        }

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.BuildOverlay();
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var imageGo = new GameObject("Fade", typeof(RectTransform));
            imageGo.transform.SetParent(transform, false);
            var rt = (RectTransform)imageGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fadeImage = imageGo.AddComponent<Image>();
            fadeImage.color = new Color(0.06f, 0.05f, 0.1f, 0f);
            fadeImage.raycastTarget = false;
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            fadeImage.raycastTarget = true;
            yield return Fade(0f, 1f, 0.35f);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            yield return Fade(1f, 0f, 0.35f);
            fadeImage.raycastTarget = false;
            loading = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            var c = fadeImage.color;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(from, to, t / duration);
                fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            fadeImage.color = c;
        }
    }
}
