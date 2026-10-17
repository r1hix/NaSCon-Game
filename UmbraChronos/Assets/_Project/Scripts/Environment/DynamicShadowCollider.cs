using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Projects an EdgeCollider2D and visible bridge graphics along the top surface of a monolith's cast shadow.
    /// In Umbra Chronos, shadows are solid physical matter:
    /// - When the celestial sun rotates ([Q] / [E] / scroll), monoliths cast dynamic shadows.
    /// - This component renders a visible shadow fill and a luminous cyan walkable rim (#38BDF8),
    ///   and updates an EdgeCollider2D so the top edge acts as a solid bridge the player can cross.
    /// - Uses thin EdgeCollider2D geometry (NOT a solid block) so it never traps the player or bulldozes objects.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    [DisallowMultipleComponent]
    public class DynamicShadowCollider : MonoBehaviour
    {
        [Header("Celestial Sun Reference")]
        [Tooltip("The celestial orbital sun transform. If null, automatically locates SunDialController or object tagged 'Sun'.")]
        [SerializeField] private Transform sunTransform;

        [Header("Ground & Projection Settings")]
        [Tooltip("Layer mask for ground/platforms that stop the shadow projection.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Tooltip("Fallback ground Y level if raycast does not hit geometry (e.g. over a chasm).")]
        [SerializeField] private float fallbackGroundY = -1.75f;

        [Tooltip("Maximum length the shadow bridge can extend.")]
        [SerializeField] private float maxShadowDistance = 14f;

        [Header("Visual Styling ('Ink & Solar Flare')")]
        [Tooltip("Render a luminous cyan rim along the walkable top edge of the shadow.")]
        [SerializeField] private bool renderWalkableRim = true;

        [SerializeField] private Color rimColor = new Color(0.22f, 0.74f, 0.97f, 0.85f); // Radiant Cyan #38BDF8

        [SerializeField] private float rimWidth = 0.08f;

        [Tooltip("Render a translucent shadow fill under the bridge.")]
        [SerializeField] private bool renderShadowFill = true;

        [SerializeField] private Color shadowFillColor = new Color(0.05f, 0.05f, 0.08f, 0.65f); // Translucent ink

        // Components
        private EdgeCollider2D edgeCollider;
        private BoxCollider2D boxCollider;
        private LineRenderer rimRenderer;
        private MeshFilter fillMeshFilter;
        private MeshRenderer fillMeshRenderer;
        private Mesh fillMesh;
        private Vector2 lastSunPosition;

        private void Awake()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            boxCollider = GetComponent<BoxCollider2D>();

            SetupVisuals();
            SetupShadowCaster();
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
            UpdateShadowBridge();
        }

        private void Update()
        {
            if (sunTransform == null)
            {
                LocateSun();
                if (sunTransform == null) return;
            }

            Vector2 currentSunPos = sunTransform.position;
            if (Vector2.SqrMagnitude(currentSunPos - lastSunPosition) > 0.005f)
            {
                UpdateShadowBridge();
                lastSunPosition = currentSunPos;
            }
        }

        private void OnSunAngleChanged(float angle)
        {
            UpdateShadowBridge();
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;

            var sunDial = UnityEngine.Object.FindAnyObjectByType<SunDialController>();
            if (sunDial != null)
            {
                sunTransform = sunDial.transform;
                return;
            }

            GameObject sunObj = GameObject.FindWithTag("Sun");
            if (sunObj != null)
            {
                sunTransform = sunObj.transform;
            }
        }

        public void UpdateShadowBridge()
        {
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (edgeCollider == null) return;

            if (sunTransform == null) LocateSun();
            if (sunTransform == null) return;

            Vector2 sunPos = sunTransform.position;

            // Compute world corners of the monolith pillar
            Vector2 worldTopLeft, worldTopRight, worldBottomLeft, worldBottomRight;
            if (boxCollider != null)
            {
                Bounds b = boxCollider.bounds;
                worldTopLeft = new Vector2(b.min.x, b.max.y);
                worldTopRight = new Vector2(b.max.x, b.max.y);
                worldBottomLeft = new Vector2(b.min.x, b.min.y);
                worldBottomRight = new Vector2(b.max.x, b.min.y);
            }
            else
            {
                Vector2 half = transform.lossyScale * 0.5f;
                Vector2 center = transform.position;
                worldTopLeft = new Vector2(center.x - half.x, center.y + half.y);
                worldTopRight = new Vector2(center.x + half.x, center.y + half.y);
                worldBottomLeft = new Vector2(center.x - half.x, center.y - half.y);
                worldBottomRight = new Vector2(center.x + half.x, center.y - half.y);
            }

            float monolithCenterX = (worldTopLeft.x + worldTopRight.x) * 0.5f;
            Vector2[] walkableBridgeWorld;
            Vector3[] fillVertices;

            if (sunPos.x <= monolithCenterX)
            {
                // Sun on LEFT -> Light projects shadow to the RIGHT (+X)
                Vector2 dirRight = (worldTopRight - sunPos).normalized;
                Vector2 groundHit = ProjectRayToGround(worldTopRight, dirRight);

                // Walkable top surface: from TopLeft across TopRight down to ground landing
                walkableBridgeWorld = new Vector2[]
                {
                    worldTopLeft,
                    worldTopRight,
                    groundHit
                };

                // Triangular / quad shadow fill under the bridge (rendered behind platforms at order -10)
                fillVertices = new Vector3[]
                {
                    new Vector3(worldTopLeft.x, worldTopLeft.y, 0f),
                    new Vector3(worldTopRight.x, worldTopRight.y, 0f),
                    new Vector3(groundHit.x, groundHit.y, 0f),
                    new Vector3(worldBottomRight.x, worldBottomRight.y, 0f)
                };
            }
            else
            {
                // Sun on RIGHT -> Light projects shadow to the LEFT (-X)
                Vector2 dirLeft = (worldTopLeft - sunPos).normalized;
                Vector2 groundHit = ProjectRayToGround(worldTopLeft, dirLeft);

                // Walkable top surface: from ground landing up to TopLeft and across TopRight
                walkableBridgeWorld = new Vector2[]
                {
                    groundHit,
                    worldTopLeft,
                    worldTopRight
                };

                fillVertices = new Vector3[]
                {
                    new Vector3(worldTopRight.x, worldTopRight.y, 0f),
                    new Vector3(worldTopLeft.x, worldTopLeft.y, 0f),
                    new Vector3(groundHit.x, groundHit.y, 0f),
                    new Vector3(worldBottomLeft.x, worldBottomLeft.y, 0f)
                };
            }

            // 1. Update EdgeCollider2D (Local Space)
            Vector2[] localColliderPoints = new Vector2[walkableBridgeWorld.Length];
            for (int i = 0; i < walkableBridgeWorld.Length; i++)
            {
                localColliderPoints[i] = transform.InverseTransformPoint(walkableBridgeWorld[i]);
            }
            edgeCollider.points = localColliderPoints;

            // 2. Update Luminous Cyan Rim LineRenderer
            if (renderWalkableRim && rimRenderer != null)
            {
                rimRenderer.positionCount = walkableBridgeWorld.Length;
                for (int i = 0; i < walkableBridgeWorld.Length; i++)
                {
                    rimRenderer.SetPosition(i, new Vector3(walkableBridgeWorld[i].x, walkableBridgeWorld[i].y, 0f));
                }
            }

            // 3. Update Translucent Shadow Fill Mesh
            if (renderShadowFill && fillMesh != null)
            {
                UpdateFillMesh(fillVertices);
            }
        }

        private Vector2 ProjectRayToGround(Vector2 corner, Vector2 dir)
        {
            if (dir.y >= -0.001f)
            {
                dir.y = -0.001f;
                dir.Normalize();
            }

            // Raycast towards ground layer
            Vector2 rayOrigin = corner + dir * 0.05f;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, maxShadowDistance, groundLayerMask);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                {
                    return hit.point;
                }
            }

            // Intersect with fallback ground plane
            if (corner.y > fallbackGroundY)
            {
                float t = (fallbackGroundY - corner.y) / dir.y;
                if (t > 0f)
                {
                    float clampedT = Mathf.Min(t, maxShadowDistance);
                    return corner + dir * clampedT;
                }
            }

            return corner + dir * maxShadowDistance;
        }

        private void SetupVisuals()
        {
            // 1. Setup Walkable Cyan Rim
            if (renderWalkableRim)
            {
                rimRenderer = GetComponent<LineRenderer>();
                if (rimRenderer == null) rimRenderer = gameObject.AddComponent<LineRenderer>();

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null)
                {
                    rimRenderer.material = new Material(shader) { color = rimColor };
                }
                rimRenderer.startWidth = rimWidth;
                rimRenderer.endWidth = rimWidth;
                rimRenderer.useWorldSpace = true;
                rimRenderer.sortingOrder = 4; // Above shadows and background, crisp and readable
            }

            // 2. Setup Translucent Shadow Fill (Dedicated child object with sortingOrder -10)
            if (renderShadowFill)
            {
                Transform existingFill = transform.Find("Shadow_Translucent_Fill");
                GameObject fillObj = existingFill != null ? existingFill.gameObject : new GameObject("Shadow_Translucent_Fill");
                fillObj.transform.SetParent(transform, false);
                fillObj.transform.localPosition = Vector3.zero;
                fillObj.transform.localRotation = Quaternion.identity;
                fillObj.transform.localScale = Vector3.one;

                fillMeshFilter = fillObj.GetComponent<MeshFilter>();
                if (fillMeshFilter == null) fillMeshFilter = fillObj.AddComponent<MeshFilter>();

                fillMeshRenderer = fillObj.GetComponent<MeshRenderer>();
                if (fillMeshRenderer == null) fillMeshRenderer = fillObj.AddComponent<MeshRenderer>();

                if (fillMesh == null)
                {
                    fillMesh = new Mesh { name = "ShadowFillMesh" };
                    fillMesh.MarkDynamic();
                    fillMeshFilter.sharedMesh = fillMesh;
                }

                Shader fillShader = Shader.Find("Sprites/Default");
                if (fillShader == null) fillShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (fillShader != null)
                {
                    fillMeshRenderer.sharedMaterial = new Material(fillShader) { color = shadowFillColor };
                }
                fillMeshRenderer.sortingOrder = -10; // Behind platforms and player, but in front of sky canvas
            }
        }

        private void UpdateFillMesh(Vector3[] worldVerts)
        {
            if (worldVerts == null || worldVerts.Length < 3 || fillMesh == null) return;

            // Convert world vertices into fill object's local space
            Vector3[] localVerts = new Vector3[worldVerts.Length];
            for (int i = 0; i < worldVerts.Length; i++)
            {
                localVerts[i] = fillMeshFilter.transform.InverseTransformPoint(worldVerts[i]);
            }

            fillMesh.Clear();
            fillMesh.vertices = localVerts;

            // Quad triangles (0-1-2 and 0-2-3)
            int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            fillMesh.triangles = triangles;
            fillMesh.RecalculateBounds();
        }

        private void SetupShadowCaster()
        {
            var caster = GetComponent<ShadowCaster2D>();
            if (caster == null)
            {
                caster = gameObject.AddComponent<ShadowCaster2D>();
            }

            caster.castsShadows = true;
            caster.useRendererSilhouette = true;
        }
    }
}
