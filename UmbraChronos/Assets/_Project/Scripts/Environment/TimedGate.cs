using UnityEngine;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// A puzzle gate that slides open vertically when activated by a PressurePlate.
    /// Smoothly returns to closed position when the plate is released.
    /// </summary>
    public class TimedGate : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 openOffset = new Vector2(0f, 3.2f);
        [SerializeField] private float slideSpeed = 6f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer gateRenderer;
        [SerializeField] private Color closedColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        [SerializeField] private Color openColor = new Color(0.22f, 0.74f, 0.97f, 0.85f); // Glowing cyan #38BDF8

        private Vector2 closedPos;
        private Vector2 targetPos;
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            closedPos = transform.position;
            targetPos = closedPos;

            if (gateRenderer == null)
            {
                gateRenderer = GetComponent<SpriteRenderer>();
            }

            if (gateRenderer != null)
            {
                gateRenderer.color = closedColor;
            }
        }

        private void Update()
        {
            // Smoothly slide towards target position
            transform.position = Vector2.MoveTowards(transform.position, targetPos, slideSpeed * UnityEngine.Time.deltaTime);

            if (gateRenderer != null)
            {
                Color targetCol = isOpen ? openColor : closedColor;
                gateRenderer.color = Color.Lerp(gateRenderer.color, targetCol, 5f * UnityEngine.Time.deltaTime);
            }
        }

        /// <summary>
        /// Command the gate to slide open (true) or close (false).
        /// </summary>
        public void SetOpen(bool open)
        {
            if (isOpen != open)
            {
                isOpen = open;
                targetPos = open ? (closedPos + openOffset) : closedPos;
                UmbraChronos.Core.TimeEvents.OnGateStateChanged?.Invoke(isOpen);
            }
        }
    }
}
