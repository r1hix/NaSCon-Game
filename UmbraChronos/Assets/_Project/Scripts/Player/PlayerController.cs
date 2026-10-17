using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UmbraChronos.Core;

namespace UmbraChronos.Player
{
    /// <summary>
    /// Snappy 2D Platformer Controller featuring Coyote Time, Jump Buffering, 
    /// Variable Jump Cut, and Fall Gravity Scaling.
    /// Works seamlessly across Unity's New Input System and Legacy Input.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 9f;
        [SerializeField] private float acceleration = 65f;
        [SerializeField] private float deceleration = 75f;
        [SerializeField] [Range(0f, 1f)] private float airControl = 0.85f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 15.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float normalGravityScale = 3f;
        [SerializeField] private float fallGravityMultiplier = 1.9f;
        [SerializeField] private float maxFallSpeed = 22f;

        [Header("Ground Detection")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.75f, 0.15f);
        [SerializeField] private LayerMask groundLayer = ~0; // default to everything

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // Internal State
        private Rigidbody2D rb;
        private Collider2D col;
        private float horizontalInput;
        private bool jumpPressed;
        private bool jumpReleased;
        private bool isGrounded;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool isRewinding;
        private bool isDead;
        private bool isFacingRight = true;

        // Public Accessors
        public bool IsGrounded => isGrounded;
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool IsFacingRight => isFacingRight;
        public bool IsRewinding => isRewinding;
        public bool IsDead => isDead;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            rb.gravityScale = normalGravityScale;
            rb.freezeRotation = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            if (GetComponent<PlayerRadiationFX>() == null)
            {
                gameObject.AddComponent<PlayerRadiationFX>();
            }

            if (GetComponent<PlayerAnimator>() == null)
            {
                gameObject.AddComponent<PlayerAnimator>();
            }
        }

        private void OnEnable()
        {
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void Update()
        {
            if (isRewinding || isDead) return;

            GatherInput();
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            if (isRewinding || isDead) return;

            CheckGround();
            HandleHorizontalMovement();
            HandleJump();
            ApplyCustomGravity();
        }

        private void GatherInput()
        {
            horizontalInput = 0f;
            jumpPressed = false;
            jumpReleased = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1f;

                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    jumpPressed = true;
                }

                if (keyboard.spaceKey.wasReleasedThisFrame || keyboard.wKey.wasReleasedThisFrame || keyboard.upArrowKey.wasReleasedThisFrame)
                {
                    jumpReleased = true;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stickX = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) horizontalInput = Mathf.Sign(stickX);
                if (gamepad.dpad.left.isPressed) horizontalInput -= 1f;
                if (gamepad.dpad.right.isPressed) horizontalInput += 1f;

                if (gamepad.buttonSouth.wasPressedThisFrame) jumpPressed = true;
                if (gamepad.buttonSouth.wasReleasedThisFrame) jumpReleased = true;
            }
#else
            horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump")) jumpPressed = true;
            if (Input.GetButtonUp("Jump")) jumpReleased = true;
#endif

            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);

            if (jumpPressed)
            {
                jumpBufferTimer = jumpBufferTime;
            }
        }

        private void UpdateTimers()
        {
            // Coyote timer
            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= UnityEngine.Time.deltaTime;
            }

            // Jump buffer timer
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= UnityEngine.Time.deltaTime;
            }

            // Variable jump cut
            if (jumpReleased && rb.linearVelocity.y > 0.1f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        private void CheckGround()
        {
            if (col == null) col = GetComponent<Collider2D>();
            if (col == null) return;

            Bounds bounds = col.bounds;
            // Place box check precisely at bottom edge of collider, slightly inset on sides
            Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y);
            Vector2 checkSize = new Vector2(bounds.size.x * 0.85f, 0.14f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundLayer);
            isGrounded = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != col && !hits[i].isTrigger)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        private void HandleHorizontalMovement()
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float accelRate = isGrounded ? (Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration) : (acceleration * airControl);

            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * UnityEngine.Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);

            // Sprite facing direction
            if (horizontalInput > 0.05f && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < -0.05f && isFacingRight)
            {
                Flip();
            }
        }

        private void HandleJump()
        {
            // Trigger jump if coyote time and jump buffer are active
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
        }

        private void ApplyCustomGravity()
        {
            if (rb.linearVelocity.y < -0.05f)
            {
                // Falling: scale gravity for crisp descent
                rb.gravityScale = normalGravityScale * fallGravityMultiplier;
            }
            else
            {
                rb.gravityScale = normalGravityScale;
            }

            // Clamp max terminal velocity
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !isFacingRight;
            }
            else
            {
                Vector3 scale = transform.localScale;
                scale.x = isFacingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        private void HandleRewindStateChanged(bool active)
        {
            isRewinding = active;
            rb.simulated = !active;
            if (active)
            {
                // Rewinding clears death state, allowing player to rewind out of danger!
                isDead = false;
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void HandlePlayerDied()
        {
            isDead = true;
            rb.linearVelocity = Vector2.zero;
        }

        private void HandlePlayerRespawned()
        {
            isDead = false;
            rb.linearVelocity = Vector2.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (col == null) col = GetComponent<Collider2D>();
            if (col == null) return;

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Bounds bounds = col.bounds;
            Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y);
            Vector2 checkSize = new Vector2(bounds.size.x * 0.85f, 0.14f);
            Gizmos.DrawWireCube(checkCenter, checkSize);
        }
    }
}
