using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Inimigo terrestre que patrulha automaticamente: vira ao encontrar uma parede ou a borda da plataforma.
    /// Usado pelo Lodo Sombrio (lento, pode ser pisado) e pelo Espinhento (rápido, não pode ser pisado).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolEnemy : EnemyBase
    {
        [SerializeField] float speed = 1.6f;
        [SerializeField] bool startMovingRight;
        [SerializeField] LayerMask groundMask = 1 << GameConstants.LayerGround;
        [SerializeField] float turnCooldown = 0.25f;

        float direction;
        float lastTurnTime = -10f;

        protected override void Awake()
        {
            base.Awake();
            direction = startMovingRight ? 1f : -1f;
            FaceDirection(direction);
        }

        void FixedUpdate()
        {
            if (IsDead || rb == null || !rb.simulated) return;

            var bounds = col.bounds;
            bool grounded = Physics2D.Raycast(new Vector2(bounds.center.x, bounds.min.y + 0.05f), Vector2.down, 0.15f, groundMask);

            if (grounded && Time.time - lastTurnTime > turnCooldown)
            {
                Vector2 front = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.02f), bounds.center.y);
                bool wall = Physics2D.Raycast(front, Vector2.right * direction, 0.1f, groundMask);
                Vector2 ledgeOrigin = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.1f), bounds.min.y + 0.1f);
                bool floorAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, 0.6f, groundMask);
                if (wall || !floorAhead)
                {
                    direction = -direction;
                    lastTurnTime = Time.time;
                    FaceDirection(direction);
                }
            }

            rb.linearVelocity = new Vector2(grounded ? direction * speed : 0f, rb.linearVelocity.y);
        }
    }
}
