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

        [Header("Volumetric Godray Hazard System")]
        [Tooltip("Render volumetric godray light shafts that dynamically frame danger boundaries.")]
        [SerializeField] private bool renderGodrayHazard = true;

        [Tooltip("Feathered light shaft sprite.")]
        [SerializeField] private Sprite godraySprite;

        [Tooltip("Floating celestial dust mote sprite.")]
        [SerializeField] private Sprite moteSprite;

        [Tooltip("Width of the volumetric godray shaft.")]
        [SerializeField] private float godrayWidth = 2.4f;

        [SerializeField] private Color godraySafeColor = new Color(1f, 0.94f, 0.72f, 0.22f);      // Soft celestial gold on roof
        [SerializeField] private Color godrayThresholdColor = new Color(0.96f, 0.62f, 0.04f, 0.48f); // #F59E0B Warning amber at shelter edge
        [SerializeField] private Color godrayLethalColor = new Color(1f, 0.96f, 0.85f, 0.75f);       // Searing solar flare on player
        [SerializeField] private Color godrayBridgeColor = new Color(0.22f, 0.74f, 0.97f, 0.35f);     // Safe cyan on solid shadow

        [Header("Trigger Volume Mode (Optional)")]
        [Tooltip("If true, only monitors exposure when player is inside this object's Collider2D trigger.")]
        [SerializeField] private bool useTriggerBoundsOnly = false;

        // Internal State
        private Collider2D playerCollider;
        private PlayerController playerCtrl;
        private float currentExposureTimer;
        private bool isExposedToLight;
        private bool isOnShadowBridge;
        private bool hasTriggeredDeath;
        private bool isPlayerInTriggerZone = true;
        private bool isRewinding;

        // Volumetric Godray Visuals
        private Transform directShaftTransform;
        private SpriteRenderer directShaftRenderer;
        private Transform thresholdShaftTransform;
        private SpriteRenderer thresholdShaftRenderer;
        private ParticleSystem contactMotesParticles;

        private const float BaseSpriteHeightUnits = 16f; // 1024px / 64 PPU = 16 units

        // Preallocated raycast hit buffer (zero GC allocations)
        private static readonly RaycastHit2D[] s_HitBuffer = new RaycastHit2D[16];

        // Public Accessors
        public float ExposureTimer => currentExposureTimer;
        public float ExposureFraction => Mathf.Clamp01(currentExposureTimer / lethalGracePeriod);
        public bool IsExposedToLight => isExposedToLight;
        public bool IsOnShadowBridge => isOnShadowBridge;
        public bool HasTriggeredDeath => hasTriggeredDeath;

        private void Awake()
        {
            LocateDependencies();
            SetupGodrayVisuals();
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

        private void LoadSprites()
        {
#if UNITY_EDITOR
            if (godraySprite == null)
            {
                godraySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Environment/Sprite_Godray_Shaft.png");
            }
            if (moteSprite == null)
            {
                moteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Environment/Sprite_Solar_Mote.png");
            }
#endif
        }

        private void SetupGodrayVisuals()
        {
            if (!renderGodrayHazard) return;

            LoadSprites();

            // Clean up any old LineRenderer child objects
            for (int c = transform.childCount - 1; c >= 0; c--)
            {
                Transform child = transform.GetChild(c);
                if (child.name.StartsWith("SunBeam_"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material godrayMat = unlitShader != null ? new Material(unlitShader) { color = Color.white } : null;

            // 1. Direct Godray Shaft (Tracks player / roof above player)
            if (directShaftTransform == null)
            {
                Transform t = transform.Find("Godray_Direct_Shaft");
                GameObject obj = t != null ? t.gameObject : new GameObject("Godray_Direct_Shaft");
                obj.transform.SetParent(this.transform, false);
                directShaftTransform = obj.transform;

                directShaftRenderer = obj.GetComponent<SpriteRenderer>();
                if (directShaftRenderer == null) directShaftRenderer = obj.AddComponent<SpriteRenderer>();

                if (godraySprite != null) directShaftRenderer.sprite = godraySprite;
                if (godrayMat != null) directShaftRenderer.material = godrayMat;
                directShaftRenderer.sortingLayerName = "Default";
                directShaftRenderer.sortingOrder = 7; // In front of platforms (0), behind player sprite (10)
            }

            // 2. Threshold Godray Shaft (Pours down past the shelter roof edge onto the floor)
            if (thresholdShaftTransform == null)
            {
                Transform t = transform.Find("Godray_Threshold_Shaft");
                GameObject obj = t != null ? t.gameObject : new GameObject("Godray_Threshold_Shaft");
                obj.transform.SetParent(this.transform, false);
                thresholdShaftTransform = obj.transform;

                thresholdShaftRenderer = obj.GetComponent<SpriteRenderer>();
                if (thresholdShaftRenderer == null) thresholdShaftRenderer = obj.AddComponent<SpriteRenderer>();

                if (godraySprite != null) thresholdShaftRenderer.sprite = godraySprite;
                if (godrayMat != null) thresholdShaftRenderer.material = godrayMat;
                thresholdShaftRenderer.sortingLayerName = "Default";
                thresholdShaftRenderer.sortingOrder = 7;
            }

            // 3. Ground Contact Motes Particle System
            if (contactMotesParticles == null)
            {
                Transform t = transform.Find("Godray_Contact_Motes");
                GameObject obj = t != null ? t.gameObject : new GameObject("Godray_Contact_Motes");
                obj.transform.SetParent(this.transform, false);

                contactMotesParticles = obj.GetComponent<ParticleSystem>();
                if (contactMotesParticles == null) contactMotesParticles = obj.AddComponent<ParticleSystem>();

                contactMotesParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystemRenderer psRenderer = obj.GetComponent<ParticleSystemRenderer>();
                if (godrayMat != null)
                {
                    Material moteMat = new Material(godrayMat);
                    if (moteSprite != null) moteMat.mainTexture = moteSprite.texture;
                    psRenderer.material = moteMat;
                }
                psRenderer.sortingOrder = 8; // In front of godray shaft

                var main = contactMotesParticles.main;
                main.duration = 2f;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
                main.startColor = new Color(1f, 0.94f, 0.65f, 0.75f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 30;
                main.playOnAwake = true;

                var emission = contactMotesParticles.emission;
                emission.rateOverTime = 8f;

                var shape = contactMotesParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(godrayWidth * 0.8f, 0.2f, 1f);

                var vel = contactMotesParticles.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                vel.y = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

                var col = contactMotesParticles.colorOverLifetime;
                col.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(new Color(0.96f, 0.62f, 0.04f), 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
                );
                col.color = grad;

                contactMotesParticles.Play();
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
                UpdateGodrayVisuals(true);
                return;
            }

            EvaluateSunlightExposure();
            UpdateGodrayVisuals(false);
        }

        // Cached collider for active shelter roof
        private Collider2D currentBlockingRoof;

        private void EvaluateSunlightExposure()
        {
            if (sunTransform == null) return;
            Vector2 sunPos = sunTransform.position;
            Bounds bounds = playerCollider != null ? playerCollider.bounds : new Bounds(playerTransform.position, Vector3.one * 0.8f);

            int samples = Mathf.Max(1, verticalSamplePoints);
            int exposedSampleCount = 0;
            currentBlockingRoof = null;

            for (int i = 0; i < samples; i++)
            {
                float t = samples == 1 ? 0.5f : (float)i / (samples - 1);
                float yPos = Mathf.Lerp(bounds.min.y + 0.1f, bounds.max.y - 0.1f, t);
                Vector2 samplePoint = new Vector2(bounds.center.x, yPos);

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
                    if (currentBlockingRoof == null) currentBlockingRoof = col;
                    break;
                }

                if (!occluded)
                {
                    exposedSampleCount++;
                }
            }

            // Check if player is safely standing on a solid shadow bridge
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

        private void UpdateGodrayVisuals(bool hideAll)
        {
            if (!renderGodrayHazard || sunTransform == null) return;
            if (directShaftTransform == null || thresholdShaftTransform == null)
            {
                SetupGodrayVisuals();
                if (directShaftTransform == null) return;
            }

            if (hideAll)
            {
                if (directShaftRenderer != null) directShaftRenderer.enabled = false;
                if (thresholdShaftRenderer != null) thresholdShaftRenderer.enabled = false;
                if (contactMotesParticles != null)
                {
                    var em = contactMotesParticles.emission;
                    em.rateOverTime = 0f;
                }
                return;
            }

            Vector2 sunPos = sunTransform.position;
            Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            float time = UnityEngine.Time.time;

            // 1. DIRECT SHAFT (Volumetric light aligned with player's X coordinate)
            Vector2 playerTarget = new Vector2(playerPos.x, playerPos.y - 0.2f);
            Vector2 directDir = (playerTarget - sunPos).normalized;
            float directAngleZ = Mathf.Atan2(directDir.y, directDir.x) * Mathf.Rad2Deg + 90f;

            directShaftTransform.position = sunPos;
            directShaftTransform.rotation = Quaternion.Euler(0f, 0f, directAngleZ);

            float directHitDistance = 35f;
            int directHits = Physics2D.RaycastNonAlloc(sunPos, directDir, s_HitBuffer, 35f, shadowBlockerMask);
            Collider2D blockingRoof = null;

            for (int h = 0; h < directHits; h++)
            {
                var col = s_HitBuffer[h].collider;
                if (col == null || col.isTrigger) continue;
                if (col == playerCollider || col.transform.IsChildOf(playerTransform)) continue;
                if (col.transform.IsChildOf(sunTransform)) continue;

                directHitDistance = s_HitBuffer[h].distance;
                blockingRoof = col;
                break;
            }

            float directScaleY = Mathf.Max(0.1f, directHitDistance / BaseSpriteHeightUnits);
            directShaftTransform.localScale = new Vector3(godrayWidth, directScaleY, 1f);

            if (directShaftRenderer != null)
            {
                directShaftRenderer.enabled = true;
                if (isOnShadowBridge)
                {
                    directShaftRenderer.color = godrayBridgeColor; // Protected cyan
                }
                else if (isExposedToLight)
                {
                    // Searing solar bloom on player!
                    float pulse = 0.70f + (Mathf.Sin(time * 16f) * 0.15f);
                    Color c = godrayLethalColor;
                    c.a = godrayLethalColor.a * pulse;
                    directShaftRenderer.color = c;
                }
                else
                {
                    // Soft celestial gold stopping safely on roof
                    directShaftRenderer.color = godraySafeColor;
                }
            }

            // 2. THRESHOLD SHAFT (Pours down past the shelter roof edge onto the floor)
            if (thresholdShaftRenderer != null && thresholdShaftTransform != null)
            {
                if (isExposedToLight || isOnShadowBridge || blockingRoof == null)
                {
                    // When in direct sun or on shadow bridge, direct shaft handles the visual
                    thresholdShaftRenderer.enabled = false;
                    if (contactMotesParticles != null)
                    {
                        var em = contactMotesParticles.emission;
                        em.rateOverTime = isExposedToLight ? 14f : 0f;
                        if (isExposedToLight)
                        {
                            contactMotesParticles.transform.position = new Vector3(playerPos.x, playerPos.y - 0.7f, 0f);
                        }
                    }
                }
                else
                {
                    // Player is shaded! Find the active shadow lip/edge of the blocking roof:
                    Bounds rBounds = blockingRoof.bounds;
                    float edgeX = (sunPos.x < playerPos.x) ? rBounds.max.x : rBounds.min.x;
                    float edgeY = rBounds.min.y;

                    Vector2 edgePos = new Vector2(edgeX, edgeY);
                    Vector2 edgeDir = (edgePos - sunPos).normalized;

                    // Raycast from edgePos along edgeDir to find the exact floor contact point
                    int edgeHits = Physics2D.RaycastNonAlloc(edgePos, edgeDir, s_HitBuffer, 30f, shadowBlockerMask);
                    Vector2 floorPoint = edgePos + (edgeDir * 16f);

                    for (int h = 0; h < edgeHits; h++)
                    {
                        var col = s_HitBuffer[h].collider;
                        if (col == null || col.isTrigger || col == blockingRoof) continue;
                        if (col == playerCollider || col.transform.IsChildOf(playerTransform)) continue;

                        floorPoint = s_HitBuffer[h].point;
                        break;
                    }

                    float distToHazard = Mathf.Abs(floorPoint.x - playerPos.x);

                    if (distToHazard <= 9.0f)
                    {
                        thresholdShaftRenderer.enabled = true;
                        float totalDist = (floorPoint - sunPos).magnitude;
                        float threshAngleZ = Mathf.Atan2(edgeDir.y, edgeDir.x) * Mathf.Rad2Deg + 90f;

                        thresholdShaftTransform.position = sunPos;
                        thresholdShaftTransform.rotation = Quaternion.Euler(0f, 0f, threshAngleZ);

                        float scaleY = Mathf.Max(0.1f, totalDist / BaseSpriteHeightUnits);
                        thresholdShaftTransform.localScale = new Vector3(godrayWidth, scaleY, 1f);

                        // Pulse amber warning as player approaches within 4 meters
                        float proximityT = Mathf.Clamp01(1f - (distToHazard / 4f));
                        Color col = Color.Lerp(godraySafeColor, godrayThresholdColor, proximityT);
                        float pulse = 0.90f + (Mathf.Sin(time * (6f + (proximityT * 8f))) * 0.1f);
                        col.a = col.a * pulse;
                        thresholdShaftRenderer.color = col;

                        // Contact motes at the floor threshold
                        if (contactMotesParticles != null)
                        {
                            var em = contactMotesParticles.emission;
                            em.rateOverTime = 5f + (proximityT * 8f);
                            contactMotesParticles.transform.position = new Vector3(floorPoint.x, floorPoint.y + 0.1f, 0f);
                        }
                    }
                    else
                    {
                        thresholdShaftRenderer.enabled = false;
                        if (contactMotesParticles != null)
                        {
                            var em = contactMotesParticles.emission;
                            em.rateOverTime = 0f;
                        }
                    }
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
            if (!showDebugGizmos || sunTransform == null) return;

            Vector3 sunPos = sunTransform.position;
            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

            // Direct celestial ray to player
            Gizmos.color = isExposedToLight ? new Color(1f, 0.2f, 0.1f, 0.85f) : (isOnShadowBridge ? new Color(0.2f, 0.8f, 1f, 0.6f) : new Color(0.2f, 1f, 0.2f, 0.4f));
            Gizmos.DrawLine(sunPos, playerPos);

            // If a threshold shaft is active, draw the threshold ray in warning amber
            if (thresholdShaftTransform != null && thresholdShaftRenderer != null && thresholdShaftRenderer.enabled)
            {
                Gizmos.color = new Color(0.96f, 0.62f, 0.04f, 0.9f);
                Gizmos.DrawRay(thresholdShaftTransform.position, -thresholdShaftTransform.up * 16f);
            }
        }
    }
}
