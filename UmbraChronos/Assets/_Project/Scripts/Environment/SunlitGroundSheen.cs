using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Renders a luminous golden ground sheen (#FFEAA7 at ~0.15 alpha) on floor surfaces
    /// that are currently exposed to direct sunlight. This gives the player an immediate
    /// visual cue: glowing warm floors are lethal, dark unlit floors are safe.
    ///
    /// Attach this to any ground/floor GameObject. It will raycast from the sun to
    /// determine whether this surface is in direct sunlight or in shadow, and render
    /// a glowing overlay accordingly.
    /// </summary>
    [ExecuteAlways]
    public class SunlitGroundSheen : MonoBehaviour
    {
        [Header("Sun Source")]
        [Tooltip("Transform of the celestial sun. If null, auto-locates SunDialController.")]
        [SerializeField] private Transform sunTransform;

        [Header("Sheen Visual")]
        [Tooltip("Luminous golden color for sunlit ground. #FFEAA7 at ~0.15 alpha.")]
        [SerializeField] private Color sheenColor = new Color(1.0f, 0.918f, 0.655f, 0.15f); // #FFEAA7

        [Tooltip("Color when the ground is fully in shadow (typically fully transparent).")]
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0f);

        [Tooltip("Sorting order for the sheen overlay (above ground at 0 but below player).")]
        [SerializeField] private int sortingOrder = 1;

        [Tooltip("Vertical offset above the ground surface for the sheen plane.")]
        [SerializeField] private float yOffset = 0.02f;

        [Header("Raycast Detection")]
        [Tooltip("Number of sample points along the ground surface to check for sunlight exposure.")]
        [SerializeField] private int samplePoints = 5;

        [Tooltip("Layers that block sunlight (roofs, monoliths, walls).")]
        [SerializeField] private LayerMask shadowBlockerMask = ~0;

        [Header("Transition")]
        [Tooltip("Speed at which the sheen fades in/out as sunlight changes.")]
        [SerializeField] private float transitionSpeed = 4.0f;

        [Header("Pulsing Effect")]
        [Tooltip("Enable subtle pulsing to make the sunlit ground feel alive and dangerous.")]
        [SerializeField] private bool enablePulse = true;

        [Tooltip("Pulse speed (oscillations per second).")]
        [SerializeField] private float pulseSpeed = 1.5f;

        [Tooltip("Pulse intensity range (0 = no pulse, 0.5 = strong pulse).")]
        [SerializeField] private float pulseIntensity = 0.06f;

        // Internal state
        private SpriteRenderer sheenRenderer;
        private GameObject sheenObj;
        private float currentExposure = 0f; // 0 = shadow, 1 = full sunlight
        private float currentSunAngle = 50f;

        private void Awake()
        {
            LocateSun();
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            CreateSheen();
            UpdateSheen();
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
            CleanupSheen();
        }

        private void Update()
        {
            if (sunTransform == null) LocateSun();
            UpdateSheen();
        }

        private void HandleSunAngleChanged(float angle)
        {
            currentSunAngle = angle;
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;

            var dial = Object.FindAnyObjectByType<SunDialController>();
            if (dial != null)
            {
                sunTransform = dial.transform;
                return;
            }

            try
            {
                GameObject sunObj = GameObject.FindWithTag("Sun");
                if (sunObj != null)
                {
                    sunTransform = sunObj.transform;
                    return;
                }
            }
            catch (UnityException) { }
        }

        private void CreateSheen()
        {
            if (sheenObj != null) return;

            sheenObj = new GameObject("Sunlit_Ground_Sheen");
            sheenObj.transform.SetParent(transform, false);

            sheenRenderer = sheenObj.AddComponent<SpriteRenderer>();

            // Use the project's square sprite or generate one
            Sprite square = FindSquareSprite();
            if (square != null) sheenRenderer.sprite = square;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                sheenRenderer.material = new Material(unlitShader);
            }

            sheenRenderer.color = shadowColor; // Start invisible
            sheenRenderer.sortingOrder = sortingOrder;
        }

        private void UpdateSheen()
        {
            if (sheenRenderer == null || sheenObj == null)
            {
                CreateSheen();
                if (sheenRenderer == null) return;
            }

            // Calculate floor bounds
            Collider2D col = GetComponent<Collider2D>();
            SpriteRenderer parentSr = GetComponent<SpriteRenderer>();

            float minX, maxX, surfaceY;

            if (col != null)
            {
                Bounds b = col.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                surfaceY = b.max.y;
            }
            else if (parentSr != null)
            {
                Bounds b = parentSr.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                surfaceY = b.max.y;
            }
            else
            {
                Vector3 pos = transform.position;
                Vector3 scale = transform.lossyScale;
                minX = pos.x - scale.x * 0.5f;
                maxX = pos.x + scale.x * 0.5f;
                surfaceY = pos.y + scale.y * 0.5f;
            }

            // Position the sheen overlay on the floor surface
            float width = maxX - minX;
            float sheenHeight = 0.3f; // Thin glow strip on the surface
            float centerX = (minX + maxX) * 0.5f;

            sheenObj.transform.position = new Vector3(centerX, surfaceY + yOffset, -0.01f);
            sheenObj.transform.localScale = new Vector3(width, sheenHeight, 1f);

            // Evaluate sunlight exposure via raycasting
            float targetExposure = EvaluateSunlightExposure(minX, maxX, surfaceY + 0.1f);

            // Smooth transition
            float dt = Application.isPlaying ? UnityEngine.Time.deltaTime : 0.016f;
            currentExposure = Mathf.MoveTowards(currentExposure, targetExposure, transitionSpeed * dt);

            // Calculate final color with optional pulse
            float alpha = Mathf.Lerp(shadowColor.a, sheenColor.a, currentExposure);

            if (enablePulse && currentExposure > 0.01f)
            {
                float pulse = Mathf.Sin(UnityEngine.Time.time * pulseSpeed * Mathf.PI * 2f) * pulseIntensity;
                alpha = Mathf.Clamp01(alpha + pulse);
            }

            Color finalColor = Color.Lerp(shadowColor, sheenColor, currentExposure);
            finalColor.a = alpha;
            sheenRenderer.color = finalColor;
        }

        private float EvaluateSunlightExposure(float minX, float maxX, float surfaceY)
        {
            if (sunTransform == null) return 0f;

            Vector2 sunPos = sunTransform.position;
            int exposed = 0;
            int total = Mathf.Max(1, samplePoints);

            for (int i = 0; i < total; i++)
            {
                float t = total == 1 ? 0.5f : (float)i / (total - 1);
                float sampleX = Mathf.Lerp(minX + 0.1f, maxX - 0.1f, t);
                Vector2 samplePoint = new Vector2(sampleX, surfaceY);

                Vector2 toSample = samplePoint - sunPos;
                float distance = toSample.magnitude;
                Vector2 dir = toSample / distance;

                // Raycast from sun to this floor sample point
                RaycastHit2D[] hits = Physics2D.RaycastAll(sunPos, dir, distance, shadowBlockerMask);

                bool occluded = false;
                for (int h = 0; h < hits.Length; h++)
                {
                    var hitCol = hits[h].collider;
                    if (hitCol == null || hitCol.isTrigger) continue;
                    // Skip the floor surface itself
                    if (hitCol.gameObject == gameObject) continue;
                    if (hitCol.transform.IsChildOf(transform)) continue;
                    // Skip the sun
                    if (hitCol.transform.IsChildOf(sunTransform)) continue;
                    // Skip anything tagged Player
                    if (hitCol.CompareTag("Player")) continue;

                    occluded = true;
                    break;
                }

                if (!occluded) exposed++;
            }

            return (float)exposed / total;
        }

        private Sprite FindSquareSprite()
        {
            Sprite sprite = null;

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Square_32.png");
#endif

            if (sprite != null) return sprite;

            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void CleanupSheen()
        {
            if (sheenObj != null)
            {
                if (Application.isPlaying)
                    Destroy(sheenObj);
                else
                    DestroyImmediate(sheenObj);
                sheenObj = null;
                sheenRenderer = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Collider2D col = GetComponent<Collider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            float minX, maxX, surfaceY;

            if (col != null)
            {
                Bounds b = col.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                surfaceY = b.max.y;
            }
            else if (sr != null)
            {
                Bounds b = sr.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                surfaceY = b.max.y;
            }
            else
            {
                Vector3 pos = transform.position;
                Vector3 scale = transform.lossyScale;
                minX = pos.x - scale.x * 0.5f;
                maxX = pos.x + scale.x * 0.5f;
                surfaceY = pos.y + scale.y * 0.5f;
            }

            // Draw the sheen zone
            Gizmos.color = new Color(1f, 0.92f, 0.65f, 0.3f);
            float width = maxX - minX;
            Gizmos.DrawCube(new Vector3((minX + maxX) * 0.5f, surfaceY + yOffset, 0f), new Vector3(width, 0.3f, 0.01f));
        }
    }
}
