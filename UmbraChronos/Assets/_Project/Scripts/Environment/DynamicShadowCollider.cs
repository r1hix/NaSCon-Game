using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Projects a solid, horizontal EdgeCollider2D shadow bridge and visible graphics
    /// when the celestial sun shines upon an ancient monolith.
    /// 
    /// In Umbra Chronos ("Ink & Solar Flare"):
    /// - Shadows are solid physical matter: players can walk across cast shadows to traverse lethal chasms.
    /// - Uses the celestial sun angle theta (degrees) directly, ensuring shadows remain completely stable
    ///   when the camera shifts between screens.
    /// - Generates a flat, horizontal walkable shadow bridge at ground level (Y = fallbackGroundY)
    ///   spanning across the chasm from the monolith base to the shadow tip.
    /// - Renders a luminous radiant cyan rim (#38BDF8) along the flat walkable bridge surface.
    /// - Renders a solid obsidian matter bridge slab (#0D0D15) under the bridge.
    /// - Renders a moody atmospheric umbral silhouette cone in the air between monolith top and bridge tip.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(EdgeCollider2D))]
    [DisallowMultipleComponent]
    public class DynamicShadowCollider : MonoBehaviour
    {
        [Header("Celestial Sun & Angle")]
        [Tooltip("Direct celestial sun angle in degrees (90 = overhead noon, <90 = dawn/morning casting right, >90 = dusk casting left).")]
        [SerializeField] private float currentSunAngle = 50f;

        [Tooltip("Transform of the celestial sun source. If null, locates SunDialController in scene.")]
        [SerializeField] private Transform sunTransform;

        [Header("Ground & Bridge Geometry")]
        [Tooltip("Ground Y level where the solid shadow bridge forms (matches player walking plane).")]
        [SerializeField] private float fallbackGroundY = -1.75f;

        [Tooltip("Layer mask for ground/walls that stop the shadow projection.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Tooltip("Maximum length the shadow bridge can extend across chasms.")]
        [SerializeField] private float maxShadowDistance = 25f;

        [Tooltip("Multiplier applied to the natural shadow length.")]
        [SerializeField] private float shadowLengthMultiplier = 1.0f;

        [Header("Solid Shadow Bridge Matter ('Ink')")]
        [Tooltip("Thickness of the solid obsidian bridge matter below the walking surface.")]
        [SerializeField] private float bridgeThickness = 0.45f;

        [Tooltip("Color of the solid obsidian bridge matter.")]
        [SerializeField] private Color bridgeColor = new Color(0.05f, 0.05f, 0.08f, 1.0f); // #0D0D15 Solid Obsidian

        [Header("Walkable Cyan Rim ('Solar Flare')")]
        [Tooltip("Render luminous cyan rim along the walkable top surface of the horizontal bridge.")]
        [SerializeField] private bool renderWalkableRim = true;

        [SerializeField] private Color rimColor = new Color(0.22f, 0.74f, 0.97f, 1.0f); // #38BDF8 Radiant Cyan

        [SerializeField] private float rimWidth = 0.12f;

        [Header("Atmospheric Umbral Silhouette")]
        [Tooltip("Render subtle atmospheric shadow cone in the air between monolith top and ground tip.")]
        [SerializeField] private bool renderUmbraSilhouette = true;

        [Tooltip("Alpha opacity of the atmospheric shadow cone in the sky (0.2 to 0.4 allows sky gradient to shine through).")]
        [Range(0f, 1f)]
        [SerializeField] private float umbraSilhouetteAlpha = 0.30f;

        [SerializeField] private Color umbraColor = new Color(0.05f, 0.05f, 0.08f, 0.30f);

        [Header("Razor-Sharp Shadow Boundary Cutoff")]
        [Tooltip("Render razor-sharp vertical boundary line at the exact tip of the solid shadow bridge.")]
        [SerializeField] private bool renderShadowBoundary = true;

        [SerializeField] private Color boundaryColor = new Color(0.96f, 0.62f, 0.04f, 0.95f); // #F59E0B Warning Solar Amber

        [SerializeField] private float boundaryWidth = 0.09f;

        [SerializeField] private float boundaryHeight = 1.35f;

        [Header("Sunlit Solar Heat Fringe")]
        [Tooltip("Render subtle gradient heat fringe extending from the shadow tip into the lethal chasm.")]
        [SerializeField] private bool renderSunlitGlowStrip = true;

        [SerializeField] private Color sunlitGlowColor = new Color(1.0f, 0.92f, 0.65f, 0.45f); // #FFEAA7 Searing Solar Glare

        [SerializeField] private float sunlitGlowWidth = 0.90f;

        // Components
        private EdgeCollider2D edgeCollider;
        private BoxCollider2D boxCollider;
        private LineRenderer rimRenderer;

        // Bridge Slab Visual (Child GameObject)
        private MeshFilter bridgeMeshFilter;
        private MeshRenderer bridgeMeshRenderer;
        private Mesh bridgeMesh;

        // Umbra Silhouette Visual (Child GameObject)
        private MeshFilter umbraMeshFilter;
        private MeshRenderer umbraMeshRenderer;
        private Mesh umbraMesh;

        // Shadow Boundary Marker Visual (Child GameObject)
        private LineRenderer boundaryLineRenderer;

        // Sunlit Glow Fringe Visual (Child GameObject)
        private MeshFilter glowStripMeshFilter;
        private MeshRenderer glowStripMeshRenderer;
        private Mesh glowStripMesh;

        // Public Accessors
        public float CurrentSunAngle => currentSunAngle;
        public float FallbackGroundY => fallbackGroundY;

        private void Awake()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            boxCollider = GetComponent<BoxCollider2D>();

            SetupVisuals();
            SetupShadowCaster();
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            SetupVisuals();
            UpdateShadowBridge();
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
        }

        private void Start()
        {
            LocateSun();
            UpdateShadowBridge();
        }

        private void Update()
        {
            if (sunTransform == null)
            {
                LocateSun();
            }

            // In Edit Mode, keep shadow bridge synchronized with inspector or SunDial
            if (!Application.isPlaying)
            {
                UpdateShadowBridge();
            }
        }

        private void HandleSunAngleChanged(float angle)
        {
            currentSunAngle = angle;
            UpdateShadowBridge();
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;

            var sunDial = UnityEngine.Object.FindAnyObjectByType<SunDialController>();
            if (sunDial != null)
            {
                sunTransform = sunDial.transform;
                currentSunAngle = sunDial.CurrentAngle;
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

            var anyLight = UnityEngine.Object.FindAnyObjectByType<Light2D>();
            if (anyLight != null)
            {
                sunTransform = anyLight.transform;
            }
        }

        /// <summary>
        /// Calculates the horizontal solid shadow bridge and atmospheric silhouette
        /// strictly based on the celestial sun angle.
        /// </summary>
        public void UpdateShadowBridge()
        {
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (edgeCollider == null) return;

            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

            // Query sun angle from SunDialController if available
            if (sunTransform != null)
            {
                var sunDial = sunTransform.GetComponent<SunDialController>();
                if (sunDial != null)
                {
                    currentSunAngle = sunDial.CurrentAngle;
                }
            }

            // Monolith world bounds
            float xMin, xMax, yTop, yBase;
            if (boxCollider != null)
            {
                Bounds b = boxCollider.bounds;
                xMin = b.min.x;
                xMax = b.max.x;
                yTop = b.max.y;
                yBase = b.min.y;
            }
            else
            {
                Vector2 half = transform.lossyScale * 0.5f;
                Vector2 center = transform.position;
                xMin = center.x - half.x;
                xMax = center.x + half.x;
                yTop = center.y + half.y;
                yBase = center.y - half.y;
            }

            float groundY = fallbackGroundY;
            float pillarHeight = Mathf.Max(0.5f, yTop - groundY);

            // Determine sun position and direction relative to this monolith
            float xCenter = (xMin + xMax) * 0.5f;
            Vector2 sunPos = sunTransform != null ? (Vector2)sunTransform.position : Vector2.zero;

            bool sunIsOnLeft;
            float shadowLength;

            if (sunTransform != null)
            {
                sunIsOnLeft = sunPos.x < xCenter;
                Vector2 corner = sunIsOnLeft ? new Vector2(xMax, yTop) : new Vector2(xMin, yTop);
                Vector2 rayDir = (corner - sunPos).normalized;
                if (rayDir.y >= -0.01f) rayDir.y = -0.01f;

                float t = (groundY - corner.y) / rayDir.y;
                float rawTargetX = corner.x + (rayDir.x * t);
                float rawLen = Mathf.Abs(rawTargetX - corner.x) * shadowLengthMultiplier;
                shadowLength = Mathf.Clamp(rawLen, 0f, maxShadowDistance);
            }
            else
            {
                // Fallback to celestial angle math if sun transform is unavailable
                float angleClamped = Mathf.Clamp(currentSunAngle, 15f, 165f);
                float rad = angleClamped * Mathf.Deg2Rad;
                float cot = Mathf.Cos(rad) / Mathf.Sin(rad);

                sunIsOnLeft = angleClamped < 89.9f;
                float rawLen = pillarHeight * Mathf.Abs(cot) * shadowLengthMultiplier;
                shadowLength = Mathf.Min(rawLen, maxShadowDistance);
            }

            bool isRetracted = shadowLength < 0.25f || (sunTransform != null && Mathf.Abs(sunPos.x - xCenter) < 0.25f && sunPos.y > yTop);

            float xStart = 0f;
            float xEnd = 0f;
            float targetX = 0f;
            Vector3 umbraCorner = Vector3.zero;

            if (!isRetracted)
            {
                if (sunIsOnLeft)
                {
                    // Sun is to the LEFT of this pillar -> Shadow projects to the RIGHT (+X)
                    float xBase = xMax;
                    targetX = xBase + shadowLength;

                    // Raycast along walking plane to check for blocking walls
                    RaycastHit2D hit = Physics2D.Raycast(new Vector2(xBase + 0.05f, groundY + 0.1f), Vector2.right, shadowLength, groundLayerMask);
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                    {
                        targetX = Mathf.Min(targetX, hit.point.x);
                    }

                    xStart = xBase;
                    xEnd = targetX;
                    umbraCorner = new Vector3(xMax, yTop, 0f);
                }
                else
                {
                    // Sun is to the RIGHT of this pillar -> Shadow projects to the LEFT (-X)
                    float xBase = xMin;
                    targetX = xBase - shadowLength;

                    // Raycast along walking plane to check for blocking walls
                    RaycastHit2D hit = Physics2D.Raycast(new Vector2(xBase - 0.05f, groundY + 0.1f), Vector2.left, shadowLength, groundLayerMask);
                    if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                    {
                        targetX = Mathf.Max(targetX, hit.point.x);
                    }

                    xStart = targetX;
                    xEnd = xBase;
                    umbraCorner = new Vector3(xMin, yTop, 0f);
                }
            }

            // 1. UPDATE PHYSICAL EDGE COLLIDER (Horizontal walking bridge at ground level)
            if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
            {
                edgeCollider.enabled = false;
            }
            else
            {
                edgeCollider.enabled = true;
                Vector2 worldP1 = new Vector2(xStart, groundY);
                Vector2 worldP2 = new Vector2(xEnd, groundY);
                edgeCollider.points = new Vector2[]
                {
                    transform.InverseTransformPoint(worldP1),
                    transform.InverseTransformPoint(worldP2)
                };
            }

            // 2. UPDATE LUMINOUS CYAN RIM (Flat line across the top of the horizontal bridge)
            if (renderWalkableRim)
            {
                if (rimRenderer == null) SetupVisuals();
                if (rimRenderer != null)
                {
                    if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
                    {
                        rimRenderer.enabled = false;
                    }
                    else
                    {
                        rimRenderer.enabled = true;
                        rimRenderer.positionCount = 2;
                        rimRenderer.SetPosition(0, new Vector3(xStart, groundY, -0.05f));
                        rimRenderer.SetPosition(1, new Vector3(xEnd, groundY, -0.05f));
                    }
                }
            }

            // 3. UPDATE SOLID OBSIDIAN BRIDGE SLAB MESH
            if (bridgeMesh == null || bridgeMeshFilter == null) SetupVisuals();
            if (bridgeMesh != null && bridgeMeshFilter != null)
            {
                if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
                {
                    bridgeMesh.Clear();
                }
                else
                {
                    UpdateBridgeSlabMesh(xStart, xEnd, groundY, bridgeThickness);
                }
            }

            // 4. UPDATE UMBRAL ATMOSPHERIC SHADOW SILHOUETTE MESH
            if (renderUmbraSilhouette)
            {
                if (umbraMesh == null || umbraMeshFilter == null) SetupVisuals();
                if (umbraMesh != null && umbraMeshFilter != null)
                {
                    if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
                    {
                        umbraMesh.Clear();
                    }
                    else
                    {
                        UpdateUmbraSilhouetteMesh(umbraCorner, targetX, groundY);
                    }
                }
            }

            // 5. UPDATE RAZOR-SHARP SHADOW BOUNDARY CUTOFF MARKER
            if (renderShadowBoundary)
            {
                if (boundaryLineRenderer == null) SetupVisuals();
                if (boundaryLineRenderer != null)
                {
                    if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
                    {
                        boundaryLineRenderer.enabled = false;
                    }
                    else
                    {
                        boundaryLineRenderer.enabled = true;
                        float tipX = sunIsOnLeft ? xEnd : xStart;
                        boundaryLineRenderer.positionCount = 2;
                        boundaryLineRenderer.SetPosition(0, new Vector3(tipX, groundY - (boundaryHeight * 0.15f), -0.06f));
                        boundaryLineRenderer.SetPosition(1, new Vector3(tipX, groundY + (boundaryHeight * 0.85f), -0.06f));

                        // Pulsing solar amber warning glow
                        float pulse = 0.82f + (Mathf.Sin(UnityEngine.Time.time * 4.0f) * 0.18f);
                        Color pulsedColor = boundaryColor;
                        pulsedColor.a = boundaryColor.a * pulse;
                        boundaryLineRenderer.startColor = pulsedColor;
                        boundaryLineRenderer.endColor = new Color(pulsedColor.r, pulsedColor.g, pulsedColor.b, pulsedColor.a * 0.25f);
                    }
                }
            }

            // 6. UPDATE SUNLIT SOLAR HEAT FRINGE (Gradient out into the lethal void)
            if (renderSunlitGlowStrip)
            {
                if (glowStripMesh == null || glowStripMeshFilter == null) SetupVisuals();
                if (glowStripMesh != null && glowStripMeshFilter != null)
                {
                    if (isRetracted || Mathf.Abs(xEnd - xStart) < 0.25f)
                    {
                        glowStripMesh.Clear();
                    }
                    else
                    {
                        float tipX = sunIsOnLeft ? xEnd : xStart;
                        float glowEndX = sunIsOnLeft ? (tipX + sunlitGlowWidth) : (tipX - sunlitGlowWidth);
                        UpdateSunlitGlowStripMesh(tipX, glowEndX, groundY);
                    }
                }
            }
        }

        private void UpdateBridgeSlabMesh(float xStart, float xEnd, float groundY, float thickness)
        {
            Vector3[] worldVerts = new Vector3[]
            {
                new Vector3(xStart, groundY, 0f),
                new Vector3(xEnd, groundY, 0f),
                new Vector3(xEnd, groundY - thickness, 0f),
                new Vector3(xStart, groundY - thickness, 0f)
            };

            Vector3[] localVerts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                localVerts[i] = bridgeMeshFilter.transform.InverseTransformPoint(worldVerts[i]);
            }

            bridgeMesh.Clear();
            bridgeMesh.vertices = localVerts;
            // Double-sided quad triangles
            bridgeMesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 1,
                0, 2, 3,
                0, 3, 2
            };
            bridgeMesh.RecalculateBounds();
        }

        private void UpdateUmbraSilhouetteMesh(Vector3 corner, float targetX, float groundY)
        {
            Vector3[] worldVerts = new Vector3[]
            {
                corner,
                new Vector3(targetX, groundY, 0f),
                new Vector3(corner.x, groundY, 0f)
            };

            Vector3[] localVerts = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                localVerts[i] = umbraMeshFilter.transform.InverseTransformPoint(worldVerts[i]);
            }

            umbraMesh.Clear();
            umbraMesh.vertices = localVerts;
            // Double-sided triangle
            umbraMesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 1
            };
            umbraMesh.RecalculateBounds();
        }

        private void UpdateSunlitGlowStripMesh(float xNear, float xFar, float groundY)
        {
            float stripHeight = boundaryHeight;
            float yBottom = groundY - (stripHeight * 0.15f);
            float yTop = groundY + (stripHeight * 0.40f);

            Vector3[] worldVerts = new Vector3[]
            {
                new Vector3(xNear, yBottom, 0.02f), // Bottom near (at shadow tip)
                new Vector3(xFar, yBottom, 0.02f),  // Bottom far (into chasm)
                new Vector3(xFar, yTop, 0.02f),     // Top far (into chasm)
                new Vector3(xNear, yTop, 0.02f),    // Top near (at shadow tip)
            };

            Vector3[] localVerts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                localVerts[i] = glowStripMeshFilter.transform.InverseTransformPoint(worldVerts[i]);
            }

            Color opaqueColor = sunlitGlowColor;
            Color transparentColor = new Color(sunlitGlowColor.r, sunlitGlowColor.g, sunlitGlowColor.b, 0f);

            // Vertices 0 & 3 are at xNear (the shadow boundary); Vertices 1 & 2 are at xFar (fading out into the void)
            Color[] colors = new Color[]
            {
                opaqueColor,
                transparentColor,
                transparentColor,
                opaqueColor
            };

            glowStripMesh.Clear();
            glowStripMesh.vertices = localVerts;
            glowStripMesh.colors = colors;
            // Double-sided quad
            glowStripMesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 1,
                0, 2, 3,
                0, 3, 2
            };
            glowStripMesh.RecalculateBounds();
        }

        private void SetupVisuals()
        {
            Shader uShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (uShader == null) uShader = Shader.Find("Sprites/Default");

            // 1. Walkable Cyan Rim LineRenderer
            if (renderWalkableRim)
            {
                rimRenderer = GetComponent<LineRenderer>();
                if (rimRenderer == null) rimRenderer = gameObject.AddComponent<LineRenderer>();

                if (uShader != null)
                {
                    rimRenderer.material = new Material(uShader) { color = rimColor };
                }
                rimRenderer.startWidth = rimWidth;
                rimRenderer.endWidth = rimWidth;
                rimRenderer.useWorldSpace = true;
                rimRenderer.sortingOrder = 5; // Crisp top rim above bridge
                rimRenderer.alignment = LineAlignment.View;
            }

            // 2. Solid Obsidian Bridge Slab (Child GameObject)
            Transform slabTransform = transform.Find("Shadow_Bridge_Slab");
            GameObject slabObj = slabTransform != null ? slabTransform.gameObject : new GameObject("Shadow_Bridge_Slab");
            slabObj.transform.SetParent(transform, false);
            slabObj.transform.localPosition = Vector3.zero;
            slabObj.transform.localRotation = Quaternion.identity;
            slabObj.transform.localScale = Vector3.one;

            bridgeMeshFilter = slabObj.GetComponent<MeshFilter>();
            if (bridgeMeshFilter == null) bridgeMeshFilter = slabObj.AddComponent<MeshFilter>();

            bridgeMeshRenderer = slabObj.GetComponent<MeshRenderer>();
            if (bridgeMeshRenderer == null) bridgeMeshRenderer = slabObj.AddComponent<MeshRenderer>();

            if (bridgeMesh == null)
            {
                bridgeMesh = new Mesh { name = "BridgeSlabMesh" };
                bridgeMesh.MarkDynamic();
                bridgeMeshFilter.sharedMesh = bridgeMesh;
            }

            if (uShader != null)
            {
                bridgeMeshRenderer.sharedMaterial = new Material(uShader) { color = bridgeColor };
            }
            bridgeMeshRenderer.sortingOrder = 1; // Solid matter in line with platforms

            // Clean up legacy fill child if present
            Transform legacyFill = transform.Find("Shadow_Solid_Fill");
            if (legacyFill != null)
            {
                if (Application.isPlaying) Destroy(legacyFill.gameObject);
                else DestroyImmediate(legacyFill.gameObject);
            }

            // 3. Umbral Atmospheric Shadow Silhouette (Child GameObject)
            if (renderUmbraSilhouette)
            {
                Transform umbraTransform = transform.Find("Shadow_Umbra_Cone");
                GameObject umbraObj = umbraTransform != null ? umbraTransform.gameObject : new GameObject("Shadow_Umbra_Cone");
                umbraObj.transform.SetParent(transform, false);
                umbraObj.transform.localPosition = Vector3.zero;
                umbraObj.transform.localRotation = Quaternion.identity;
                umbraObj.transform.localScale = Vector3.one;

                umbraMeshFilter = umbraObj.GetComponent<MeshFilter>();
                if (umbraMeshFilter == null) umbraMeshFilter = umbraObj.AddComponent<MeshFilter>();

                umbraMeshRenderer = umbraObj.GetComponent<MeshRenderer>();
                if (umbraMeshRenderer == null) umbraMeshRenderer = umbraObj.AddComponent<MeshRenderer>();

                if (umbraMesh == null)
                {
                    umbraMesh = new Mesh { name = "UmbraConeMesh" };
                    umbraMesh.MarkDynamic();
                    umbraMeshFilter.sharedMesh = umbraMesh;
                }

                if (uShader != null)
                {
                    Color tintedUmbra = new Color(umbraColor.r, umbraColor.g, umbraColor.b, umbraSilhouetteAlpha);
                    umbraMeshRenderer.sharedMaterial = new Material(uShader) { color = tintedUmbra };
                }
                umbraMeshRenderer.sortingOrder = -2; // Behind platforms and player, in front of sky canvas
            }

            // 4. Razor-Sharp Shadow Boundary Cutoff Marker (Child GameObject)
            if (renderShadowBoundary)
            {
                Transform boundaryTransform = transform.Find("Shadow_Boundary_Marker");
                GameObject boundaryObj = boundaryTransform != null ? boundaryTransform.gameObject : new GameObject("Shadow_Boundary_Marker");
                boundaryObj.transform.SetParent(transform, false);
                boundaryObj.transform.localPosition = Vector3.zero;
                boundaryObj.transform.localRotation = Quaternion.identity;
                boundaryObj.transform.localScale = Vector3.one;

                boundaryLineRenderer = boundaryObj.GetComponent<LineRenderer>();
                if (boundaryLineRenderer == null) boundaryLineRenderer = boundaryObj.AddComponent<LineRenderer>();

                if (uShader != null)
                {
                    boundaryLineRenderer.material = new Material(uShader) { color = boundaryColor };
                }
                boundaryLineRenderer.startWidth = boundaryWidth;
                boundaryLineRenderer.endWidth = boundaryWidth * 0.4f;
                boundaryLineRenderer.useWorldSpace = true;
                boundaryLineRenderer.sortingOrder = 6; // Above the cyan rim
                boundaryLineRenderer.alignment = LineAlignment.View;
            }

            // 5. Sunlit Solar Heat Fringe (Child GameObject)
            if (renderSunlitGlowStrip)
            {
                Transform glowTransform = transform.Find("Shadow_Sunlit_Fringe");
                GameObject glowObj = glowTransform != null ? glowTransform.gameObject : new GameObject("Shadow_Sunlit_Fringe");
                glowObj.transform.SetParent(transform, false);
                glowObj.transform.localPosition = Vector3.zero;
                glowObj.transform.localRotation = Quaternion.identity;
                glowObj.transform.localScale = Vector3.one;

                glowStripMeshFilter = glowObj.GetComponent<MeshFilter>();
                if (glowStripMeshFilter == null) glowStripMeshFilter = glowObj.AddComponent<MeshFilter>();

                glowStripMeshRenderer = glowObj.GetComponent<MeshRenderer>();
                if (glowStripMeshRenderer == null) glowStripMeshRenderer = glowObj.AddComponent<MeshRenderer>();

                if (glowStripMesh == null)
                {
                    glowStripMesh = new Mesh { name = "SunlitFringeMesh" };
                    glowStripMesh.MarkDynamic();
                    glowStripMeshFilter.sharedMesh = glowStripMesh;
                }

                if (uShader != null)
                {
                    glowStripMeshRenderer.sharedMaterial = new Material(uShader) { color = Color.white }; // Vertex colors define gradient
                }
                glowStripMeshRenderer.sortingOrder = 0; // At surface level
            }
        }

        private void SetupShadowCaster()
        {
            var caster = GetComponent<ShadowCaster2D>();
            if (caster == null)
            {
                caster = gameObject.AddComponent<ShadowCaster2D>();
            }

            caster.castsShadows = true;
        }
    }
}
