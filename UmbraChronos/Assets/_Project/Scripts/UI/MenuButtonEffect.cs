using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UmbraChronos.UI
{
    /// <summary>
    /// Hollow-Knight-flavoured menu entry animation.
    /// A resting entry is dim and still; a highlighted entry brightens to solar cyan,
    /// eases to the right, breathes gently, and reveals a caret glyph beside the label.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform labelTransform;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform cursor;
        [SerializeField] private Image cursorImage;

        [Header("Colours")]
        [SerializeField] private Color idleColor = new Color(0.60f, 0.66f, 0.76f, 0.85f);
        [SerializeField] private Color highlightColor = new Color(0.22f, 0.74f, 0.97f, 1f);

        [Header("Motion")]
        [Tooltip("Horizontal distance the label travels when highlighted, in canvas units.")]
        [SerializeField] private float slideDistance = 16f;
        [SerializeField] private float highlightScale = 1.09f;
        [Tooltip("How quickly the entry eases into its highlighted pose.")]
        [SerializeField] private float responsiveness = 13f;
        [Tooltip("Idle 'breathing' scale amount applied to the label.")]
        [SerializeField] private float breathAmount = 0.03f;
        [SerializeField] private float breathSpeed = 2.1f;
        [SerializeField] private float cursorSlideDistance = 14f;

        private Button button;
        private bool isHighlighted;
        private float highlightT;
        private float pressT;
        private float phase;

        private Vector3 labelBaseScale = Vector3.one;
        private Vector2 labelBasePos;
        private Vector2 cursorBasePos;
        private bool hasLabel;
        private bool hasCursor;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                // We drive all visuals ourselves; disable the default colour tinting.
                button.transition = Selectable.Transition.None;
            }

            if (labelTransform == null && label != null) labelTransform = label.rectTransform;
            if (labelTransform != null)
            {
                labelBasePos = labelTransform.anchoredPosition;
                labelBaseScale = labelTransform.localScale;
                hasLabel = true;
            }

            if (cursor != null)
            {
                cursorBasePos = cursor.anchoredPosition;
                hasCursor = true;
            }

            phase = Random.value * 6.28318f;
        }

        private void OnEnable()
        {
            isHighlighted = false;
        }

        private void Update()
        {
            float dt = UnityEngine.Time.unscaledDeltaTime;
            float target = isHighlighted ? 1f : 0f;
            highlightT = Mathf.MoveTowards(highlightT, target, responsiveness * dt);
            pressT = Mathf.MoveTowards(pressT, 0f, 6f * dt);

            if (hasLabel)
            {
                float breath = Mathf.Sin((UnityEngine.Time.unscaledTime * breathSpeed) + phase) * breathAmount;
                breath *= 0.35f + (0.65f * highlightT);

                float scale = (1f + ((highlightScale - 1f) * highlightT)) * (1f + breath) * (1f - (0.06f * pressT));
                labelTransform.localScale = labelBaseScale * scale;
                labelTransform.anchoredPosition = new Vector2(labelBasePos.x + (slideDistance * highlightT), labelBasePos.y);
            }

            if (hasCursor)
            {
                bool showCursor = highlightT > 0.02f;
                if (cursor.gameObject.activeSelf != showCursor) cursor.gameObject.SetActive(showCursor);

                if (showCursor)
                {
                    cursor.anchoredPosition = new Vector2(cursorBasePos.x - (cursorSlideDistance * (1f - highlightT)), cursorBasePos.y);
                }
            }

            if (label != null)
            {
                label.color = Color.Lerp(idleColor, highlightColor, highlightT);
            }

            if (cursorImage != null)
            {
                Color c = highlightColor;
                c.a = highlightT;
                cursorImage.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);
        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pressT = 1f;
        }

        public void OnPointerUp(PointerEventData eventData) => pressT = 0f;

        private void SetHighlighted(bool value)
        {
            isHighlighted = value;
        }
    }
}
