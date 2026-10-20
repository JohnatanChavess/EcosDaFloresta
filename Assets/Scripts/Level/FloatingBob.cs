using UnityEngine;

namespace Ecos
{
    /// <summary>Faz o objeto flutuar suavemente para cima e para baixo.</summary>
    public class FloatingBob : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.12f;
        [SerializeField] float frequency = 2f;

        Vector3 basePosition;
        float offset;

        void Start()
        {
            basePosition = transform.localPosition;
            offset = transform.position.x * 0.7f;
        }

        void Update()
        {
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * frequency + offset) * amplitude);
        }
    }
}
