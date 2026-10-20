using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Move o objeto em vai-e-vem entre a posição inicial e (inicial + deslocamento).
    /// Usado pelas esferas de espinhos móveis.
    /// </summary>
    public class PathMover : MonoBehaviour
    {
        [SerializeField] Vector2 travel = new Vector2(4f, 0f);
        [SerializeField] float speed = 2.5f;
        [SerializeField, Range(0f, 1f)] float startOffset;

        Rigidbody2D rb;
        Vector2 origin;
        float progress;

        public Vector2 Travel { get => travel; set => travel = value; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            origin = transform.position;
            progress = startOffset * 2f;
        }

        void FixedUpdate()
        {
            float length = travel.magnitude;
            if (length < 0.01f) return;
            progress += Time.fixedDeltaTime * speed / length;
            float k = Mathf.PingPong(progress, 1f);
            float eased = k * k * (3f - 2f * k);
            Vector2 target = origin + travel * eased;
            if (rb != null) rb.MovePosition(target);
            else transform.position = target;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 o = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(o, o + (Vector3)travel);
        }
    }
}
