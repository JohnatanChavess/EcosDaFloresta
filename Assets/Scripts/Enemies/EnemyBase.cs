using System.Collections;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Base dos inimigos: guarda se pode ser derrotado com um pisão, a pontuação e o efeito de morte.
    /// O dano ao jogador é detectado pelo PlayerHealth (camada Enemy).
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] bool canBeStomped = true;
        [SerializeField] int scoreValue = GameConstants.EnemyScore;
        [SerializeField] GameObject deathEffectPrefab;
        [SerializeField] protected SpriteRenderer spriteRenderer;

        protected Rigidbody2D rb;
        protected Collider2D col;

        public bool IsDead { get; private set; }
        public bool CanBeStomped => canBeStomped;
        public float StompHeight => col != null ? col.bounds.center.y : transform.position.y;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public virtual void Stomp()
        {
            if (IsDead) return;
            IsDead = true;
            AudioManager.Play(SfxId.Stomp);
            if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue, transform.position + Vector3.up);
            if (deathEffectPrefab != null)
            {
                var fx = Instantiate(deathEffectPrefab, col != null ? col.bounds.center : transform.position, Quaternion.identity);
                var fxRenderer = fx.GetComponentInChildren<SpriteRenderer>();
                if (fxRenderer != null) fxRenderer.color = new Color(0.8f, 0.6f, 1f);
            }
            if (col != null) col.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            StartCoroutine(SquashAndDie());
        }

        IEnumerator SquashAndDie()
        {
            var t = spriteRenderer != null ? spriteRenderer.transform : transform;
            Vector3 start = t.localScale;
            float time = 0f;
            while (time < 0.18f)
            {
                time += Time.deltaTime;
                float k = time / 0.18f;
                t.localScale = new Vector3(start.x * (1f + 0.4f * k), start.y * (1f - 0.75f * k), start.z);
                yield return null;
            }
            Destroy(gameObject);
        }

        protected void FaceDirection(float dir)
        {
            if (spriteRenderer != null && Mathf.Abs(dir) > 0.01f) spriteRenderer.flipX = dir < 0f;
        }
    }
}
