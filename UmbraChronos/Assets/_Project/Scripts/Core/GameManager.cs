using System;
using UnityEngine;
using UmbraChronos.Player;
using UmbraChronos.Time;

namespace UmbraChronos.Core
{
    /// <summary>
    /// Master game lifecycle coordinator for Umbra Chronos.
    /// Manages:
    /// 1. Safe Room Checkpoints: Automatically tracks the player's last safe chamber threshold.
    /// 2. Death-to-Rewind Lifecycle: When vaporized by celestial light, grants a 3.5s grace window
    ///    for the player to hold [R] and rewind themselves out of danger.
    /// 3. Fallback Auto-Respawn: If rewind is not activated or buffer is depleted, cleanly respawns
    ///    the player at the room checkpoint with restored energy.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Death & Rewind Recovery")]
        [Tooltip("Seconds the player has to hold [R] to rewind out of death before auto-respawning.")]
        [SerializeField] private float deathRewindGraceSeconds = 3.5f;

        [Tooltip("Minimum rewind energy (seconds) guaranteed upon death so player can always rewind their last fatal step.")]
        [SerializeField] private float minDeathRewindEnergy = 2.5f;

        [Header("Temporal Anchors (Lives)")]
        [Tooltip("Number of rewind chances before permanent timeline collapse (Game Over).")]
        [SerializeField] private int maxRewindChances = 3;

        [Header("Checkpoints")]
        [Tooltip("Current active respawn position.")]
        [SerializeField] private Vector3 currentCheckpoint;

        // State
        private int currentRewindChances = 3;
        private bool isGameOver = false;
        private bool isPlayerDead = false;
        private float deathCountdown = 0f;
        private float currentRewindEnergy = 5f;
        private Transform playerTransform;
        private Rigidbody2D playerRb;
        private PlayerController playerCtrl;

        public Vector3 CurrentCheckpoint => currentCheckpoint;
        public bool IsPlayerDead => isPlayerDead;
        public bool IsGameOver => isGameOver;
        public int RemainingChances => currentRewindChances;
        public int MaxChances => maxRewindChances;
        public float DeathCountdownFraction => deathRewindGraceSeconds > 0.01f ? Mathf.Clamp01(deathCountdown / deathRewindGraceSeconds) : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && UnityEngine.Object.FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("[GameManager]");
                gmObj.AddComponent<GameManager>();
                DontDestroyOnLoad(gmObj);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnRewindEnergyChanged += HandleRewindEnergyChanged;
            TimeEvents.OnRoomChanged += HandleRoomChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnRewindEnergyChanged -= HandleRewindEnergyChanged;
            TimeEvents.OnRoomChanged -= HandleRoomChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            isGameOver = false;
            isPlayerDead = false;
            deathCountdown = 0f;
            currentRewindChances = maxRewindChances;
            playerTransform = null;
            LocatePlayer();
            TimeEvents.OnLivesChanged?.Invoke(currentRewindChances, maxRewindChances);
        }

        private void Start()
        {
            LocatePlayer();
            if (playerTransform != null)
            {
                currentCheckpoint = playerTransform.position;
            }
            TimeEvents.OnLivesChanged?.Invoke(currentRewindChances, maxRewindChances);
        }

        private void Update()
        {
            if (isGameOver)
            {
                bool retry = false;
                bool quit = false;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame) retry = true;
                    if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) quit = true;
                }
#endif
                if (!retry && Input.GetKeyDown(KeyCode.R)) retry = true;
                if (!quit && Input.GetKeyDown(KeyCode.Escape)) quit = true;

                if (retry)
                {
                    RetryLevel();
                }
                else if (quit)
                {
                    ReturnToTitle();
                }
                return;
            }

            if (playerTransform == null)
            {
                LocatePlayer();
                return;
            }

            if (isPlayerDead)
            {
                // Only tick down the countdown if player is NOT currently rewinding
                var rewindMgr = RewindManager.Instance;
                bool isRewinding = rewindMgr != null && rewindMgr.IsRewinding;

                if (!isRewinding)
                {
                    deathCountdown -= UnityEngine.Time.deltaTime;

                    // If time ran out or buffer is exhausted while still dead, perform fallback respawn
                    if (deathCountdown <= 0f || currentRewindEnergy <= 0.05f)
                    {
                        RespawnAtCheckpoint();
                    }
                }
            }
        }

        private void LocatePlayer()
        {
            if (playerTransform != null) return;

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
                playerRb = player.GetComponent<Rigidbody2D>();
                playerCtrl = player;
                currentCheckpoint = playerTransform.position;
            }
        }

        private void HandlePlayerDied()
        {
            isPlayerDead = true;
            deathCountdown = deathRewindGraceSeconds;

            // Ensure player has enough rewind energy to rewind their fatal misstep
            var rewindMgr = RewindManager.Instance;
            if (rewindMgr != null && rewindMgr.CurrentEnergy < minDeathRewindEnergy)
            {
                rewindMgr.SetEnergy(minDeathRewindEnergy);
            }
        }

        private void HandleRewindStateChanged(bool rewinding)
        {
            if (rewinding && isPlayerDead)
            {
                // Rewinding is actively running; death state is being undone
            }
            else if (!rewinding && isPlayerDead)
            {
                // Player released rewind: check if they are now in safety
                // If they rewound into safety, clear death state
                isPlayerDead = false;
                TimeEvents.OnPlayerRespawned?.Invoke();
            }
        }

        private void HandleRewindEnergyChanged(float current, float max)
        {
            currentRewindEnergy = current;
        }

        private void HandleRoomChanged(Vector2Int roomIndex)
        {
            // When moving to a new chamber, update checkpoint to the entry point
            if (playerTransform != null && !isPlayerDead)
            {
                currentCheckpoint = playerTransform.position;
            }
        }

        private void HandlePlayerRespawned()
        {
            isPlayerDead = false;
            deathCountdown = 0f;
        }

        /// <summary>
        /// Resets player to the active room checkpoint when rewind fails or is exhausted.
        /// Decrements available rewind chances; triggers perma-death Game Over when 0.
        /// </summary>
        public void RespawnAtCheckpoint()
        {
            if (isGameOver) return;

            currentRewindChances--;
            TimeEvents.OnLivesChanged?.Invoke(currentRewindChances, maxRewindChances);

            if (currentRewindChances <= 0)
            {
                TriggerGameOver();
                return;
            }

            isPlayerDead = false;
            deathCountdown = 0f;

            if (playerTransform == null) LocatePlayer();

            if (playerTransform != null)
            {
                playerTransform.position = currentCheckpoint;

                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                    playerRb.simulated = true;
                }
            }

            // Refill rewind energy so player has full gauge
            var rewindMgr = RewindManager.Instance;
            if (rewindMgr != null)
            {
                rewindMgr.SetEnergy(rewindMgr.MaxEnergy);
            }

            // Snap camera if present
            var camCtrl = UnityEngine.Object.FindAnyObjectByType<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.SnapToTargetRoom();
            }

            TimeEvents.OnPlayerRespawned?.Invoke();
        }

        private void TriggerGameOver()
        {
            isGameOver = true;
            isPlayerDead = true;
            deathCountdown = 0f;

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.simulated = false;
            }
            if (playerCtrl != null)
            {
                playerCtrl.enabled = false;
            }

            TimeEvents.OnGameOver?.Invoke();
        }

        public void RetryLevel()
        {
            isGameOver = false;
            isPlayerDead = false;
            currentRewindChances = maxRewindChances;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToTitle()
        {
            isGameOver = false;
            isPlayerDead = false;
            currentRewindChances = maxRewindChances;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Explicitly sets a new checkpoint position.
        /// </summary>
        public void SetCheckpoint(Vector3 position)
        {
            currentCheckpoint = position;
        }
    }
}
