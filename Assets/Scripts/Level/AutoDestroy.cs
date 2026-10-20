using UnityEngine;

namespace Ecos
{
    /// <summary>Destrói o objeto após um tempo (efeitos visuais).</summary>
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.5f;

        public float Lifetime { get => lifetime; set => lifetime = value; }

        void Start() => Destroy(gameObject, lifetime);
    }
}
