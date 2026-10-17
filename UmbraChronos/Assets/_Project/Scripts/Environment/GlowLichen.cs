using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Bioluminescent ambient wall-clinging lichen/moss (inspired by Hollow Knight / Minecraft glow lichen).
    /// Emits a soft, safe ambient glow that illuminates obsidian stone corners and deep chambers
    /// without relying on the celestial sun, ensuring the dark world remains readable and atmospheric.
    /// Does NOT cast lethal radiation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GlowLichen : MonoBehaviour
    {
        [Header("Lighting")]
        [SerializeField] private Light2D pointLight;
        [SerializeField] private Color glowColor = new Color(0.22f, 0.74f, 0.97f, 1f); // Bioluminescent Cyan #38BDF8
        [SerializeField] private float baseIntensity = 0.55f;
        [SerializeField] private float lightRadius = 3.2f;

        [Header("Organic Pulse")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 1.6f;
        [SerializeField] private float pulseAmount = 0.18f;

        private SpriteRenderer spriteRenderer;
        private float randomPhase;
        private Color originalSpriteColor;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            randomPhase = Random.Range(0f, Mathf.PI * 2f);

            if (spriteRenderer != null)
            {
                originalSpriteColor = spriteRenderer.color;
            }

            if (pointLight == null)
            {
                pointLight = GetComponentInChildren<Light2D>();
            }

            if (pointLight == null)
            {
                GameObject lightChild = new GameObject("Lichen_PointLight");
                lightChild.transform.SetParent(transform, false);
                lightChild.transform.localPosition = Vector3.zero;

                pointLight = lightChild.AddComponent<Light2D>();
                pointLight.lightType = Light2D.LightType.Point;
                pointLight.pointLightInnerRadius = 0.2f;
                pointLight.pointLightOuterRadius = lightRadius;
                pointLight.color = glowColor;
                pointLight.intensity = baseIntensity;
                pointLight.falloffIntensity = 0.65f;
            }
        }

        private void Update()
        {
            if (!enablePulse) return;

            // Smooth organic sine wave breathing
            float wave = Mathf.Sin((UnityEngine.Time.time * pulseSpeed) + randomPhase);
            float factor = 1f + (wave * pulseAmount);

            if (pointLight != null)
            {
                pointLight.intensity = baseIntensity * factor;
            }

            if (spriteRenderer != null)
            {
                float alpha = Mathf.Clamp01(originalSpriteColor.a * (0.85f + (wave * 0.15f)));
                spriteRenderer.color = new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, alpha);
            }
        }
    }
}
