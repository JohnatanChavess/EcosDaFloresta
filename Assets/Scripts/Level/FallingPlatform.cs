using System.Collections;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Bloco rachado das ruínas: treme quando o jogador pisa, cai e depois reaparece.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class FallingPlatform : MonoBehaviour
    {
        [SerializeField] float shakeTime = 0.45f;
        [SerializeField] float respawnTime = 3f;
        [SerializeField] float fallGravity = 3f;
        [SerializeField] SpriteRenderer spriteRenderer;

        Rigidbody2D rb;
        Collider2D col;
        Vector3 startPosition;
        bool triggered;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            startPosition = transform.position;
        }

        void OnCollisionEnter2D(Collision2D collision) => TryTrigger(collision);
        void OnCollisionStay2D(Collision2D collision) => TryTrigger(collision);

        void TryTrigger(Collision2D collision)
        {
            if (triggered || !collision.collider.CompareTag(GameConstants.PlayerTag)) return;
            if (collision.transform.position.y < col.bounds.max.y - 0.1f) return;
            StartCoroutine(Crumble());
        }

        IEnumerator Crumble()
        {
            triggered = true;
            AudioManager.Play(SfxId.Crumble, 0.7f, 0.15f);
            var visual = spriteRenderer.transform;
            float t = 0f;
            while (t < shakeTime)
            {
                t += Time.deltaTime;
                visual.localPosition = (Vector3)(Random.insideUnitCircle * 0.06f);
                yield return null;
            }
            visual.localPosition = Vector3.zero;

            col.enabled = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravity;
            yield return FadeTo(0f, 0.8f);

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(respawnTime);

            rb.position = startPosition;
            transform.position = startPosition;
            col.enabled = true;
            yield return FadeTo(1f, 0.3f);
            triggered = false;
        }

        IEnumerator FadeTo(float alpha, float duration)
        {
            Color c = spriteRenderer.color;
            float start = c.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(start, alpha, t / duration);
                spriteRenderer.color = c;
                yield return null;
            }
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
