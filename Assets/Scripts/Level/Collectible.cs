using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Item coletável: cristal de energia (pontos), coração (vida extra) ou o Cristal Primordial (fim do jogo).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        public enum Kind { Crystal, Heart, FinalCrystal }

        [SerializeField] Kind kind = Kind.Crystal;
        [SerializeField] GameObject collectEffectPrefab;

        bool collected;

        public Kind Type => kind;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag(GameConstants.PlayerTag)) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
            collected = true;

            switch (kind)
            {
                case Kind.Crystal:
                    AudioManager.Play(SfxId.Collect, 0.9f, 0.1f);
                    gm.AddCrystal(transform.position);
                    break;
                case Kind.Heart:
                    AudioManager.Play(SfxId.Heart);
                    gm.AddLife(transform.position);
                    break;
                case Kind.FinalCrystal:
                    gm.AddScore(GameConstants.FinalCrystalScore, transform.position + Vector3.up);
                    gm.CompleteLevel();
                    break;
            }

            if (collectEffectPrefab != null)
            {
                var fx = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                if (kind == Kind.FinalCrystal) fx.transform.localScale = Vector3.one * 3f;
                if (kind == Kind.Heart)
                {
                    var r = fx.GetComponentInChildren<SpriteRenderer>();
                    if (r != null) r.color = new Color(1f, 0.6f, 0.7f);
                }
            }
            Destroy(gameObject);
        }
    }
}
