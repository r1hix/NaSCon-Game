using UnityEngine;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Generates a dark interior backdrop sprite behind enclosed shelters, caverns, and rooms.
    /// Placed at sortingOrder = -12 (between the sky canvas at -20 and foreground platforms at 0)
    /// to visually separate indoor shaded zones from the bright outdoor sunlit sky.
    ///
    /// Attach this component to any roof/ceiling GameObject. It will automatically create a
    /// dark rectangle filling the space below the roof down to the specified floor level.
    /// </summary>
    [ExecuteAlways]
    public class InteriorBackdrop : MonoBehaviour
    {
        [Header("Interior Zone")]
        [Tooltip("The Y-level of the floor beneath this roof/ceiling. The backdrop extends from the roof down to this Y.")]
        [SerializeField] private float floorY = -3.25f;

        [Tooltip("Optional horizontal padding added to each side of the backdrop beyond the roof bounds.")]
        [SerializeField] private float horizontalPadding = 0.5f;

        [Tooltip("Optional vertical padding added above the roof for visual breathing room.")]
        [SerializeField] private float verticalPaddingAbove = 0.25f;

        [Header("Visual Settings")]
        [Tooltip("Primary dark interior color (#08080E obsidian chamber dark).")]
        [SerializeField] private Color backdropColor = new Color(0.031f, 0.031f, 0.055f, 1.0f); // #08080E

        [Tooltip("Sorting order for the backdrop (-12 places it behind platforms but in front of sky).")]
        [SerializeField] private int sortingOrder = -12;

        [Header("Gradient Fade (Optional)")]
        [Tooltip("If true, renders a subtle vertical gradient fade at the exposed edges of the interior zone.")]
        [SerializeField] private bool useEdgeFade = true;

        [Tooltip("Width of the gradient fade strips at the left/right exposed openings.")]
        [SerializeField] private float edgeFadeWidth = 1.2f;

        // Internal references
        private SpriteRenderer backdropRenderer;
        private GameObject backdropObj;

        // Edge fade meshes
        private MeshFilter leftFadeMeshFilter;
        private MeshRenderer leftFadeMeshRenderer;
        private Mesh leftFadeMesh;
        private MeshFilter rightFadeMeshFilter;
        private MeshRenderer rightFadeMeshRenderer;
        private Mesh rightFadeMesh;

        private void OnEnable()
        {
            CreateBackdrop();
            UpdateBackdrop();
        }

        private void OnDisable()
        {
            CleanupBackdrop();
        }

        private void Update()
        {
            // Keep backdrop synchronized in Edit Mode and at runtime
            UpdateBackdrop();
        }

        private void CreateBackdrop()
        {
            if (backdropObj != null) return;

            // Create the main dark interior backdrop
            backdropObj = new GameObject("Interior_Backdrop");
            backdropObj.transform.SetParent(transform, false);

            backdropRenderer = backdropObj.AddComponent<SpriteRenderer>();

            // Use the default white square sprite for a clean filled rectangle
            Sprite whiteSquare = FindWhiteSquareSprite();
            if (whiteSquare != null)
            {
                backdropRenderer.sprite = whiteSquare;
            }

            // Assign the unlit shader so the backdrop isn't affected by scene lighting
            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                backdropRenderer.material = new Material(unlitShader);
            }

            backdropRenderer.color = backdropColor;
            backdropRenderer.sortingOrder = sortingOrder;

            // Create edge fade meshes if enabled
            if (useEdgeFade)
            {
                CreateEdgeFade("Interior_Fade_Left", ref leftFadeMeshFilter, ref leftFadeMeshRenderer, ref leftFadeMesh);
                CreateEdgeFade("Interior_Fade_Right", ref rightFadeMeshFilter, ref rightFadeMeshRenderer, ref rightFadeMesh);
            }
        }

        private void CreateEdgeFade(string name, ref MeshFilter mf, ref MeshRenderer mr, ref Mesh mesh)
        {
            GameObject fadeObj = new GameObject(name);
            fadeObj.transform.SetParent(backdropObj.transform, false);

            mf = fadeObj.AddComponent<MeshFilter>();
            mr = fadeObj.AddComponent<MeshRenderer>();

            mesh = new Mesh { name = name + "_Mesh" };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material fadeMat = new Material(unlitShader);
                fadeMat.color = Color.white; // Vertex colors will handle the gradient
                mr.sharedMaterial = fadeMat;
            }
            mr.sortingOrder = sortingOrder; // Same layer as backdrop
        }

        private void UpdateBackdrop()
        {
            if (backdropRenderer == null || backdropObj == null)
            {
                CreateBackdrop();
                if (backdropRenderer == null) return;
            }

            // Calculate the roof's world bounds
            Collider2D col = GetComponent<Collider2D>();
            SpriteRenderer parentSr = GetComponent<SpriteRenderer>();

            float roofMinX, roofMaxX, roofMinY, roofMaxY;

            if (col != null)
            {
                Bounds b = col.bounds;
                roofMinX = b.min.x;
                roofMaxX = b.max.x;
                roofMinY = b.min.y;
                roofMaxY = b.max.y;
            }
            else if (parentSr != null)
            {
                Bounds b = parentSr.bounds;
                roofMinX = b.min.x;
                roofMaxX = b.max.x;
                roofMinY = b.min.y;
                roofMaxY = b.max.y;
            }
            else
            {
                // Fallback: use transform scale as an approximation
                Vector3 pos = transform.position;
                Vector3 scale = transform.lossyScale;
                roofMinX = pos.x - scale.x * 0.5f;
                roofMaxX = pos.x + scale.x * 0.5f;
                roofMinY = pos.y - scale.y * 0.5f;
                roofMaxY = pos.y + scale.y * 0.5f;
            }

            // Backdrop fills from roof top down to the floor
            float width = (roofMaxX - roofMinX) + (horizontalPadding * 2f);
            float height = (roofMaxY + verticalPaddingAbove) - floorY;
            float centerX = (roofMinX + roofMaxX) * 0.5f;
            float centerY = ((roofMaxY + verticalPaddingAbove) + floorY) * 0.5f;

            backdropObj.transform.position = new Vector3(centerX, centerY, 0.1f); // Slightly behind
            backdropObj.transform.localScale = new Vector3(width, height, 1f);

            backdropRenderer.color = backdropColor;
            backdropRenderer.sortingOrder = sortingOrder;

            // Update edge fade gradients
            if (useEdgeFade && leftFadeMesh != null && rightFadeMesh != null)
            {
                float fadeTop = roofMaxY + verticalPaddingAbove;
                float fadeBottom = floorY;
                float leftEdge = roofMinX - horizontalPadding;
                float rightEdge = roofMaxX + horizontalPadding;

                UpdateEdgeFadeMesh(leftFadeMesh, leftFadeMeshFilter,
                    leftEdge - edgeFadeWidth, leftEdge, fadeBottom, fadeTop, true);
                UpdateEdgeFadeMesh(rightFadeMesh, rightFadeMeshFilter,
                    rightEdge, rightEdge + edgeFadeWidth, fadeBottom, fadeTop, false);
            }
        }

        private void UpdateEdgeFadeMesh(Mesh mesh, MeshFilter mf, float xStart, float xEnd, float yBottom, float yTop, bool fadeFromRight)
        {
            // Gradient quad: opaque on the interior side, transparent on the exterior side
            // fadeFromRight = true means left edge (opaque on right side, fading to transparent on left)

            Vector3[] worldVerts = new Vector3[]
            {
                new Vector3(xStart, yBottom, 0.1f), // Bottom-left
                new Vector3(xEnd, yBottom, 0.1f),   // Bottom-right
                new Vector3(xEnd, yTop, 0.1f),      // Top-right
                new Vector3(xStart, yTop, 0.1f),    // Top-left
            };

            Vector3[] localVerts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                localVerts[i] = mf.transform.InverseTransformPoint(worldVerts[i]);
            }

            Color opaqueColor = backdropColor;
            Color transparentColor = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0f);

            Color[] colors;
            if (fadeFromRight)
            {
                // Left edge: transparent on the left, opaque on the right
                colors = new Color[]
                {
                    transparentColor, // Bottom-left
                    opaqueColor,      // Bottom-right
                    opaqueColor,      // Top-right
                    transparentColor, // Top-left
                };
            }
            else
            {
                // Right edge: opaque on the left, transparent on the right
                colors = new Color[]
                {
                    opaqueColor,      // Bottom-left
                    transparentColor, // Bottom-right
                    transparentColor, // Top-right
                    opaqueColor,      // Top-left
                };
            }

            mesh.Clear();
            mesh.vertices = localVerts;
            mesh.colors = colors;
            mesh.triangles = new int[]
            {
                0, 2, 1,
                0, 3, 2
            };
            mesh.RecalculateBounds();
        }

        private Sprite FindWhiteSquareSprite()
        {
            // Try to load the project's Square_32 sprite
            Sprite sprite = null;

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Square_32.png");
#endif

            if (sprite != null) return sprite;

            // Fallback: create a 4x4 white texture at runtime
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void CleanupBackdrop()
        {
            if (backdropObj != null)
            {
                if (Application.isPlaying)
                    Destroy(backdropObj);
                else
                    DestroyImmediate(backdropObj);
                backdropObj = null;
                backdropRenderer = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Show the interior zone in the editor
            Collider2D col = GetComponent<Collider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            float minX, maxX, maxY;

            if (col != null)
            {
                Bounds b = col.bounds;
                minX = b.min.x - horizontalPadding;
                maxX = b.max.x + horizontalPadding;
                maxY = b.max.y + verticalPaddingAbove;
            }
            else if (sr != null)
            {
                Bounds b = sr.bounds;
                minX = b.min.x - horizontalPadding;
                maxX = b.max.x + horizontalPadding;
                maxY = b.max.y + verticalPaddingAbove;
            }
            else
            {
                Vector3 pos = transform.position;
                Vector3 scale = transform.lossyScale;
                minX = pos.x - scale.x * 0.5f - horizontalPadding;
                maxX = pos.x + scale.x * 0.5f + horizontalPadding;
                maxY = pos.y + scale.y * 0.5f + verticalPaddingAbove;
            }

            float width = maxX - minX;
            float height = maxY - floorY;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, (maxY + floorY) * 0.5f, 0f);

            Gizmos.color = new Color(0.03f, 0.03f, 0.06f, 0.4f);
            Gizmos.DrawCube(center, new Vector3(width, height, 0.01f));

            Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.6f);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.01f));
        }
    }
}
