using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Perigo que causa dano ao toque (espinhos, esferas de espinhos).
    /// Se RespawnOnHit estiver ativo, o jogador volta ao último checkpoint (usado em fossos de espinhos).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] bool respawnOnHit;

        public bool RespawnOnHit
        {
            get => respawnOnHit;
            set => respawnOnHit = value;
        }
    }
}
