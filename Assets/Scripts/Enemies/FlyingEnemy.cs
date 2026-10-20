using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Morcego Sombrio: voa de um lado para o outro dentro de um alcance, oscilando na vertical.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FlyingEnemy : EnemyBase
    {
        [SerializeField] float range = 3f;
        [SerializeField] float speed = 2.2f;
        [SerializeField] float bobAmplitude = 0.6f;
        [SerializeField] float bobFrequency = 2.2f;

        Vector2 origin;
        float phase;
        float timeOffset;

        public float Range { get => range; set => range = value; }

        protected override void Awake()
        {
            base.Awake();
            origin = transform.position;
            timeOffset = Random.Range(0f, 10f);
        }

        void FixedUpdate()
        {
            if (IsDead || rb == null || !rb.simulated) return;
            phase += Time.fixedDeltaTime * speed / Mathf.Max(0.5f, range);
            float x = Mathf.Sin(phase) * range;
            float y = Mathf.Sin((Time.time + timeOffset) * bobFrequency) * bobAmplitude;
            Vector2 target = origin + new Vector2(x, y);
            FaceDirection(target.x - rb.position.x);
            rb.MovePosition(target);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 o = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(o + Vector3.left * range, o + Vector3.right * range);
        }
    }
}
