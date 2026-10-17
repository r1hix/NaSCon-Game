using UnityEngine;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Generates a solid dark interior backdrop sprite (#08080E) behind enclosed chambers,
    /// vaults, and shelters.
    ///
    /// Placed at sortingOrder = -12 (between the sky canvas at -20 and foreground platforms at 0)
    /// to visually occlude outdoor sky glow and godrays, rendering indoor puzzle chambers
    /// deep, moody, and distinct from open sunlit chasms.
    ///
    /// Performance & Stability:
    /// - Normalizes scale using world-space geometry so child objects NEVER inherit parent transform blowups.
    /// - Static shared unlit material prevents Edit-Mode material leaking.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class InteriorBackdrop : MonoBehaviour
    {
        [Header("Chamber Dimensions")]
        [Tooltip("The Y-level of the floor beneath this ceiling. The backdrop extends from the ceiling down to this Y.")]
        [SerializeField] private float floorY = -1.75f;

        [Tooltip("Extra horizontal padding beyond the ceiling width.")]
        [SerializeField] private float horizontalPadding = 0.5f;

        [Tooltip("Extra vertical padding above the ceiling.")]
        [SerializeField] private float verticalPaddingAbove = 0.25f;

        [Header("Visual Settings")]
        [Tooltip("Deep obsidian interior shade (#07070C).")]
        [SerializeField] private Color backdropColor = new Color(0.027f, 0.027f, 0.047f, 0.98f);

        [Tooltip("Sorting order for the interior backdrop (-12 places it behind platforms and player, but in front of sky).")]
        [SerializeField] private int sortingOrder = -12;

        private GameObject backdropObj;
        private SpriteRenderer backdropRenderer;
        private BoxCollider2D boxCollider;

        private static Material s_SharedBackdropMaterial;
        private static Sprite s_SharedWhiteSprite;

        private void OnEnable()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            UpdateBackdropGeometry();
        }

        private void OnDisable()
        {
            if (backdropObj != null)
            {
                backdropObj.SetActive(false);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdateBackdropGeometry();
            }
#endif
        }

        public void UpdateBackdropGeometry()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return;

            Transform child = transform.Find("Interior_Backdrop_Quad");
            if (child == null)
            {
                backdropObj = new GameObject("Interior_Backdrop_Quad");
                backdropObj.transform.SetParent(transform, false);
            }
            else
            {
                backdropObj = child.gameObject;
            }

            backdropObj.SetActive(true);

            Vector3 lossy = transform.lossyScale;
            float invLossyX = Mathf.Abs(lossy.x) > 0.0001f ? (1f / lossy.x) : 1f;
            float invLossyY = Mathf.Abs(lossy.y) > 0.0001f ? (1f / lossy.y) : 1f;

            // Compute world geometry
            float roofBottomWorldY = transform.position.y + ((boxCollider.offset.y - (boxCollider.size.y * 0.5f)) * lossy.y);
            float ceilingTopWorldY = transform.position.y + ((boxCollider.offset.y + (boxCollider.size.y * 0.5f)) * lossy.y) + verticalPaddingAbove;

            float worldHeight = Mathf.Max(0.2f, ceilingTopWorldY - floorY);
            float worldWidth = (boxCollider.size.x * Mathf.Abs(lossy.x)) + (horizontalPadding * 2f);

            float centerWorldX = transform.position.x + (boxCollider.offset.x * lossy.x);
            float centerWorldY = floorY + (worldHeight * 0.5f);

            // Set local position relative to parent
            float localCenterX = boxCollider.offset.x;
            float localCenterY = (centerWorldY - transform.position.y) * invLossyY;

            backdropObj.transform.localPosition = new Vector3(localCenterX, localCenterY, 0.05f);
            backdropObj.transform.localRotation = Quaternion.identity;

            // Counteract parent lossyScale so dimensions match exact world meters
            backdropObj.transform.localScale = new Vector3(worldWidth * invLossyX, worldHeight * invLossyY, 1f);

            backdropRenderer = backdropObj.GetComponent<SpriteRenderer>();
            if (backdropRenderer == null) backdropRenderer = backdropObj.AddComponent<SpriteRenderer>();

            if (s_SharedWhiteSprite == null) s_SharedWhiteSprite = CreateWhiteSprite();
            backdropRenderer.sprite = s_SharedWhiteSprite;

            if (s_SharedBackdropMaterial == null)
            {
                Shader uShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (uShader == null) uShader = Shader.Find("Sprites/Default");
                if (uShader != null)
                {
                    s_SharedBackdropMaterial = new Material(uShader) { name = "InteriorBackdrop_SharedMat" };
                }
            }

            if (s_SharedBackdropMaterial != null)
            {
                backdropRenderer.sharedMaterial = s_SharedBackdropMaterial;
            }

            backdropRenderer.color = backdropColor;
            backdropRenderer.sortingOrder = sortingOrder;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.name = "BackdropWhiteTex";
            Color[] colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}
