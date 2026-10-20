using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Câmera lateral que segue o jogador com suavização, antecipação na direção do movimento,
    /// limites da fase e tremor de tela.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.6f);
        [SerializeField] float smoothTimeX = 0.12f;
        [SerializeField] float smoothTimeY = 0.2f;
        [SerializeField] float lookAhead = 2f;
        [SerializeField] float lookAheadSpeed = 3f;

        [Header("Limites da fase")]
        [SerializeField] bool useBounds = true;
        [SerializeField] Vector2 boundsMin = new Vector2(0f, 0f);
        [SerializeField] Vector2 boundsMax = new Vector2(100f, 20f);

        Camera cam;
        PlayerController player;
        float velocityX;
        float velocityY;
        float currentLookAhead;
        float shakeTime;
        float shakeDuration;
        float shakeMagnitude;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                player = target != null ? target.GetComponent<PlayerController>() : null;
            }
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            boundsMin = min;
            boundsMax = max;
            useBounds = true;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target != null) player = target.GetComponent<PlayerController>();
        }

        void Start() => SnapToTarget();

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 p = Clamp((Vector2)target.position + offset);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            velocityX = velocityY = 0f;
        }

        void LateUpdate()
        {
            if (target == null) return;

            float facing = player != null ? (player.FacingRight ? 1f : -1f) : 1f;
            currentLookAhead = Mathf.MoveTowards(currentLookAhead, facing * lookAhead, lookAheadSpeed * Time.deltaTime);

            Vector2 desired = (Vector2)target.position + offset + new Vector2(currentLookAhead, 0f);
            desired = Clamp(desired);

            float x = Mathf.SmoothDamp(transform.position.x, desired.x, ref velocityX, smoothTimeX);
            float y = Mathf.SmoothDamp(transform.position.y, desired.y, ref velocityY, smoothTimeY);
            Vector2 pos = Clamp(new Vector2(x, y));

            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float strength = shakeMagnitude * Mathf.Clamp01(shakeTime / shakeDuration);
                pos += Random.insideUnitCircle * strength;
            }

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        Vector2 Clamp(Vector2 p)
        {
            if (!useBounds || cam == null) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float minX = boundsMin.x + halfW, maxX = boundsMax.x - halfW;
            float minY = boundsMin.y + halfH, maxY = boundsMax.y - halfH;
            p.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? boundsMin.y + halfH : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        public void Shake(float duration = 0.25f, float magnitude = 0.25f)
        {
            shakeDuration = duration;
            shakeTime = duration;
            shakeMagnitude = magnitude;
        }
    }
}
