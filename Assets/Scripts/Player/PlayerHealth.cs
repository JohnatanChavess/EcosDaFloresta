using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Detecta contato com inimigos (dano ou "pisão") e perigos, e controla a invulnerabilidade
    /// temporária com efeito de piscar. A contagem de vidas fica no GameManager.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float invulnerabilityDuration = 1.6f;
        [SerializeField] float blinkInterval = 0.08f;
        [SerializeField] float stompBounceVelocity = 13f;
        [SerializeField] LayerMask enemyMask = 1 << GameConstants.LayerEnemy;
        [SerializeField] Vector2 hurtboxSize = new Vector2(0.56f, 0.82f);
        [SerializeField] GameObject deathEffectPrefab;

        PlayerController controller;
        SpriteRenderer spriteRenderer;
        float invulnerableUntil;
        readonly List<Collider2D> hits = new List<Collider2D>(8);
        ContactFilter2D enemyFilter;

        public bool IsInvulnerable => Time.time < invulnerableUntil;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            spriteRenderer = controller.Renderer != null ? controller.Renderer : GetComponentInChildren<SpriteRenderer>();
            enemyFilter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = enemyMask };
        }

        void Update()
        {
            if (spriteRenderer == null) return;
            var c = spriteRenderer.color;
            c.a = IsInvulnerable && Mathf.Repeat(Time.time, blinkInterval * 2f) < blinkInterval ? 0.25f : 1f;
            spriteRenderer.color = c;
        }

        void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
            CheckEnemyContacts(gm);
        }

        void CheckEnemyContacts(GameManager gm)
        {
            hits.Clear();
            Vector2 center = (Vector2)transform.position + new Vector2(0f, hurtboxSize.y * 0.5f + 0.04f);
            Physics2D.OverlapBox(center, hurtboxSize, 0f, enemyFilter, hits);
            var body = controller.Body;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                bool falling = body.linearVelocity.y < 0.5f;
                bool above = transform.position.y > enemy.StompHeight;
                if (enemy.CanBeStomped && falling && above)
                {
                    enemy.Stomp();
                    controller.Bounce(stompBounceVelocity);
                    return;
                }
                gm.DamagePlayer(enemy.transform.position, false);
                return;
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var hazard = other.GetComponentInParent<Hazard>();
            if (hazard == null || !hazard.isActiveAndEnabled) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.DamagePlayer(other.bounds.center, hazard.RespawnOnHit);
        }

        public void StartInvulnerability() => invulnerableUntil = Time.time + invulnerabilityDuration;

        public void ClearInvulnerability() => invulnerableUntil = 0f;

        public void SpawnDeathEffect()
        {
            if (deathEffectPrefab != null)
                Instantiate(deathEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, hurtboxSize.y * 0.5f + 0.04f), hurtboxSize);
        }
    }
}
