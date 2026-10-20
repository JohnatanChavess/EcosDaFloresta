using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Totem de checkpoint: ao ser tocado, acende e passa a ser o ponto de reaparecimento.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        static readonly int ActiveHash = Animator.StringToHash("Active");

        [SerializeField] Animator animator;
        [SerializeField] GameObject activateEffectPrefab;

        public bool IsActive { get; private set; }
        public Vector2 RespawnPoint => (Vector2)transform.position + new Vector2(0f, 0.05f);

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive || !other.CompareTag(GameConstants.PlayerTag)) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.State.Playing) return;
            Activate();
            gm.SetCheckpoint(this);
        }

        void Activate()
        {
            IsActive = true;
            if (animator != null) animator.SetBool(ActiveHash, true);
            AudioManager.Play(SfxId.Checkpoint);
            if (activateEffectPrefab != null)
                Instantiate(activateEffectPrefab, transform.position + Vector3.up * 1.4f, Quaternion.identity);
        }
    }
}
