using System;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Projects a geometric shadow collider from a monolith pillar based on the celestial sun position.
    /// In Umbra Chronos, shadows are solid physical matter:
    /// - When the sun rotates ([Q] / [E] / mouse scroll), monoliths cast dynamic shadows.
    /// - This component calculates the geometric shadow boundary and dynamically updates
    ///   a PolygonCollider2D and/or EdgeCollider2D so players can walk across cast shadows as solid bridges.
    /// - Generates an ink-black visual silhouette mesh for the shadow volume matching the collider.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class DynamicShadowCollider : MonoBehaviour
    {
        [Header("Celestial Sun Reference")]
        [Tooltip("The celestial orbital sun transform. If null, automatically locates SunDialController or object tagged 'Sun'.")]
        [SerializeField] private Transform sunTransform;

        [Header("Ground & Projection Settings")]
        [Tooltip("Layer mask for ground/platforms that stop the shadow projection.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Tooltip("Fallback Y plane level if the shadow ray does not hit ground geometry (e.g. over a chasm).")]
        [SerializeField] private float fallbackGroundY = -2.5f;

        [Tooltip("Maximum length the shadow can stretch.")]
        [SerializeField] private float maxShadowDistance = 35f;

        [Tooltip("Whether to use raycasting against ground colliders before falling back to fallbackGroundY.")]
        [SerializeField] private bool useGroundRaycasting = true;

        [Header("Monolith Dimensions")]
        [Tooltip("Custom dimensions for this monolith. If (0,0), auto-detects from BoxCollider2D or SpriteRenderer.")]
        [SerializeField] private Vector2 customDimensions = Vector2.zero;

        [Tooltip("Offset relative to monolith center.")]
        [SerializeField] private Vector2 dimensionOffset = Vector2.zero;

        [Header("Collider & Visual Settings")]
        [Tooltip("Generate a solid PolygonCollider2D covering the full cast shadow volume.")]
        [SerializeField] private bool generatePolygonCollider = true;

        [Tooltip("Generate an EdgeCollider2D along the top walkable bridge surface.")]
        [SerializeField] private bool generateEdgeCollider = false;

        [Tooltip("Generate an ink-black visual mesh matching the shadow polygon in real time.")]
        [SerializeField] private bool renderShadowMesh = true;

        [Tooltip("Visual color for the solid shadow matter.")]
        [SerializeField] private Color shadowColor = new Color(0.05f, 0.05f, 0.08f, 0.95f); // #0D0D15 ink silhouette

        [Header("Physics Tuning")]
        [Tooltip("Minimum movement distance of sun before updating collider points (prevents micro-reallocations).")]
        [SerializeField] private float sunMovementThreshold = 0.005f;

        // Child components
        private GameObject shadowChildObject;
        private PolygonCollider2D polygonCollider;
        private EdgeCollider2D edgeCollider;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh shadowMesh;

        // Cached State
        private Vector2 lastSunPosition;
        private bool hasInitialized;
        private Material shadowMaterial;

        // Monolith Corners in local space
        private Vector2 localTopLeft;
        private Vector2 localTopRight;
        private Vector2 localBottomLeft;
        private Vector2 localBottomRight;

        public Transform SunTransform
        {
            get => sunTransform;
            set { sunTransform = value; ForceUpdateShadow(); }
        }

        public float FallbackGroundY
        {
            get => fallbackGroundY;
            set { fallbackGroundY = value; ForceUpdateShadow(); }
        }

        private void Awake()
        {
            InitializeComponents();
            CalculateMonolithBounds();
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += OnSunAngleChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= OnSunAngleChanged;
        }

        private void Start()
        {
            LocateSun();
            ForceUpdateShadow();
        }

        private void Update()
        {
            if (sunTransform == null)
            {
                LocateSun();
                if (sunTransform == null) return;
            }

            Vector2 currentSunPos = sunTransform.position;
            if (Vector2.SqrMagnitude(currentSunPos - lastSunPosition) > (sunMovementThreshold * sunMovementThreshold))
            {
                UpdateShadowGeometry(currentSunPos);
                lastSunPosition = currentSunPos;
            }
        }

        private void OnSunAngleChanged(float angle)
        {
            if (sunTransform != null)
            {
                UpdateShadowGeometry(sunTransform.position);
                lastSunPosition = sunTransform.position;
            }
        }

        private void InitializeComponents()
        {
            if (hasInitialized && shadowChildObject != null) return;

            // Find or create dedicated child object for shadow physical presence
            Transform existingChild = transform.Find("Shadow_Solid_Presence");
            if (existingChild != null)
            {
                shadowChildObject = existingChild.gameObject;
            }
            else
            {
                shadowChildObject = new GameObject("Shadow_Solid_Presence");
                shadowChildObject.transform.SetParent(transform, false);
                shadowChildObject.transform.localPosition = Vector3.zero;
                shadowChildObject.transform.localRotation = Quaternion.identity;
                shadowChildObject.transform.localScale = Vector3.one;
            }

            // Layer setup: inherit from parent or set to Default so player can stand on it
            shadowChildObject.layer = gameObject.layer;

            // Setup PolygonCollider2D
            if (generatePolygonCollider)
            {
                polygonCollider = shadowChildObject.GetComponent<PolygonCollider2D>();
                if (polygonCollider == null)
                {
                    polygonCollider = shadowChildObject.AddComponent<PolygonCollider2D>();
                }
            }

            // Setup EdgeCollider2D
            if (generateEdgeCollider)
            {
                edgeCollider = shadowChildObject.GetComponent<EdgeCollider2D>();
                if (edgeCollider == null)
                {
                    edgeCollider = shadowChildObject.AddComponent<EdgeCollider2D>();
                }
            }

            // Setup Visual Mesh
            if (renderShadowMesh)
            {
                meshFilter = shadowChildObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = shadowChildObject.AddComponent<MeshFilter>();
                }

                meshRenderer = shadowChildObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = shadowChildObject.AddComponent<MeshRenderer>();
                }

                if (shadowMesh == null)
                {
                    shadowMesh = new Mesh { name = "DynamicShadowMesh" };
                    shadowMesh.MarkDynamic();
                    meshFilter.sharedMesh = shadowMesh;
                }

                // Create clean unlit silhouette material
                Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader == null) unlitShader = Shader.Find("UI/Default");

                shadowMaterial = new Material(unlitShader)
                {
                    color = shadowColor
                };
                meshRenderer.sharedMaterial = shadowMaterial;
                meshRenderer.sortingOrder = -1; // Render just under or with silhouettes
            }

            hasInitialized = true;
        }

        private void CalculateMonolithBounds()
        {
            float width = customDimensions.x;
            float height = customDimensions.y;
            Vector2 offset = dimensionOffset;

            // If not provided in customDimensions, try to read from BoxCollider2D or SpriteRenderer
            if (width <= 0f || height <= 0f)
            {
                var box = GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    width = box.size.x;
                    height = box.size.y;
                    offset = box.offset;
                }
                else
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        width = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                        height = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                    }
                    else
                    {
                        // Default pillar proportions
                        width = 1.5f;
                        height = 6.0f;
                    }
                }
            }

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            localTopLeft = offset + new Vector2(-halfW, halfH);
            localTopRight = offset + new Vector2(halfW, halfH);
            localBottomLeft = offset + new Vector2(-halfW, -halfH);
            localBottomRight = offset + new Vector2(halfW, -halfH);
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;

            // 1. Try finding SunDialController
            var sunDial = UnityEngine.Object.FindAnyObjectByType<SunDialController>();
            if (sunDial != null)
            {
                sunTransform = sunDial.transform;
                return;
            }

            // 2. Try finding tagged "Sun"
            GameObject sunObj = GameObject.FindWithTag("Sun");
            if (sunObj != null)
            {
                sunTransform = sunObj.transform;
            }
        }

        public void ForceUpdateShadow()
        {
            if (sunTransform == null) LocateSun();
            if (sunTransform != null)
            {
                UpdateShadowGeometry(sunTransform.position);
                lastSunPosition = sunTransform.position;
            }
        }

        private void UpdateShadowGeometry(Vector2 sunPos)
        {
            if (!hasInitialized) InitializeComponents();

            // Transform local corners to world space
            Vector2 worldTopLeft = transform.TransformPoint(localTopLeft);
            Vector2 worldTopRight = transform.TransformPoint(localTopRight);
            Vector2 worldBottomLeft = transform.TransformPoint(localBottomLeft);
            Vector2 worldBottomRight = transform.TransformPoint(localBottomRight);

            // Center of the monolith top
            float monolithCenterX = (worldTopLeft.x + worldTopRight.x) * 0.5f;

            // Project rays from Sun through the monolith top corners
            Vector2 dirLeft = (worldTopLeft - sunPos).normalized;
            Vector2 dirRight = (worldTopRight - sunPos).normalized;

            // Find where these rays hit the ground/chasm plane
            Vector2 groundHitLeft = ProjectRayToGround(worldTopLeft, dirLeft);
            Vector2 groundHitRight = ProjectRayToGround(worldTopRight, dirRight);

            // Construct the solid shadow boundary polygon depending on sun angle
            // When sun is to the LEFT (sunPos.x < monolithCenterX):
            // The shadow is cast towards the RIGHT.
            // Walkable top edge goes from worldTopRight down to groundHitRight.
            //
            // When sun is to the RIGHT (sunPos.x > monolithCenterX):
            // The shadow is cast towards the LEFT.
            // Walkable top edge goes from worldTopLeft down to groundHitLeft.

            Vector2[] worldPolygonPoints;
            Vector2[] worldWalkableEdgePoints;

            if (sunPos.x <= monolithCenterX)
            {
                // Shadow cast to RIGHT
                worldPolygonPoints = new Vector2[]
                {
                    worldBottomLeft,
                    worldTopLeft,
                    worldTopRight,
                    groundHitRight,
                    groundHitLeft.y < worldBottomRight.y ? groundHitLeft : worldBottomRight
                };

                worldWalkableEdgePoints = new Vector2[]
                {
                    worldTopLeft,
                    worldTopRight,
                    groundHitRight
                };
            }
            else
            {
                // Shadow cast to LEFT
                worldPolygonPoints = new Vector2[]
                {
                    worldBottomRight,
                    worldTopRight,
                    worldTopLeft,
                    groundHitLeft,
                    groundHitRight.y < worldBottomLeft.y ? groundHitRight : worldBottomLeft
                };

                worldWalkableEdgePoints = new Vector2[]
                {
                    worldTopRight,
                    worldTopLeft,
                    groundHitLeft
                };
            }

            // Convert points to local space of child collider
            Vector2[] localPolygonPoints = new Vector2[worldPolygonPoints.Length];
            for (int i = 0; i < worldPolygonPoints.Length; i++)
            {
                localPolygonPoints[i] = shadowChildObject.transform.InverseTransformPoint(worldPolygonPoints[i]);
            }

            // Update PolygonCollider2D
            if (polygonCollider != null && generatePolygonCollider)
            {
                polygonCollider.pathCount = 1;
                polygonCollider.SetPath(0, localPolygonPoints);
            }

            // Update EdgeCollider2D
            if (edgeCollider != null && generateEdgeCollider)
            {
                Vector2[] localEdgePoints = new Vector2[worldWalkableEdgePoints.Length];
                for (int i = 0; i < worldWalkableEdgePoints.Length; i++)
                {
                    localEdgePoints[i] = shadowChildObject.transform.InverseTransformPoint(worldWalkableEdgePoints[i]);
                }
                edgeCollider.points = localEdgePoints;
            }

            // Rebuild visual shadow mesh
            if (renderShadowMesh && shadowMesh != null)
            {
                UpdateMesh(localPolygonPoints);
            }
        }

        private Vector2 ProjectRayToGround(Vector2 corner, Vector2 dir)
        {
            // Safety check against nearly horizontal or upwards rays
            if (dir.y >= -0.001f)
            {
                dir.y = -0.001f;
                dir.Normalize();
            }

            // 1. Raycast against ground geometry if enabled
            if (useGroundRaycasting)
            {
                // Cast ray slightly offset from corner to avoid self-collision
                Vector2 rayOrigin = corner + dir * 0.05f;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, maxShadowDistance, groundLayerMask);

                if (hit.collider != null && hit.collider.gameObject != gameObject && hit.collider.gameObject != shadowChildObject)
                {
                    // Do not hit player or triggers
                    if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                    {
                        return hit.point;
                    }
                }
            }

            // 2. Intersect with fallbackGroundY plane
            float targetY = fallbackGroundY;
            if (corner.y > targetY)
            {
                float t = (targetY - corner.y) / dir.y;
                if (t > 0f)
                {
                    float clampedDist = Mathf.Min(t, maxShadowDistance);
                    return corner + dir * clampedDist;
                }
            }

            return corner + dir * maxShadowDistance;
        }

        private void UpdateMesh(Vector2[] points)
        {
            if (points == null || points.Length < 3) return;

            shadowMesh.Clear();

            Vector3[] vertices = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                vertices[i] = new Vector3(points[i].x, points[i].y, 0f);
            }

            // Convex polygon triangulation (triangle fan from vertex 0)
            int triangleCount = (points.Length - 2) * 3;
            int[] triangles = new int[triangleCount];
            int triIndex = 0;

            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles[triIndex++] = 0;
                triangles[triIndex++] = i;
                triangles[triIndex++] = i + 1;
            }

            shadowMesh.vertices = vertices;
            shadowMesh.triangles = triangles;
            shadowMesh.RecalculateBounds();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Vector3 center = transform.position + (Vector3)dimensionOffset;
            Vector2 size = customDimensions != Vector2.zero ? customDimensions : new Vector2(1.5f, 6f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.1f));

            if (sunTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(sunTransform.position, transform.position);
            }

            // Draw fallback ground plane
            Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.5f);
            Vector3 groundStart = new Vector3(transform.position.x - 20f, fallbackGroundY, 0f);
            Vector3 groundEnd = new Vector3(transform.position.x + 20f, fallbackGroundY, 0f);
            Gizmos.DrawLine(groundStart, groundEnd);
        }
#endif
    }
}
