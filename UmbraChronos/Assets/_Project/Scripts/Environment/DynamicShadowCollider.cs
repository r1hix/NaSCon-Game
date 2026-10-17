using System;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Projects an EdgeCollider2D along the top surface of a monolith's cast shadow based on sun position.
    /// In Umbra Chronos, shadows are solid physical matter:
    /// - When the sun rotates ([Q] / [E] / mouse scroll), monoliths cast dynamic shadows.
    /// - This component updates an EdgeCollider2D so the top edge of the cast shadow acts as a solid walkable bridge.
    /// - Uses thin edge geometry (NOT a solid filled block) to prevent bulldozing physics objects or trapping the player.
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

        [Header("Bridge Edge Visual (Optional)")]
        [Tooltip("Optional subtle LineRenderer showing the walkable rim of the shadow.")]
        [SerializeField] private bool showBridgeLine = false;

        [SerializeField] private Color bridgeLineColor = new Color(0.22f, 0.74f, 0.97f, 0.7f); // Subtle cyan rim #38BDF8

        // Cached components
        private EdgeCollider2D edgeCollider;
        private LineRenderer lineRenderer;
        private BoxCollider2D boxCollider;
        private Vector2 lastSunPosition;

        private void Awake()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();
            boxCollider = GetComponent<BoxCollider2D>();

            if (showBridgeLine)
            {
                SetupLineRenderer();
            }
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

            // Get top corners of the monolith in world space
            Vector2 worldTopLeft, worldTopRight;
            if (boxCollider != null)
            {
                Bounds b = boxCollider.bounds;
                worldTopLeft = new Vector2(b.min.x, b.max.y);
                worldTopRight = new Vector2(b.max.x, b.max.y);
            }
            else
            {
                Vector2 half = transform.lossyScale * 0.5f;
                Vector2 center = transform.position;
                worldTopLeft = new Vector2(center.x - half.x, center.y + half.y);
                worldTopRight = new Vector2(center.x + half.x, center.y + half.y);
            }

            float monolithCenterX = (worldTopLeft.x + worldTopRight.x) * 0.5f;
            Vector2[] worldPoints;

            if (sunPos.x <= monolithCenterX)
            {
                // Sun is to the LEFT -> Shadow stretches to the RIGHT
                Vector2 dirRight = (worldTopRight - sunPos).normalized;
                Vector2 groundHit = ProjectRayToGround(worldTopRight, dirRight);

                // Top surface bridge: TopLeft -> TopRight -> GroundHit
                worldPoints = new Vector2[]
                {
                    worldTopLeft,
                    worldTopRight,
                    groundHit
                };
            }
            else
            {
                // Sun is to the RIGHT -> Shadow stretches to the LEFT
                Vector2 dirLeft = (worldTopLeft - sunPos).normalized;
                Vector2 groundHit = ProjectRayToGround(worldTopLeft, dirLeft);

                // Top surface bridge: GroundHit -> TopLeft -> TopRight
                worldPoints = new Vector2[]
                {
                    groundHit,
                    worldTopLeft,
                    worldTopRight
                };
            }

            // Convert world points to local space of EdgeCollider2D
            Vector2[] localPoints = new Vector2[worldPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++)
            {
                localPoints[i] = transform.InverseTransformPoint(worldPoints[i]);
            }

            edgeCollider.points = localPoints;

            // Optional line renderer
            if (showBridgeLine && lineRenderer != null)
            {
                lineRenderer.positionCount = worldPoints.Length;
                for (int i = 0; i < worldPoints.Length; i++)
                {
                    lineRenderer.SetPosition(i, new Vector3(worldPoints[i].x, worldPoints[i].y, 0f));
                }
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

        private void SetupLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineRenderer.material = new Material(shader) { color = bridgeLineColor };
            }
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.06f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 3;
        }
    }
}
