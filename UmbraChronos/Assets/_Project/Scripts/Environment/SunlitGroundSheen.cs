using System;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Renders a luminous golden ground sheen (#FFEAA7 at alpha ~0.25) on floor surfaces
    /// that are currently exposed to direct celestial sunlight.
    ///
    /// In Umbra Chronos ("Ink & Solar Flare"):
    /// - Light is lethal radiation, shadows are safe matter.
    /// - When floor surfaces are bathed in sunlight, this sheen radiates a warm amber/gold heat warning.
    /// - When covered by a monolith's solid shadow bridge or a sheltered ceiling, the sheen smoothly
    ///   fades to transparent, signaling to the player that this ground is safe.
    ///
    /// Performance & Architecture:
    /// - Event-driven: checks exposure ONLY when TimeEvents.OnSunAngleChanged fires, NOT every frame.
    /// - Zero GC allocations: uses Physics2D.RaycastNonAlloc with a static hit buffer.
    /// - Normalized scale: correctly counteracts parent transform.lossyScale to prevent stretching.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SunlitGroundSheen : MonoBehaviour
    {
        [Header("Celestial Sun & Exposure")]
        [Tooltip("Direct celestial sun angle in degrees (0 = Dawn, 90 = Zenith, 180 = Dusk).")]
        [SerializeField] private float currentSunAngle = 50f;

        [Tooltip("Transform of the celestial sun source. If null, locates SunDialController in scene.")]
        [SerializeField] private Transform sunTransform;

        [Tooltip("Layers that block sunlight from reaching the floor (Monoliths, Roofs, Walls).")]
        [SerializeField] private LayerMask shadowBlockerMask = ~0;

        [Header("Sheen Visuals")]
        [Tooltip("Luminous golden color for sunlit ground. #FFEAA7 / #F59E0B.")]
        [SerializeField] private Color sheenColor = new Color(1.0f, 0.918f, 0.655f, 0.28f);

        [Tooltip("Color when the ground is safely covered in shadow.")]
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Sorting order for the sheen overlay (1 places it just above ground platforms at 0).")]
        [SerializeField] private int sortingOrder = 1;

        [Tooltip("Vertical height/thickness of the surface sheen overlay line.")]
        [SerializeField] private float sheenThickness = 0.08f;

        [Tooltip("Vertical offset above the ground surface to prevent Z-fighting.")]
        [SerializeField] private float yOffset = 0.02f;

        [Header("Dynamics")]
        [Tooltip("Speed at which the sheen fades between sunlit and shadow states.")]
        [SerializeField] private float transitionSpeed = 5.0f;

        [Tooltip("Enable subtle pulsating heat shimmer on exposed ground.")]
        [SerializeField] private bool enablePulse = true;

        [Tooltip("Pulse speed in radians per second.")]
        [SerializeField] private float pulseSpeed = 3.0f;

        [Tooltip("Pulse intensity amplitude.")]
        [SerializeField] private float pulseIntensity = 0.15f;

        // Internal State
        private GameObject sheenObj;
        private SpriteRenderer sheenRenderer;
        private BoxCollider2D boxCollider;
        private float targetExposure = 0f; // 0 = shadow, 1 = full sunlight
        private float currentExposure = 0f;

        // Shared static buffer to guarantee zero GC allocations
        private static readonly RaycastHit2D[] s_HitBuffer = new RaycastHit2D[8];
        private static Material s_SharedSheenMaterial;
        private static Sprite s_SharedWhiteSprite;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            LocateSun();
            SetupSheenObject();
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            LocateSun();
            SetupSheenObject();
            EvaluateSunlightExposure();
            currentExposure = targetExposure;
            ApplySheenVisual(currentExposure);
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
        }

        private void Update()
        {
            // Smoothly interpolate towards target exposure
            float dt = UnityEngine.Time.deltaTime;
            if (dt > 0f)
            {
                currentExposure = Mathf.MoveTowards(currentExposure, targetExposure, transitionSpeed * dt);
            }
            else
            {
                currentExposure = targetExposure;
            }

            ApplySheenVisual(currentExposure);
        }

        private void HandleSunAngleChanged(float angle)
        {
            currentSunAngle = angle;
            EvaluateSunlightExposure();
        }

        /// <summary>
        /// Evaluates whether this platform is exposed to direct sunlight by raycasting
        /// from 3 sample points along the top walking surface towards the celestial sun.
        /// </summary>
        public void EvaluateSunlightExposure()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return;

            Vector3 lossy = transform.lossyScale;
            float topY = transform.position.y + ((boxCollider.offset.y + (boxCollider.size.y * 0.5f)) * lossy.y);
            float width = boxCollider.size.x * Mathf.Abs(lossy.x);
            float centerX = transform.position.x + (boxCollider.offset.x * lossy.x);

            // Sun direction vector pointing back towards the celestial light source
            Vector2 sunDir;
            if (sunTransform != null)
            {
                sunDir = ((Vector2)sunTransform.position - new Vector2(centerX, topY)).normalized;
            }
            else
            {
                float rad = currentSunAngle * Mathf.Deg2Rad;
                sunDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
            }

            // Test 3 sample points: left (20%), center (50%), right (80%)
            int sampleCount = 3;
            int exposedCount = 0;
            float[] sampleFractions = new float[] { 0.20f, 0.50f, 0.80f };

            for (int i = 0; i < sampleCount; i++)
            {
                float sampleX = centerX - (width * 0.5f) + (width * sampleFractions[i]);
                Vector2 rayOrigin = new Vector2(sampleX, topY + 0.05f);

                int hitCount = Physics2D.RaycastNonAlloc(rayOrigin, sunDir, s_HitBuffer, 35f, shadowBlockerMask);
                bool blocked = false;

                for (int h = 0; h < hitCount; h++)
                {
                    RaycastHit2D hit = s_HitBuffer[h];
                    if (hit.collider == null || hit.collider == boxCollider) continue;
                    if (hit.collider.isTrigger || hit.collider.CompareTag("Player")) continue;

                    // A solid occluder (Monolith, Roof, Wall, or Shadow Bridge) blocks the sunlight
                    blocked = true;
                    break;
                }

                if (!blocked) exposedCount++;
            }

            targetExposure = exposedCount / (float)sampleCount;
        }

        private void ApplySheenVisual(float exposure)
        {
            if (sheenRenderer == null) SetupSheenObject();
            if (sheenRenderer == null) return;

            float pulse = 1f;
            if (enablePulse && exposure > 0.05f)
            {
                pulse = 1.0f + (Mathf.Sin(UnityEngine.Time.time * pulseSpeed) * pulseIntensity);
            }

            Color c = Color.Lerp(shadowColor, sheenColor, exposure);
            c.a = Mathf.Clamp01(c.a * pulse);
            sheenRenderer.color = c;
            sheenRenderer.enabled = c.a > 0.005f;
        }

        private void SetupSheenObject()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return;

            Transform child = transform.Find("Sunlit_Sheen_Overlay");
            if (child == null)
            {
                sheenObj = new GameObject("Sunlit_Sheen_Overlay");
                sheenObj.transform.SetParent(transform, false);
            }
            else
            {
                sheenObj = child.gameObject;
            }

            Vector3 lossy = transform.lossyScale;
            float invLossyX = Mathf.Abs(lossy.x) > 0.0001f ? (1f / lossy.x) : 1f;
            float invLossyY = Mathf.Abs(lossy.y) > 0.0001f ? (1f / lossy.y) : 1f;

            // Position flush on top surface of the platform box collider
            float localTopY = boxCollider.offset.y + (boxCollider.size.y * 0.5f) + (yOffset * invLossyY);
            sheenObj.transform.localPosition = new Vector3(boxCollider.offset.x, localTopY, -0.02f);
            sheenObj.transform.localRotation = Quaternion.identity;

            // Counteract parent transform scale so width matches platform and thickness remains crisp
            sheenObj.transform.localScale = new Vector3(boxCollider.size.x, sheenThickness * invLossyY, 1f);

            sheenRenderer = sheenObj.GetComponent<SpriteRenderer>();
            if (sheenRenderer == null) sheenRenderer = sheenObj.AddComponent<SpriteRenderer>();

            if (s_SharedWhiteSprite == null) s_SharedWhiteSprite = CreateSharedWhiteSprite();
            sheenRenderer.sprite = s_SharedWhiteSprite;

            if (s_SharedSheenMaterial == null)
            {
                Shader uShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (uShader == null) uShader = Shader.Find("Sprites/Default");
                if (uShader != null)
                {
                    s_SharedSheenMaterial = new Material(uShader) { name = "SunlitGroundSheen_SharedMat" };
                }
            }

            if (s_SharedSheenMaterial != null)
            {
                sheenRenderer.sharedMaterial = s_SharedSheenMaterial;
            }

            sheenRenderer.sortingOrder = sortingOrder;
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;
            SunDialController dial = FindAnyObjectByType<SunDialController>();
            if (dial != null)
            {
                sunTransform = dial.transform;
            }
        }

        private static Sprite CreateSharedWhiteSprite()
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.name = "SheenWhiteTex";
            Color[] colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}
