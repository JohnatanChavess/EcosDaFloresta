using UnityEngine;

namespace Ecos
{
    /// <summary>Pulsa a escala suavemente (Cristal Primordial, telas de menu).</summary>
    public class PulseGlow : MonoBehaviour
    {
        [SerializeField] float scaleAmount = 0.06f;
        [SerializeField] float speed = 3f;

        Vector3 baseScale;

        void Start() => baseScale = transform.localScale;

        void Update()
        {
            transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * speed) * scaleAmount);
        }
    }
}
