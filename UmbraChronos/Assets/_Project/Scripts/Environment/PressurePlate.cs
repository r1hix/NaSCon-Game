using UnityEngine;
using UmbraChronos.Core;
using UmbraChronos.Time;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Rock-solid Pressure Plate trigger for Player, UmbralEcho, or Pushable Blocks.
    /// Uses an anchored spatial sensor to eliminate jitter, bouncing, or physical tripping.
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        [Header("Connected Objects")]
        [SerializeField] private TimedGate targetGate;

        [Header("Sensor Area")]
        [Tooltip("Detection box size on top of the plate")]
        [SerializeField] private Vector2 sensorSize = new Vector2(1.8f, 0.5f);
        [SerializeField] private Vector2 sensorOffset = new Vector2(0f, 0.25f);
        [SerializeField] private LayerMask detectionLayers = ~0;

        [Header("Visual Depression")]
        [SerializeField] private SpriteRenderer plateRenderer;
        [SerializeField] private float depressDistance = 0.08f;
        [SerializeField] private float visualMoveSpeed = 8f;

        [Header("Colors")]
        [SerializeField] private Color inactiveColor = new Color(0.85f, 0.45f, 0.1f, 1f); // Burnt Amber (#D97706)
        [SerializeField] private Color activeColor = new Color(0.22f, 0.74f, 0.97f, 1f);   // Neon Cyan (#38BDF8)

        private Vector2 baseWorldPos;
        private Vector2 visualRestingLocalPos;
        private Vector2 visualDepressedLocalPos;
        private Vector2 visualTargetLocalPos;
        private bool isPressed;

        public bool IsPressed => isPressed;

        private void Awake()
        {
            baseWorldPos = transform.position;

            if (plateRenderer == null)
            {
                plateRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (plateRenderer != null)
            {
                visualRestingLocalPos = plateRenderer.transform.localPosition;
                visualDepressedLocalPos = visualRestingLocalPos - new Vector2(0f, depressDistance);
                visualTargetLocalPos = visualRestingLocalPos;
                plateRenderer.color = inactiveColor;
            }

            // Ensure our own collider is a trigger so the player never trips on it
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void FixedUpdate()
        {
            CheckOccupancy();
        }

        private void Update()
        {
            // Smoothly animate the plate visual up/down and crossfade color
            if (plateRenderer != null)
            {
                plateRenderer.transform.localPosition = Vector2.MoveTowards(
                    plateRenderer.transform.localPosition, 
                    visualTargetLocalPos, 
                    visualMoveSpeed * UnityEngine.Time.deltaTime);

                Color targetCol = isPressed ? activeColor : inactiveColor;
                plateRenderer.color = Color.Lerp(plateRenderer.color, targetCol, 8f * UnityEngine.Time.deltaTime);
            }
        }

        private void CheckOccupancy()
        {
            // Anchored to the initial base position so visual depression never breaks sensor contact
            Vector2 checkCenter = baseWorldPos + sensorOffset;
            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, sensorSize, 0f, detectionLayers);

            bool occupied = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.gameObject == gameObject) continue;
                if (hit.isTrigger) continue; // ignore other triggers

                // Check for Player, UmbralEcho, or any physical object with Rigidbody
                if (hit.CompareTag("Player") || 
                    hit.GetComponentInParent<UmbralEcho>() != null || 
                    hit.attachedRigidbody != null)
                {
                    occupied = true;
                    break;
                }
            }

            SetPressed(occupied);
        }

        private void SetPressed(bool pressed)
        {
            if (isPressed == pressed) return;

            isPressed = pressed;
            visualTargetLocalPos = isPressed ? visualDepressedLocalPos : visualRestingLocalPos;

            TimeEvents.OnPressurePlateChanged?.Invoke(isPressed);

            if (targetGate != null)
            {
                targetGate.SetOpen(isPressed);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isPressed ? Color.cyan : Color.yellow;
            Vector2 checkCenter = Application.isPlaying ? (baseWorldPos + sensorOffset) : ((Vector2)transform.position + sensorOffset);
            Gizmos.DrawWireCube(checkCenter, sensorSize);
        }
    }
}
