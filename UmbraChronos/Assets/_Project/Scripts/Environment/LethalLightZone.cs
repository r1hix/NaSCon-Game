using System;
using UnityEngine;
using UmbraChronos.Core;
using UmbraChronos.Player;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Monitors celestial sunlight exposure on the player.
    /// In Umbra Chronos, celestial light is lethal radiation:
    /// - Standing in direct, unshaded sunlight builds lethal exposure.
    /// - If exposed continuously for greater than lethalGracePeriod (default 0.85s),
    ///   the player is vaporized and TimeEvents.OnPlayerDied is triggered.
    /// - Standing inside the geometric shadow of a monolith/obstacle blocks the rays,
    ///   allowing the player to cool down and recover.
    /// - Features in-game visible celestial sunbeams rendered via LineRenderers.
    /// </summary>
    public class LethalLightZone : MonoBehaviour
    {
        [Header("Light Source")]
        [Tooltip("The celestial orbital Sun transform. If null, automatically locates a light or object tagged 'Sun'.")]
        [SerializeField] private Transform sunTransform;

        [Header("Target & Detection")]
        [Tooltip("The player transform. If null, automatically locates PlayerController in the scene.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Layers that block celestial light (Monoliths, solid shadow casters, terrain, pushable blocks).")]
        [SerializeField] private LayerMask shadowBlockerMask = ~0;

        [Tooltip("Sample points checked on player bounds. More points = higher precision.")]
        [SerializeField] private int verticalSamplePoints = 3;

        [Header("Exposure Timings")]
        [Tooltip("Grace period (seconds) of direct light before fatal vaporization.")]
        [SerializeField] private float lethalGracePeriod = 0.85f;

        [Tooltip("Speed (multiplier) at which exposure dissipates when ducking back into shadow.")]
        [SerializeField] private float recoveryRate = 3.0f;

        [Header("In-Game Visible Celestial Rays")]
        [Tooltip("Render visible celestial light beams in Game view.")]
        [SerializeField] private bool renderVisibleBeams = true;

        [SerializeField] private Color beamLethalColor = new Color(1f, 0.94f, 0.5f, 0.85f); // Searing solar flare
        [SerializeField] private Color beamSafeColor = new Color(0.9f, 0.65f, 0.25f, 0.22f); // Muted beam blocked on roof

        [Header("Trigger Volume Mode (Optional)")]
        [Tooltip("If true, only monitors exposure when player is inside this object's Collider2D trigger.")]
        [SerializeField] private bool useTriggerBoundsOnly = false;

        // Internal State
        private Collider2D playerCollider;
        private float currentExposureTimer;
        private bool isExposedToLight;
        private bool hasTriggeredDeath;
        private bool isPlayerInTriggerZone = true;
        private bool isRewinding;

        // In-game LineRenderers for visible sunbeams
        private LineRenderer[] beamRenderers;
        private Vector2[] lastSamplePoints;
        private Vector2[] lastRayEndPoints;
        private bool[] lastSampleOccluded;

        // Public Accessors
        public float ExposureTimer => currentExposureTimer;
        public float ExposureFraction => Mathf.Clamp01(currentExposureTimer / lethalGracePeriod);
        public bool IsExposedToLight => isExposedToLight;
        public bool HasTriggeredDeath => hasTriggeredDeath;

        private void Awake()
        {
            LocateDependencies();
            SetupBeams();
        }

        [Header("Celestial Sun & Light")]
        [SerializeField] private float currentSunAngle = 50f;

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void HandleSunAngleChanged(float angle)
        {
            currentSunAngle = angle;
        }

        private void LocateDependencies()
        {
            // Auto-locate Sun: prioritize SunDialController, then tag, then any 2D light
            if (sunTransform == null)
            {
                var dial = UnityEngine.Object.FindAnyObjectByType<SunDialController>();
                if (dial != null)
                {
                    sunTransform = dial.transform;
                }
                else
                {
                    try
                    {
                        GameObject sunObj = GameObject.FindWithTag("Sun");
                        if (sunObj != null)
                        {
                            sunTransform = sunObj.transform;
                        }
                    }
                    catch (UnityException) { }

                    if (sunTransform == null)
                    {
                        var anyLight = UnityEngine.Object.FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
                        if (anyLight != null)
                        {
                            sunTransform = anyLight.transform;
                        }
                    }
                }
            }

            // Auto-locate Player
            if (playerTransform == null)
            {
                var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerCollider = player.GetComponent<Collider2D>();
                }
            }
            else if (playerCollider == null)
            {
                playerCollider = playerTransform.GetComponent<Collider2D>();
            }
        }

        private void SetupBeams()
        {
            if (!renderVisibleBeams) return;

            int count = Mathf.Max(1, verticalSamplePoints);
            beamRenderers = new LineRenderer[count];

            // Prefer URP 2D Unlit shader for crisp beams
            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material lineMat = unlitShader != null ? new Material(unlitShader) : null;

            for (int i = 0; i < count; i++)
            {
                GameObject beamObj = new GameObject($"SunBeam_{i}");
                beamObj.transform.SetParent(this.transform);

                LineRenderer lr = beamObj.AddComponent<LineRenderer>();
                if (lineMat != null) lr.material = lineMat;
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = 0.08f;
                lr.endWidth = 0.16f;
                lr.sortingOrder = 5;
                beamRenderers[i] = lr;
            }
        }

        private void FixedUpdate()
        {
            if (isRewinding) return;

            if (sunTransform == null || playerTransform == null)
            {
                LocateDependencies();
                if (sunTransform == null || playerTransform == null) return;
            }

            if (useTriggerBoundsOnly && !isPlayerInTriggerZone)
            {
                CooldownExposure();
                UpdateVisibleBeams(true);
                return;
            }

            EvaluateSunlightExposure();
            UpdateVisibleBeams(false);
        }

        private void EvaluateSunlightExposure()
        {
            Bounds bounds = playerCollider != null ? playerCollider.bounds : new Bounds(playerTransform.position, Vector3.one * 0.8f);

            // Celestial direction: sun angle theta determines direction of light rays globally
            float rad = Mathf.Clamp(currentSunAngle, 15f, 165f) * Mathf.Deg2Rad;
            Vector2 lightDir = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
            Vector2 toSunDir = -lightDir; // Direction upwards towards celestial sun

            int samples = Mathf.Max(1, verticalSamplePoints);
            if (lastSamplePoints == null || lastSamplePoints.Length != samples)
            {
                lastSamplePoints = new Vector2[samples];
                lastRayEndPoints = new Vector2[samples];
                lastSampleOccluded = new bool[samples];
            }

            int exposedSampleCount = 0;

            for (int i = 0; i < samples; i++)
            {
                float t = samples == 1 ? 0.5f : (float)i / (samples - 1);
                // Sample from bottom to top of player body
                float yPos = Mathf.Lerp(bounds.min.y + 0.1f, bounds.max.y - 0.1f, t);
                Vector2 samplePoint = new Vector2(bounds.center.x, yPos);
                lastSamplePoints[i] = samplePoint;

                // Raycast upwards towards the celestial light source
                RaycastHit2D hit = Physics2D.Raycast(samplePoint + toSunDir * 0.05f, toSunDir, 40f, shadowBlockerMask);

                bool occluded = false;
                if (hit.collider != null)
                {
                    if (hit.collider != playerCollider && !hit.collider.transform.IsChildOf(playerTransform))
                    {
                        occluded = true;
                        lastRayEndPoints[i] = hit.point;
                    }
                }

                if (!occluded)
                {
                    exposedSampleCount++;
                    lastRayEndPoints[i] = samplePoint;
                }

                lastSampleOccluded[i] = occluded;
            }

            // Check if player is safely standing on a solid shadow bridge
            Collider2D[] footHits = Physics2D.OverlapPointAll(new Vector2(bounds.center.x, bounds.min.y - 0.05f), shadowBlockerMask);
            for (int f = 0; f < footHits.Length; f++)
            {
                if (footHits[f].GetComponent<DynamicShadowCollider>() != null)
                {
                    exposedSampleCount = 0;
                    break;
                }
            }

            isExposedToLight = exposedSampleCount > 0;

            if (isExposedToLight)
            {
                if (!hasTriggeredDeath)
                {
                    currentExposureTimer += UnityEngine.Time.fixedDeltaTime;
                    float fraction = ExposureFraction;
                    TimeEvents.OnSunlightExposureChanged?.Invoke(fraction);

                    if (currentExposureTimer >= lethalGracePeriod)
                    {
                        hasTriggeredDeath = true;
                        TimeEvents.OnPlayerDied?.Invoke();
                    }
                }
            }
            else
            {
                CooldownExposure();
            }
        }

        private void CooldownExposure()
        {
            if (currentExposureTimer > 0f)
            {
                currentExposureTimer = Mathf.Max(0f, currentExposureTimer - (recoveryRate * UnityEngine.Time.fixedDeltaTime));
                TimeEvents.OnSunlightExposureChanged?.Invoke(ExposureFraction);
            }
        }

        private void UpdateVisibleBeams(bool hideAll)
        {
            if (!renderVisibleBeams || beamRenderers == null) return;

            float rad = Mathf.Clamp(currentSunAngle, 15f, 165f) * Mathf.Deg2Rad;
            Vector2 toSunDir = new Vector2(-Mathf.Cos(rad), Mathf.Sin(rad));

            for (int i = 0; i < beamRenderers.Length; i++)
            {
                LineRenderer lr = beamRenderers[i];
                if (lr == null) continue;

                if (hideAll || lastRayEndPoints == null || i >= lastRayEndPoints.Length || lastSamplePoints == null || i >= lastSamplePoints.Length)
                {
                    lr.enabled = false;
                    continue;
                }

                Vector3 skyStart = (Vector3)(lastSamplePoints[i] + toSunDir * 20f);
                Vector3 endPoint = new Vector3(lastRayEndPoints[i].x, lastRayEndPoints[i].y, 0f);

                lr.enabled = true;
                lr.SetPosition(0, skyStart);
                lr.SetPosition(1, endPoint);

                bool occluded = lastSampleOccluded != null && lastSampleOccluded[i];
                if (occluded)
                {
                    // Blocked beam stopping safely on shelter or monolith
                    lr.startColor = new Color(beamSafeColor.r, beamSafeColor.g, beamSafeColor.b, 0.4f);
                    lr.endColor = new Color(beamSafeColor.r, beamSafeColor.g, beamSafeColor.b, 0.1f);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.1f;
                }
                else
                {
                    // Searing lethal light striking player!
                    float pulse = 0.12f + (Mathf.Sin(UnityEngine.Time.time * 20f) * 0.03f);
                    lr.startColor = Color.white;
                    lr.endColor = beamLethalColor;
                    lr.startWidth = pulse;
                    lr.endWidth = pulse * 1.5f;
                }
            }
        }

        private void HandleRewindStateChanged(bool active)
        {
            isRewinding = active;
            if (active && hasTriggeredDeath)
            {
                hasTriggeredDeath = false;
                currentExposureTimer = 0f;
                TimeEvents.OnSunlightExposureChanged?.Invoke(0f);
            }
        }

        private void HandlePlayerRespawned()
        {
            hasTriggeredDeath = false;
            currentExposureTimer = 0f;
            TimeEvents.OnSunlightExposureChanged?.Invoke(0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isPlayerInTriggerZone = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isPlayerInTriggerZone = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (sunTransform == null || lastSamplePoints == null) return;

            Vector3 sunPos = sunTransform.position;
            for (int i = 0; i < lastSamplePoints.Length; i++)
            {
                bool isSafe = lastSampleOccluded != null && lastSampleOccluded[i];
                Gizmos.color = isSafe ? new Color(0.2f, 1f, 0.2f, 0.6f) : new Color(1f, 0.2f, 0.1f, 0.85f);
                Gizmos.DrawLine(sunPos, lastRayEndPoints != null ? (Vector3)lastRayEndPoints[i] : (Vector3)lastSamplePoints[i]);
            }
        }
    }
}
