using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.Rendering.Universal;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Controls the celestial Sun's orbital rotation across the sky.
    /// In Umbra Chronos, players can rotate the celestial light source using [Q] / [E] or Mouse Scroll:
    /// - As the sun rotates, shadows cast by monoliths stretch, pivot, and contract.
    /// - Invokes TimeEvents.OnSunAngleChanged so shadow colliders and HUD elements stay in sync.
    /// </summary>
    public class SunDialController : MonoBehaviour
    {
        [Header("Sun & Orbit Settings")]
        [Tooltip("The primary celestial 2D light source. If null, locates the child or scene light.")]
        [SerializeField] private Light2D sunLight;

        [Tooltip("Center of the celestial orbit arc.")]
        [SerializeField] private Vector2 orbitCenter = new Vector2(1f, 0f);

        [Tooltip("Distance from the orbit center to the sun.")]
        [SerializeField] private float orbitRadius = 14f;

        [Tooltip("Current sun angle in degrees (90 is straight overhead at noon).")]
        [Range(15f, 165f)]
        [SerializeField] private float currentAngle = 90f;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotationSpeed = 85f;

        [Tooltip("Minimum allowable sun angle (dawn / low horizon).")]
        [SerializeField] private float minAngle = 20f;

        [Tooltip("Maximum allowable sun angle (dusk / low horizon).")]
        [SerializeField] private float maxAngle = 160f;

        [Header("Visuals & Juice")]
        [Tooltip("Optional visual sprite for the sun orb in the sky.")]
        [SerializeField] private Transform sunVisualOrb;

        // Internal State
        private float targetAngle = 90f;
        private bool isRewinding;

        public float CurrentAngle => currentAngle;
        public Vector2 SunWorldPosition => CalculatePositionFromAngle(currentAngle);

        private void Awake()
        {
            if (sunLight == null)
            {
                sunLight = GetComponentInChildren<Light2D>();
            }

            targetAngle = currentAngle;
            UpdateSunPosition(true);
        }

        private void OnEnable()
        {
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
        }

        private void Update()
        {
            if (isRewinding) return;

            ReadInput();
            SmoothRotate();
        }

        private void ReadInput()
        {
            float input = 0f;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) input -= 1f;
                if (keyboard.eKey.isPressed) input += 1f;
                if (keyboard.leftBracketKey.isPressed) input -= 1f;
                if (keyboard.rightBracketKey.isPressed) input += 1f;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.y.ReadValue();
                if (Mathf.Abs(scroll) > 0.05f)
                {
                    input += Mathf.Sign(scroll) * 0.75f;
                }
            }
#endif

            // Dual input fallback if Input System didn't register
            if (Mathf.Abs(input) < 0.01f)
            {
                try
                {
                    if (Input.GetKey(KeyCode.Q)) input -= 1f;
                    if (Input.GetKey(KeyCode.E)) input += 1f;

                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        input += Mathf.Sign(scroll) * 0.75f;
                    }
                }
                catch (System.Exception) { }
            }

            if (Mathf.Abs(input) > 0.01f)
            {
                targetAngle = Mathf.Clamp(targetAngle + (input * rotationSpeed * UnityEngine.Time.deltaTime), minAngle, maxAngle);
            }
        }

        private void SmoothRotate()
        {
            float prevAngle = currentAngle;
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * 1.5f * UnityEngine.Time.deltaTime);

            if (Mathf.Abs(currentAngle - prevAngle) > 0.01f)
            {
                UpdateSunPosition(false);
                TimeEvents.OnSunAngleChanged?.Invoke(currentAngle);
            }
        }

        private void UpdateSunPosition(bool snapImmediate)
        {
            Vector2 worldPos = CalculatePositionFromAngle(currentAngle);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            if (sunLight != null)
            {
                sunLight.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            }

            if (sunVisualOrb != null)
            {
                sunVisualOrb.position = new Vector3(worldPos.x, worldPos.y, 0f);
            }
        }

        private Vector2 CalculatePositionFromAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            // 90 deg = overhead (Cos(90) = 0, Sin(90) = 1)
            float x = orbitCenter.x - (Mathf.Cos(rad) * orbitRadius);
            float y = orbitCenter.y + (Mathf.Sin(rad) * orbitRadius);
            return new Vector2(x, y);
        }

        private void HandleRewindStateChanged(bool active)
        {
            isRewinding = active;
        }

        public void SetAngleImmediate(float angle)
        {
            targetAngle = Mathf.Clamp(angle, minAngle, maxAngle);
            currentAngle = targetAngle;
            UpdateSunPosition(true);
            TimeEvents.OnSunAngleChanged?.Invoke(currentAngle);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.4f);
            const int segments = 32;
            Vector3 prev = CalculatePositionFromAngle(minAngle);
            for (int i = 1; i <= segments; i++)
            {
                float a = Mathf.Lerp(minAngle, maxAngle, (float)i / segments);
                Vector3 next = CalculatePositionFromAngle(a);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(CalculatePositionFromAngle(currentAngle), 0.6f);
        }
    }
}
