using UnityEngine;

namespace Ecos
{
    /// <summary>Gira o objeto continuamente (esferas de espinhos).</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = -360f;

        void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
