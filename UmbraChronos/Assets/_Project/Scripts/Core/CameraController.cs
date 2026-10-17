using System;
using UnityEngine;
using UmbraChronos.Player;

namespace UmbraChronos.Core
{
    public enum CameraFollowMode
    {
        ScreenShift,  // Paged room-by-room (shifts over smoothly when player goes off-screen)
        SmoothFollow  // Continuous tracking with deadzone
    }

    /// <summary>
    /// Snappy 2D Camera Controller supporting both:
    /// 1. ScreenShift (Room Paging): Shifts camera smoothly over when the player walks off-screen,
    ///    enabling expansive, multi-room levels without feeling cramped.
    /// 2. SmoothFollow: Continuous player tracking with deadzone.
    /// Also keeps the background canvas and celestial sundial aligned across all screens.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform to follow. If null, automatically locates PlayerController.")]
        [SerializeField] private Transform target;

        [Tooltip("Offset added to the target position.")]
        [SerializeField] private Vector2 targetOffset = new Vector2(0f, 0.5f);

        [Header("Follow Mode")]
        [SerializeField] private CameraFollowMode followMode = CameraFollowMode.ScreenShift;

        [Header("Screen Shift (Room Paging) Settings")]
        [Tooltip("Width and height of each room/screen in world units. If 0, auto-calculated from camera size and aspect.")]
        [SerializeField] private Vector2 roomDimensions = new Vector2(24f, 15f);

        [Tooltip("Center origin of the primary starting room (0, 0).")]
        [SerializeField] private Vector2 roomOrigin = new Vector2(0f, 1.0f);

        [Tooltip("Hysteresis threshold beyond screen edge before triggering a shift (prevents boundary flickering).")]
        [SerializeField] private float shiftHysteresis = 1.0f;

        [Tooltip("Duration in seconds for the camera to smoothly glide to the new room.")]
        [SerializeField] private float shiftTransitionDuration = 0.35f;

        [Header("Continuous Follow Settings (SmoothFollow Mode)")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector2 deadZone = new Vector2(2f, 1.5f);

        [Header("Level Bounds (Optional)")]
        [SerializeField] private bool useLevelBounds = false;
        [SerializeField] private Vector2 minPosition = new Vector2(-50f, -10f);
        [SerializeField] private Vector2 maxPosition = new Vector2(50f, 20f);

        [Header("Attached Background")]
        [Tooltip("Optional background canvas to keep aligned with the camera as it travels across rooms.")]
        [SerializeField] private Transform backgroundCanvas;

        [Header("Camera Depth")]
        [SerializeField] private float cameraZ = -10f;

        [Header("Screen Shake / Camera Trauma")]
        [Tooltip("Master multiplier for screen shake intensity.")]
        [SerializeField] private float shakeMultiplier = 1.0f;

        // Components & State
        private Camera cam;
        private Vector3 currentVelocity;
        private Vector2Int currentRoomIndex = Vector2Int.zero;
        private Vector3 targetCameraPos;

        // Shake state
        private float currentShakeIntensity = 0f;
        private float shakeDurationTotal = 0f;
        private float shakeTimeRemaining = 0f;
        private float continuousThermalShake = 0f;

        public Vector2Int CurrentRoomIndex => currentRoomIndex;
        public Camera Camera => cam;

        private void OnEnable()
        {
            TimeEvents.OnCameraShake += TriggerShake;
            TimeEvents.OnPlayerDied += HandlePlayerDiedShake;
            TimeEvents.OnPlayerLanded += HandlePlayerLandedShake;
            TimeEvents.OnEchoCreated += HandleEchoCreatedShake;
            TimeEvents.OnPressurePlateChanged += HandlePlateShake;
            TimeEvents.OnGateStateChanged += HandleGateShake;
            TimeEvents.OnSunlightExposureChanged += HandleSunlightExposureChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnCameraShake -= TriggerShake;
            TimeEvents.OnPlayerDied -= HandlePlayerDiedShake;
            TimeEvents.OnPlayerLanded -= HandlePlayerLandedShake;
            TimeEvents.OnEchoCreated -= HandleEchoCreatedShake;
            TimeEvents.OnPressurePlateChanged -= HandlePlateShake;
            TimeEvents.OnGateStateChanged -= HandleGateShake;
            TimeEvents.OnSunlightExposureChanged -= HandleSunlightExposureChanged;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            AutoCalculateRoomDimensions();
            LocateTarget();

            // Initialize camera position
            if (target != null)
            {
                InitializeRoomPosition();
            }
            else
            {
                targetCameraPos = new Vector3(roomOrigin.x, roomOrigin.y, cameraZ);
            }

            transform.position = targetCameraPos;
        }

        private void Start()
        {
            if (target == null) LocateTarget();
            if (backgroundCanvas == null)
            {
                GameObject bg = GameObject.Find("Background_Canvas");
                if (bg != null) backgroundCanvas = bg.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                LocateTarget();
                if (target == null) return;
            }

            switch (followMode)
            {
                case CameraFollowMode.ScreenShift:
                    UpdateScreenShift();
                    break;
                case CameraFollowMode.SmoothFollow:
                    UpdateSmoothFollow();
                    break;
            }

            ApplyScreenShake();

            // Keep background canvas behind camera
            if (backgroundCanvas != null)
            {
                backgroundCanvas.position = new Vector3(transform.position.x, transform.position.y, backgroundCanvas.position.z);
            }
        }

        #region Screen Shake

        /// <summary>
        /// Triggers high-impact organic camera trauma.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            intensity *= shakeMultiplier;
            if (intensity <= 0.001f || duration <= 0.001f) return;

            if (intensity > currentShakeIntensity || shakeTimeRemaining <= 0f)
            {
                currentShakeIntensity = intensity;
                shakeDurationTotal = duration;
                shakeTimeRemaining = duration;
            }
            else
            {
                currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
                shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
            }
        }

        private void ApplyScreenShake()
        {
            float totalIntensity = 0f;

            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= UnityEngine.Time.deltaTime;
                float progress = Mathf.Clamp01(shakeTimeRemaining / Mathf.Max(0.01f, shakeDurationTotal));
                // Quadratic easing: hard initial impact decaying smoothly
                totalIntensity += currentShakeIntensity * (progress * progress);
            }

            totalIntensity += continuousThermalShake;

            if (totalIntensity > 0.001f)
            {
                float seed = UnityEngine.Time.time * 26f;
                float offsetX = (Mathf.PerlinNoise(seed, 0f) * 2f - 1f) * totalIntensity;
                float offsetY = (Mathf.PerlinNoise(0f, seed) * 2f - 1f) * totalIntensity;
                transform.position += new Vector3(offsetX, offsetY, 0f);
            }
        }

        private void HandlePlayerDiedShake()
        {
            TriggerShake(0.55f, 0.45f);
        }

        private void HandlePlayerLandedShake(float fallSpeed)
        {
            if (fallSpeed > 6.5f)
            {
                float intensity = Mathf.Clamp((fallSpeed - 6f) * 0.04f, 0.08f, 0.32f);
                TriggerShake(intensity, 0.18f);
            }
        }

        private void HandleEchoCreatedShake()
        {
            TriggerShake(0.20f, 0.15f);
        }

        private void HandlePlateShake(bool pressed)
        {
            if (pressed)
            {
                TriggerShake(0.10f, 0.12f);
            }
        }

        private void HandleGateShake(bool opened)
        {
            TriggerShake(0.16f, 0.22f);
        }

        private void HandleSunlightExposureChanged(float exposure)
        {
            continuousThermalShake = exposure > 0.4f ? (exposure - 0.4f) * 0.15f : 0f;
        }

        #endregion

        private void LocateTarget()
        {
            if (target != null) return;

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
                InitializeRoomPosition();
                return;
            }

            try
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                    InitializeRoomPosition();
                }
            }
            catch (UnityException) { }
        }

        private void AutoCalculateRoomDimensions()
        {
            if (cam == null) return;
            if (roomDimensions.x <= 0f || roomDimensions.y <= 0f)
            {
                float height = cam.orthographicSize * 2f;
                float width = height * cam.aspect;
                roomDimensions = new Vector2(width, height);
            }
        }

        private void InitializeRoomPosition()
        {
            Vector2 p = (Vector2)target.position + targetOffset;
            float rx = Mathf.Max(4f, roomDimensions.x);
            float ry = Mathf.Max(4f, roomDimensions.y);

            currentRoomIndex.x = Mathf.RoundToInt((p.x - roomOrigin.x) / rx);
            currentRoomIndex.y = Mathf.RoundToInt((p.y - roomOrigin.y) / ry);

            targetCameraPos = CalculateRoomCenter(currentRoomIndex);
            transform.position = targetCameraPos;
        }

        private void UpdateScreenShift()
        {
            Vector2 p = (Vector2)target.position + targetOffset;
            float rx = Mathf.Max(4f, roomDimensions.x);
            float ry = Mathf.Max(4f, roomDimensions.y);

            // Compute current room boundaries
            float roomCenterX = roomOrigin.x + (currentRoomIndex.x * rx);
            float roomCenterY = roomOrigin.y + (currentRoomIndex.y * ry);

            float halfW = rx * 0.5f;
            float halfH = ry * 0.5f;

            float rightEdge = roomCenterX + halfW;
            float leftEdge = roomCenterX - halfW;
            float topEdge = roomCenterY + halfH;
            float bottomEdge = roomCenterY - halfH;

            bool roomChanged = false;

            // Check horizontal shift
            if (p.x > rightEdge + shiftHysteresis)
            {
                // Shift to the right
                int newIndexX = Mathf.RoundToInt((p.x - roomOrigin.x) / rx);
                if (newIndexX != currentRoomIndex.x)
                {
                    currentRoomIndex.x = newIndexX;
                    roomChanged = true;
                }
            }
            else if (p.x < leftEdge - shiftHysteresis)
            {
                // Shift to the left
                int newIndexX = Mathf.RoundToInt((p.x - roomOrigin.x) / rx);
                if (newIndexX != currentRoomIndex.x)
                {
                    currentRoomIndex.x = newIndexX;
                    roomChanged = true;
                }
            }

            // Check vertical shift
            if (p.y > topEdge + shiftHysteresis)
            {
                int newIndexY = Mathf.RoundToInt((p.y - roomOrigin.y) / ry);
                if (newIndexY != currentRoomIndex.y)
                {
                    currentRoomIndex.y = newIndexY;
                    roomChanged = true;
                }
            }
            else if (p.y < bottomEdge - shiftHysteresis)
            {
                int newIndexY = Mathf.RoundToInt((p.y - roomOrigin.y) / ry);
                if (newIndexY != currentRoomIndex.y)
                {
                    currentRoomIndex.y = newIndexY;
                    roomChanged = true;
                }
            }

            targetCameraPos = CalculateRoomCenter(currentRoomIndex);

            if (useLevelBounds)
            {
                targetCameraPos.x = Mathf.Clamp(targetCameraPos.x, minPosition.x, maxPosition.x);
                targetCameraPos.y = Mathf.Clamp(targetCameraPos.y, minPosition.y, maxPosition.y);
            }

            // Smooth glide into the next room
            transform.position = Vector3.SmoothDamp(transform.position, targetCameraPos, ref currentVelocity, shiftTransitionDuration);

            if (roomChanged)
            {
                TimeEvents.OnRoomChanged?.Invoke(currentRoomIndex);
            }
        }

        private void UpdateSmoothFollow()
        {
            Vector2 p = (Vector2)target.position + targetOffset;
            Vector3 currentPos = transform.position;

            float deltaX = p.x - currentPos.x;
            float deltaY = p.y - currentPos.y;

            Vector3 targetPos = currentPos;

            if (Mathf.Abs(deltaX) > deadZone.x)
            {
                targetPos.x = p.x - (Mathf.Sign(deltaX) * deadZone.x);
            }

            if (Mathf.Abs(deltaY) > deadZone.y)
            {
                targetPos.y = p.y - (Mathf.Sign(deltaY) * deadZone.y);
            }

            if (useLevelBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minPosition.x, maxPosition.x);
                targetPos.y = Mathf.Clamp(targetPos.y, minPosition.y, maxPosition.y);
            }

            targetPos.z = cameraZ;
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * UnityEngine.Time.deltaTime);
        }

        private Vector3 CalculateRoomCenter(Vector2Int roomIndex)
        {
            float rx = Mathf.Max(4f, roomDimensions.x);
            float ry = Mathf.Max(4f, roomDimensions.y);
            return new Vector3(roomOrigin.x + (roomIndex.x * rx), roomOrigin.y + (roomIndex.y * ry), cameraZ);
        }

        /// <summary>
        /// Instantly snaps the camera to the room containing the target (useful on respawn/teleport).
        /// </summary>
        public void SnapToTargetRoom()
        {
            if (target == null) LocateTarget();
            if (target != null)
            {
                InitializeRoomPosition();
                currentVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Configure room dimensions and origin programmatically.
        /// </summary>
        public void ConfigureRoom(Vector2 dimensions, Vector2 origin, float duration = 0.35f)
        {
            roomDimensions = dimensions;
            roomOrigin = origin;
            shiftTransitionDuration = duration;
            if (target != null) InitializeRoomPosition();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.22f, 0.74f, 0.97f, 0.35f);
            float rx = Mathf.Max(4f, roomDimensions.x);
            float ry = Mathf.Max(4f, roomDimensions.y);

            // Draw current active room
            Vector3 center = CalculateRoomCenter(currentRoomIndex);
            center.z = 0f;
            Gizmos.DrawWireCube(center, new Vector3(rx, ry, 0.1f));

            // Draw neighboring rooms
            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            Gizmos.DrawWireCube(CalculateRoomCenter(currentRoomIndex + Vector2Int.right), new Vector3(rx, ry, 0.1f));
            Gizmos.DrawWireCube(CalculateRoomCenter(currentRoomIndex + Vector2Int.left), new Vector3(rx, ry, 0.1f));
        }
    }
}
