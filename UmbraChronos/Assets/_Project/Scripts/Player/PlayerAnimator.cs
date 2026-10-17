using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Player
{
    /// <summary>
    /// Drives dynamic, high-feel 2D character animations for the Umbral Wanderer:
    /// - Frame-by-frame state animation: Idle (4 frames), Run (6 frames), Jump, Fall.
    /// - Procedural Squash & Stretch on jump launch and ground impact.
    /// - Reverse frame playback during Time Rewind.
    /// - Auto-hooks to PlayerController or UmbralEcho.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Frames (Wanderer)")]
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private Sprite[] runSprites;
        [SerializeField] private Sprite jumpSprite;
        [SerializeField] private Sprite fallSprite;

        [Header("Frame Rates")]
        [SerializeField] private float idleFps = 6f;
        [SerializeField] private float runFps = 12f;

        [Header("Juice: Squash & Stretch")]
        [Tooltip("Enable procedural squash & stretch on jump launch and ground impact.")]
        [SerializeField] private bool enableSquashStretch = true;
        [SerializeField] private float jumpStretchFactor = 1.2f;
        [SerializeField] private float landSquashFactor = 0.78f;
        [SerializeField] private float recoverySpeed = 10f;

        [Header("Character Type")]
        [Tooltip("If true, loads and uses the translucent Umbral Echo phantom sprites.")]
        [SerializeField] private bool isEcho = false;

        // Components
        private SpriteRenderer spriteRenderer;
        private PlayerController playerController;
        private Rigidbody2D rb;

        // Animation state
        private float frameTimer;
        private int currentFrameIndex;
        private string currentState = "idle";
        private bool wasGrounded;
        private Vector3 baseScale = Vector3.one;
        private Vector3 currentScale = Vector3.one;
        private Vector3 lastPosition;
        private Vector2 derivedVelocity;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerController>() ?? GetComponentInParent<PlayerController>();
            rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
            baseScale = transform.localScale;
            currentScale = baseScale;
            lastPosition = transform.position;

            LoadSprites();
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            string prefix = isEcho ? "Echo" : "Player";
            string folder = "Assets/_Project/Sprites/Player";

            if (idleSprites == null || idleSprites.Length < 4 || idleSprites[0] == null)
            {
                idleSprites = new Sprite[4];
                for (int i = 0; i < 4; i++)
                {
                    idleSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_Idle_{i}.png");
                }
            }

            if (runSprites == null || runSprites.Length < 6 || runSprites[0] == null)
            {
                runSprites = new Sprite[6];
                for (int i = 0; i < 6; i++)
                {
                    runSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_Run_{i}.png");
                }
            }

            if (jumpSprite == null)
            {
                jumpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_Jump.png");
            }

            if (fallSprite == null)
            {
                fallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_Fall.png");
            }
#endif
        }

        private void Update()
        {
            float dt = UnityEngine.Time.deltaTime;
            if (dt > 0.0001f)
            {
                derivedVelocity = (transform.position - lastPosition) / dt;
                lastPosition = transform.position;
            }

            DetermineState();
            UpdateAnimation(dt);
            UpdateSquashStretch(dt);
            UpdateFacing();
        }

        private void DetermineState()
        {
            bool isGrounded = playerController != null ? playerController.IsGrounded : Mathf.Abs(derivedVelocity.y) < 0.2f;
            Vector2 vel = playerController != null ? playerController.Velocity : (rb != null && rb.simulated ? rb.linearVelocity : derivedVelocity);
            bool isRewinding = playerController != null ? playerController.IsRewinding : false;

            // Ground impact check (land squash)
            if (isGrounded && !wasGrounded && enableSquashStretch)
            {
                ApplySquash(landSquashFactor);
            }
            // Jump launch check (jump stretch)
            else if (!isGrounded && wasGrounded && vel.y > 1f && enableSquashStretch)
            {
                ApplyStretch(jumpStretchFactor);
            }
            wasGrounded = isGrounded;

            // Determine active animation state
            string newState;
            if (isRewinding)
            {
                newState = "rewind";
            }
            else if (!isGrounded)
            {
                newState = vel.y > 0.2f ? "jump" : "fall";
            }
            else if (Mathf.Abs(vel.x) > 0.35f)
            {
                newState = "run";
            }
            else
            {
                newState = "idle";
            }

            if (newState != currentState)
            {
                currentState = newState;
                frameTimer = 0f;
                currentFrameIndex = 0;
            }
        }

        private void UpdateAnimation(float dt)
        {
            if (spriteRenderer == null) return;

            switch (currentState)
            {
                case "idle":
                    StepFrames(idleSprites, idleFps, dt, false);
                    break;

                case "run":
                    // Scale run speed with horizontal velocity
                    float velX = playerController != null ? Mathf.Abs(playerController.Velocity.x) : 5f;
                    float speedMult = Mathf.Clamp(velX / 7.5f, 0.75f, 1.45f);
                    StepFrames(runSprites, runFps * speedMult, dt, false);
                    break;

                case "jump":
                    if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                    break;

                case "fall":
                    if (fallSprite != null) spriteRenderer.sprite = fallSprite;
                    break;

                case "rewind":
                    // Play running/idle animation backward at 2x speed for authentic VHS reverse
                    StepFrames(runSprites != null && runSprites.Length > 0 ? runSprites : idleSprites, runFps * 2.2f, dt, true);
                    break;
            }
        }

        private void StepFrames(Sprite[] sprites, float fps, float dt, bool reverse)
        {
            if (sprites == null || sprites.Length == 0) return;

            frameTimer += dt;
            float frameInterval = 1f / Mathf.Max(1f, fps);

            if (frameTimer >= frameInterval)
            {
                frameTimer -= frameInterval;
                if (reverse)
                {
                    currentFrameIndex = (currentFrameIndex - 1 + sprites.Length) % sprites.Length;
                }
                else
                {
                    currentFrameIndex = (currentFrameIndex + 1) % sprites.Length;
                }
            }

            currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, sprites.Length - 1);
            if (sprites[currentFrameIndex] != null)
            {
                spriteRenderer.sprite = sprites[currentFrameIndex];
            }
        }

        private void UpdateFacing()
        {
            if (spriteRenderer == null) return;

            if (playerController != null)
            {
                // When facing left, flip sprite
                spriteRenderer.flipX = !playerController.IsFacingRight;
            }
            else if (Mathf.Abs(derivedVelocity.x) > 0.05f)
            {
                spriteRenderer.flipX = derivedVelocity.x < 0f;
            }
        }

        private void ApplySquash(float factor)
        {
            // Squash: wide and short
            currentScale = new Vector3(baseScale.x * (1f / factor), baseScale.y * factor, baseScale.z);
        }

        private void ApplyStretch(float factor)
        {
            // Stretch: narrow and tall
            currentScale = new Vector3(baseScale.x * (1f / factor), baseScale.y * factor, baseScale.z);
        }

        private void UpdateSquashStretch(float dt)
        {
            if (!enableSquashStretch) return;

            // Smoothly ease current scale back to base scale
            currentScale = Vector3.MoveTowards(currentScale, baseScale, recoverySpeed * dt);
            transform.localScale = currentScale;
        }

        /// <summary>
        /// Configures this animator for Umbral Echo ghost playback.
        /// </summary>
        public void ConfigureAsEcho()
        {
            isEcho = true;
            LoadSprites();
        }
    }
}
