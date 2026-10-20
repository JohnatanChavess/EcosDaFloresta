using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Plataforma móvel (corpo cinemático). Expõe sua velocidade para que o jogador seja carregado junto.
    /// Executa antes do jogador para que a velocidade do quadro já esteja calculada.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] Vector2 travel = new Vector2(4f, 0f);
        [SerializeField] float speed = 2.2f;
        [SerializeField] float waitTime = 0.5f;

        Rigidbody2D rb;
        Vector2 origin;
        float progress;
        float direction = 1f;
        float waitTimer;

        public Vector2 Velocity { get; private set; }
        public Vector2 Travel { get => travel; set => travel = value; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = rb.position;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float length = travel.magnitude;
            if (length < 0.01f)
            {
                Velocity = Vector2.zero;
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                Velocity = Vector2.zero;
                return;
            }

            progress += direction * speed * dt / length;
            if (progress >= 1f)
            {
                progress = 1f;
                direction = -1f;
                waitTimer = waitTime;
            }
            else if (progress <= 0f)
            {
                progress = 0f;
                direction = 1f;
                waitTimer = waitTime;
            }

            Vector2 target = origin + travel * progress;
            Velocity = (target - rb.position) / dt;
            rb.MovePosition(target);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 o = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(o, o + (Vector3)travel);
        }
    }
}
