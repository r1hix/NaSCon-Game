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
        [SerializeField] private Color beamSafeColor = new Color(0.9f, 0.65f, 0.25f, 0.30f); // Muted beam blocked on roof

        [Header("Hazard Feeler (Danger Anticipation)")]
        [Tooltip("Casts a leading feeler ray ahead of the player at floor level to warn before stepping into lethal light.")]
        [SerializeField] private bool enableHazardFeeler = true;
        [SerializeField] private float feelerDistance = 0.50f;
        [SerializeField] private Color feelerWarningColor = new Color(0.96f, 0.62f, 0.04f, 0.90f); // #F59E0B Warning Solar Amber

        [Header("Trigger Volume Mode (Optional)")]
        [Tooltip("If true, only monitors exposure when player is inside this object's Collider2D trigger.")]
        [SerializeField] private bool useTriggerBoundsOnly = false;

        // Internal State
        private Collider2D playerCollider;
        private PlayerController playerCtrl;
        private float currentExposureTimer;
        private bool isExposedToLight;
        private bool isFeelerExposed;
        private bool isOnShadowBridge;
        private bool hasTriggeredDeath;
        private bool isPlayerInTriggerZone = true;
        private bool isRewinding;

        // In-game LineRenderers for visible sunbeams
        private LineRenderer[] beamRenderers;
        private Vector2[] lastSamplePoints;
        private Vector2[] lastRayEndPoints;
        private bool[] lastSampleOccluded;

        // Preallocated raycast hit buffer (zero GC allocations)
        private static readonly RaycastHit2D[] s_HitBuffer = new RaycastHit2D[16];

        // Public Accessors
        public float ExposureTimer => currentExposureTimer;
        public float ExposureFraction => Mathf.Clamp01(currentExposureTimer / lethalGracePeriod);
        public bool IsExposedToLight => isExposedToLight;
        public bool IsFeelerExposed => isFeelerExposed;
        public bool IsOnShadowBridge => isOnShadowBridge;
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
                    playerCtrl = player;
                }
            }
            else
            {
                if (playerCollider == null) playerCollider = playerTransform.GetComponent<Collider2D>();
                if (playerCtrl == null) playerCtrl = playerTransform.GetComponent<PlayerController>();
            }
        }

        private void SetupBeams()
        {
            if (!renderVisibleBeams) return;

            int bodyCount = Mathf.Max(1, verticalSamplePoints);
            int totalCount = enableHazardFeeler ? bodyCount + 1 : bodyCount;

            // Check if beamRenderers already exist and match count
            if (beamRenderers != null && beamRenderers.Length == totalCount)
            {
                bool allValid = true;
                for (int i = 0; i < beamRenderers.Length; i++)
                {
                    if (beamRenderers[i] == null) { allValid = false; break; }
                }
                if (allValid) return;
            }

            // Clean up any stale child beams
            for (int c = transform.childCount - 1; c >= 0; c--)
            {
                Transform child = transform.GetChild(c);
                if (child.name.StartsWith("SunBeam_"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            beamRenderers = new LineRenderer[totalCount];

            // Prefer URP 2D Unlit shader for crisp beams
            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material lineMat = unlitShader != null ? new Material(unlitShader) { color = Color.white } : null;

            for (int i = 0; i < totalCount; i++)
            {
                string beamName = (i == bodyCount) ? "SunBeam_HazardFeeler" : $"SunBeam_Body_{i}";
                GameObject beamObj = new GameObject(beamName);
                beamObj.transform.SetParent(this.transform);

                LineRenderer lr = beamObj.AddComponent<LineRenderer>();
                if (lineMat != null) lr.material = lineMat;
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = 0.08f;
                lr.endWidth = 0.16f;
                lr.sortingOrder = 9; // In front of background and terrain, behind player sprite (order 10)
                lr.alignment = LineAlignment.View;
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
            if (sunTransform == null) return;
            Vector2 sunPos = sunTransform.position;
            Bounds bounds = playerCollider != null ? playerCollider.bounds : new Bounds(playerTransform.position, Vector3.one * 0.8f);

            int bodyCount = Mathf.Max(1, verticalSamplePoints);
            int totalCount = enableHazardFeeler ? bodyCount + 1 : bodyCount;

            if (lastSamplePoints == null || lastSamplePoints.Length != totalCount)
            {
                lastSamplePoints = new Vector2[totalCount];
                lastRayEndPoints = new Vector2[totalCount];
                lastSampleOccluded = new bool[totalCount];
            }

            // 1. Raycast Body Sample Points (Feet -> Chest -> Head)
            int exposedSampleCount = 0;
            for (int i = 0; i < bodyCount; i++)
            {
                float t = bodyCount == 1 ? 0.5f : (float)i / (bodyCount - 1);
                float yPos = Mathf.Lerp(bounds.min.y + 0.1f, bounds.max.y - 0.1f, t);
                Vector2 samplePoint = new Vector2(bounds.center.x, yPos);
                lastSamplePoints[i] = samplePoint;

                Vector2 toPlayer = samplePoint - sunPos;
                float distance = toPlayer.magnitude;
                Vector2 dir = toPlayer / distance;

                int hitCount = Physics2D.RaycastNonAlloc(sunPos, dir, s_HitBuffer, distance, shadowBlockerMask);
                bool occluded = false;
                for (int h = 0; h < hitCount; h++)
                {
                    var col = s_HitBuffer[h].collider;
                    if (col == null || col.isTrigger) continue;
                    if (col == playerCollider || col.transform.IsChildOf(playerTransform)) continue;
                    if (col.transform.IsChildOf(sunTransform)) continue;

                    occluded = true;
                    lastRayEndPoints[i] = s_HitBuffer[h].point;
                    break;
                }

                if (!occluded)
                {
                    exposedSampleCount++;
                    lastRayEndPoints[i] = samplePoint;
                }

                lastSampleOccluded[i] = occluded;
            }

            // 2. Raycast Hazard Feeler (Leading Edge Ground Detector)
            isFeelerExposed = false;
            if (enableHazardFeeler && totalCount > bodyCount)
            {
                int feelerIdx = bodyCount;
                float facingDir = 1f;
                if (playerCtrl != null)
                {
                    facingDir = playerCtrl.IsFacingRight ? 1f : -1f;
                }
                else if (playerTransform != null)
                {
                    facingDir = playerTransform.localScale.x >= 0 ? 1f : -1f;
                }

                Vector2 feelerPoint = new Vector2(bounds.center.x + (facingDir * feelerDistance), bounds.min.y + 0.1f);
                lastSamplePoints[feelerIdx] = feelerPoint;

                Vector2 toFeeler = feelerPoint - sunPos;
                float distance = toFeeler.magnitude;
                Vector2 dir = toFeeler / distance;

                int hitCount = Physics2D.RaycastNonAlloc(sunPos, dir, s_HitBuffer, distance, shadowBlockerMask);
                bool occluded = false;
                for (int h = 0; h < hitCount; h++)
                {
                    var col = s_HitBuffer[h].collider;
                    if (col == null || col.isTrigger) continue;
                    if (col == playerCollider || col.transform.IsChildOf(playerTransform)) continue;
                    if (col.transform.IsChildOf(sunTransform)) continue;

                    occluded = true;
                    lastRayEndPoints[feelerIdx] = s_HitBuffer[h].point;
                    break;
                }

                if (!occluded)
                {
                    lastRayEndPoints[feelerIdx] = feelerPoint;
                    isFeelerExposed = true;
                }
                lastSampleOccluded[feelerIdx] = occluded;
            }

            // 3. Check if player is safely standing on a solid shadow bridge
            isOnShadowBridge = false;
            Collider2D[] footHits = Physics2D.OverlapPointAll(new Vector2(bounds.center.x, bounds.min.y - 0.05f), shadowBlockerMask);
            for (int f = 0; f < footHits.Length; f++)
            {
                if (footHits[f].GetComponent<DynamicShadowCollider>() != null)
                {
                    isOnShadowBridge = true;
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
            if (!renderVisibleBeams || sunTransform == null) return;

            int bodyCount = Mathf.Max(1, verticalSamplePoints);
            int totalCount = enableHazardFeeler ? bodyCount + 1 : bodyCount;

            if (beamRenderers == null || beamRenderers.Length != totalCount)
            {
                SetupBeams();
                if (beamRenderers == null) return;
            }

            Vector3 sunPos = sunTransform.position;

            for (int i = 0; i < beamRenderers.Length; i++)
            {
                LineRenderer lr = beamRenderers[i];
                if (lr == null) continue;

                if (hideAll || lastRayEndPoints == null || i >= lastRayEndPoints.Length)
                {
                    lr.enabled = false;
                    continue;
                }

                // Hazard Feeler Beam (Index = bodyCount)
                if (i == bodyCount)
                {
                    if (isOnShadowBridge || !isFeelerExposed)
                    {
                        lr.enabled = false;
                        continue;
                    }

                    lr.enabled = true;
                    lr.SetPosition(0, sunPos);
                    lr.SetPosition(1, new Vector3(lastRayEndPoints[i].x, lastRayEndPoints[i].y, 0f));

                    float pulse = 0.08f + (Mathf.Sin(UnityEngine.Time.time * 14f) * 0.025f);
                    lr.startColor = new Color(1f, 0.88f, 0.50f, 0.85f);
                    lr.endColor = feelerWarningColor; // #F59E0B warning amber
                    lr.startWidth = pulse * 0.6f;
                    lr.endWidth = pulse * 1.3f;
                    continue;
                }

                // Body Beams (Head, Core, Feet)
                lr.enabled = true;
                lr.SetPosition(0, sunPos);
                lr.SetPosition(1, new Vector3(lastRayEndPoints[i].x, lastRayEndPoints[i].y, 0f));

                if (isOnShadowBridge)
                {
                    // Grounded safely on solid shadow matter (cyan rim aesthetic)
                    lr.startColor = new Color(0.22f, 0.74f, 0.97f, 0.35f);
                    lr.endColor = new Color(0.22f, 0.74f, 0.97f, 0.10f);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.08f;
                }
                else if (lastSampleOccluded != null && lastSampleOccluded[i])
                {
                    // Blocked beam stopping safely on shelter, monolith, or wall
                    lr.startColor = new Color(beamSafeColor.r, beamSafeColor.g, beamSafeColor.b, 0.40f);
                    lr.endColor = new Color(beamSafeColor.r, beamSafeColor.g, beamSafeColor.b, 0.12f);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.10f;
                }
                else
                {
                    // Searing lethal light striking player body!
                    float pulse = 0.11f + (Mathf.Sin(UnityEngine.Time.time * 20f) * 0.035f);
                    lr.startColor = Color.white;
                    lr.endColor = beamLethalColor;
                    lr.startWidth = pulse;
                    lr.endWidth = pulse * 1.6f;
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

        [Header("Editor Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || sunTransform == null || lastSamplePoints == null) return;

            Vector3 sunPos = sunTransform.position;
            int bodyCount = Mathf.Max(1, verticalSamplePoints);
            for (int i = 0; i < lastSamplePoints.Length; i++)
            {
                if (i < bodyCount)
                {
                    bool isSafe = (lastSampleOccluded != null && lastSampleOccluded[i]) || isOnShadowBridge;
                    Gizmos.color = isSafe ? new Color(0.2f, 1f, 0.2f, 0.6f) : new Color(1f, 0.2f, 0.1f, 0.85f);
                }
                else
                {
                    // Hazard feeler ray (amber warning when exposed to light ahead)
                    Gizmos.color = isFeelerExposed ? new Color(0.96f, 0.62f, 0.04f, 0.9f) : new Color(0.2f, 0.8f, 1f, 0.3f);
                }
                Gizmos.DrawLine(sunPos, lastRayEndPoints != null ? (Vector3)lastRayEndPoints[i] : (Vector3)lastSamplePoints[i]);
            }
        }
    }
}
