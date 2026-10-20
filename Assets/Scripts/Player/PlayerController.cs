using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Movimentação do Lumo: andar, correr, pular (coyote time, buffer e pulo variável),
    /// escalar escadas, descer de plataformas e andar sobre plataformas móveis.
    /// O pivô do personagem fica nos pés (transform.position.y = altura dos pés).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public enum AnimState { Idle = 0, Walk = 1, Run = 2, Jump = 3, Fall = 4, Climb = 5, Hurt = 6 }

        static readonly int StateHash = Animator.StringToHash("State");
        static readonly int ClimbSpeedHash = Animator.StringToHash("ClimbSpeed");

        [Header("Movimento horizontal")]
        [SerializeField] float walkSpeed = 5.5f;
        [SerializeField] float runSpeed = 9f;
        [SerializeField] float groundAcceleration = 80f;
        [SerializeField] float groundDeceleration = 100f;
        [SerializeField] float airAcceleration = 55f;

        [Header("Pulo")]
        [SerializeField] float jumpVelocity = 17f;
        [SerializeField] float gravityScale = 4f;
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [SerializeField] float jumpCutMultiplier = 0.5f;
        [SerializeField] float maxFallSpeed = 22f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Escalada")]
        [SerializeField] float climbSpeed = 4.5f;
        [SerializeField] float ladderSnapSpeed = 12f;

        [Header("Detecção de chão")]
        [SerializeField] LayerMask groundMask = 1 << GameConstants.LayerGround;
        [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

        [Header("Referências")]
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Collider2D bodyCollider;
        [SerializeField] GameObject dustEffectPrefab;

        Rigidbody2D rb;
        readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
        ContactFilter2D groundFilter;

        float moveInput;
        float verticalInput;
        bool runHeld;
        float ownVelocityX;
        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        bool isJumping;
        bool jumpCarriesRunSpeed;
        float controlLockUntil;
        float hurtUntil;
        bool frozen;

        Collider2D groundCollider;
        MovingPlatform groundPlatform;
        Ladder overlappingLadder;
        Ladder climbingLadder;
        Collider2D droppingThrough;
        float dropThroughUntil;
        bool wasGrounded;
        float peakFallSpeed;
        float runDustTimer;

        public bool IsGrounded { get; private set; }
        public bool IsClimbing => climbingLadder != null;
        public bool FacingRight { get; private set; } = true;
        public bool ControlEnabled { get; set; } = true;
        public Rigidbody2D Body => rb;
        public SpriteRenderer Renderer => spriteRenderer;
        public Collider2D BodyCollider => bodyCollider;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            rb.gravityScale = gravityScale;
            groundFilter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = groundMask };
        }

        void Update()
        {
            bool canControl = ControlEnabled && !frozen && Time.time >= controlLockUntil && Time.timeScale > 0f;
            moveInput = canControl ? InputReader.Horizontal : 0f;
            verticalInput = canControl ? InputReader.Vertical : 0f;
            runHeld = canControl && InputReader.RunHeld;

            if (canControl && InputReader.JumpPressed) lastJumpPressedTime = Time.time;

            // Pulo variável: soltar o botão durante a subida corta o pulo.
            if (isJumping && !frozen && (!InputReader.JumpHeld || !ControlEnabled) && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                isJumping = false;
            }

            if (canControl && !IsClimbing && Mathf.Abs(moveInput) > 0.01f) FacingRight = moveInput > 0f;

            UpdateAnimation();
        }

        void FixedUpdate()
        {
            if (frozen)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            CheckGround();

            if (!IsClimbing) TryStartClimb();
            if (IsClimbing)
            {
                ClimbUpdate();
                return;
            }

            HorizontalMovement();
            HandleJump();
            ApplyGravity();
            UpdateDust();
        }

        // ------------------------------------------------------------------ Chão

        void CheckGround()
        {
            groundCollider = null;
            groundPlatform = null;
            Vector2 feet = rb.position;

            if (rb.linearVelocity.y <= 0.1f || IsClimbing)
            {
                overlapResults.Clear();
                Physics2D.OverlapBox(feet + new Vector2(0f, -0.02f), groundCheckSize, 0f, groundFilter, overlapResults);
                foreach (var col in overlapResults)
                {
                    if (col == null || col == bodyCollider) continue;
                    if (col == droppingThrough && Time.time < dropThroughUntil) continue;
                    if (climbingLadder != null && col == climbingLadder.TopCollider) continue;
                    groundCollider = col;
                    if (col.attachedRigidbody != null)
                        groundPlatform = col.attachedRigidbody.GetComponent<MovingPlatform>();
                    break;
                }
            }

            IsGrounded = groundCollider != null;
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                jumpCarriesRunSpeed = false;
                if (!wasGrounded && peakFallSpeed > 12f)
                {
                    SpawnDust();
                    AudioManager.Play(SfxId.Land, 0.6f);
                }
                peakFallSpeed = 0f;
            }
            else
            {
                peakFallSpeed = Mathf.Max(peakFallSpeed, -rb.linearVelocity.y);
            }
            wasGrounded = IsGrounded;

            if (droppingThrough != null && Time.time >= dropThroughUntil)
            {
                Physics2D.IgnoreCollision(bodyCollider, droppingThrough, false);
                droppingThrough = null;
            }
        }

        Vector2 PlatformVelocity => IsGrounded && groundPlatform != null ? groundPlatform.Velocity : Vector2.zero;

        // ------------------------------------------------------------------ Movimento

        void HorizontalMovement()
        {
            bool running = runHeld || (!IsGrounded && jumpCarriesRunSpeed);
            float target = moveInput * (running ? runSpeed : walkSpeed);
            float accel;
            if (Time.time < controlLockUntil) accel = 6f;
            else if (IsGrounded) accel = Mathf.Abs(target) > 0.01f ? groundAcceleration : groundDeceleration;
            else accel = airAcceleration;

            ownVelocityX = Mathf.MoveTowards(ownVelocityX, target, accel * Time.fixedDeltaTime);
            Vector2 platform = PlatformVelocity;
            float vy = rb.linearVelocity.y;
            if (IsGrounded && groundPlatform != null && !isJumping && platform.y < 0f) vy = platform.y;
            rb.linearVelocity = new Vector2(ownVelocityX + platform.x, vy);
        }

        void HandleJump()
        {
            bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            if (!buffered) return;

            if (IsGrounded && verticalInput < -0.5f && IsOneWay(groundCollider) && groundCollider.GetComponentInParent<Ladder>() == null)
            {
                DropThrough(groundCollider);
                lastJumpPressedTime = -10f;
                return;
            }

            bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;
            if (canCoyote) DoJump(jumpVelocity);
        }

        void DoJump(float velocity)
        {
            Vector2 platform = PlatformVelocity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity + Mathf.Max(0f, platform.y));
            isJumping = true;
            jumpCarriesRunSpeed = Mathf.Abs(ownVelocityX) > walkSpeed + 0.25f;
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            IsGrounded = false;
            wasGrounded = false;
            AudioManager.Play(SfxId.Jump, 0.8f, 0.08f);
            SpawnDust();
        }

        void ApplyGravity()
        {
            float vy = rb.linearVelocity.y;
            rb.gravityScale = vy < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
            if (vy < -maxFallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            if (vy <= 0f) isJumping = false;
        }

        static bool IsOneWay(Collider2D col)
        {
            return col != null && col.usedByEffector && col.GetComponent<PlatformEffector2D>() != null;
        }

        void DropThrough(Collider2D platform)
        {
            droppingThrough = platform;
            dropThroughUntil = Time.time + 0.35f;
            Physics2D.IgnoreCollision(bodyCollider, platform, true);
            rb.position += Vector2.down * 0.05f;
            IsGrounded = false;
            lastGroundedTime = -10f;
        }

        // ------------------------------------------------------------------ Escadas

        void TryStartClimb()
        {
            if (!ControlEnabled || Time.time < controlLockUntil) return;

            // Descer pela escada a partir do topo.
            if (IsGrounded && verticalInput < -0.5f && groundCollider != null)
            {
                var topLadder = groundCollider.GetComponentInParent<Ladder>();
                if (topLadder != null && topLadder.TopCollider == groundCollider)
                {
                    StartClimb(topLadder);
                    rb.position += Vector2.down * 0.15f;
                    return;
                }
            }

            if (overlappingLadder == null) return;
            float feet = rb.position.y;
            if (verticalInput > 0.5f && feet < overlappingLadder.TopY - 0.05f)
                StartClimb(overlappingLadder);
            else if (verticalInput < -0.5f && !IsGrounded && feet > overlappingLadder.BottomY + 0.05f)
                StartClimb(overlappingLadder);
        }

        void StartClimb(Ladder ladder)
        {
            climbingLadder = ladder;
            isJumping = false;
            ownVelocityX = 0f;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            if (ladder.TopCollider != null) Physics2D.IgnoreCollision(bodyCollider, ladder.TopCollider, true);
        }

        void StopClimb()
        {
            if (climbingLadder != null && climbingLadder.TopCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, climbingLadder.TopCollider, false);
            climbingLadder = null;
            rb.gravityScale = gravityScale;
        }

        void ClimbUpdate()
        {
            var ladder = climbingLadder;
            float dt = Time.fixedDeltaTime;

            if (Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                StopClimb();
                ownVelocityX = moveInput * walkSpeed;
                DoJump(jumpVelocity * 0.8f);
                return;
            }

            float vy = verticalInput * climbSpeed;
            float newX = Mathf.MoveTowards(rb.position.x, ladder.CenterX, ladderSnapSpeed * dt);
            rb.linearVelocity = new Vector2((newX - rb.position.x) / dt, vy);

            float feet = rb.position.y;
            if (vy > 0f && feet + vy * dt >= ladder.TopY - 0.02f)
            {
                // Chegou ao topo: sobe na plataforma da escada.
                rb.position = new Vector2(newX, ladder.TopY + 0.02f);
                rb.linearVelocity = Vector2.zero;
                StopClimb();
                return;
            }
            if (vy < 0f && IsGrounded)
            {
                StopClimb();
                return;
            }
            if (feet < ladder.BottomY - 0.25f) StopClimb();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ladder = other.GetComponent<Ladder>();
            if (ladder != null) overlappingLadder = ladder;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var ladder = other.GetComponent<Ladder>();
            if (ladder != null && ladder == overlappingLadder) overlappingLadder = null;
            if (ladder != null && ladder == climbingLadder) StopClimb();
        }

        // ------------------------------------------------------------------ Efeitos e animação

        void UpdateDust()
        {
            if (!IsGrounded || Mathf.Abs(ownVelocityX) < walkSpeed + 0.5f) return;
            runDustTimer -= Time.fixedDeltaTime;
            if (runDustTimer <= 0f)
            {
                runDustTimer = 0.18f;
                SpawnDust();
            }
        }

        void SpawnDust()
        {
            if (dustEffectPrefab == null) return;
            var dust = Instantiate(dustEffectPrefab, transform.position, Quaternion.identity);
            dust.transform.localScale = Vector3.one * 0.6f;
        }

        void UpdateAnimation()
        {
            if (animator == null) return;
            bool groundedForAnim = IsGrounded || Time.time - lastGroundedTime < 0.06f;
            AnimState state;
            if (Time.time < hurtUntil) state = AnimState.Hurt;
            else if (IsClimbing) state = AnimState.Climb;
            else if (!groundedForAnim) state = rb.linearVelocity.y > 0.1f ? AnimState.Jump : AnimState.Fall;
            else if (Mathf.Abs(ownVelocityX) > walkSpeed + 0.5f) state = AnimState.Run;
            else if (Mathf.Abs(ownVelocityX) > 0.2f) state = AnimState.Walk;
            else state = AnimState.Idle;

            animator.SetInteger(StateHash, (int)state);
            animator.SetFloat(ClimbSpeedHash, IsClimbing ? (Mathf.Abs(verticalInput) > 0.1f ? 1f : 0f) : 1f);
            if (spriteRenderer != null) spriteRenderer.flipX = !FacingRight;
        }

        // ------------------------------------------------------------------ API pública

        /// <summary>Impulso para cima ao pisar em um inimigo.</summary>
        public void Bounce(float velocity)
        {
            if (IsClimbing) StopClimb();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity);
            isJumping = InputReader.JumpHeld;
            lastGroundedTime = -10f;
        }

        public void ApplyKnockback(Vector2 sourcePosition, float horizontal = 7f, float vertical = 9f)
        {
            if (IsClimbing) StopClimb();
            float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
            ownVelocityX = dir * horizontal;
            rb.linearVelocity = new Vector2(ownVelocityX, vertical);
            isJumping = false;
            controlLockUntil = Time.time + 0.3f;
            hurtUntil = Time.time + 0.4f;
        }

        public void TeleportTo(Vector2 position)
        {
            if (IsClimbing) StopClimb();
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            ownVelocityX = 0f;
            isJumping = false;
            peakFallSpeed = 0f;
            FacingRight = true;
        }

        /// <summary>Congela o personagem (morte, conclusão de fase).</summary>
        public void SetFrozen(bool value)
        {
            frozen = value;
            if (value)
            {
                if (IsClimbing) StopClimb();
                rb.linearVelocity = Vector2.zero;
                ownVelocityX = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = gravityScale;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, -0.02f), groundCheckSize);
        }
    }
}
