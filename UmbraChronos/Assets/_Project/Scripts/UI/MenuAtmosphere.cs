using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UmbraChronos.UI
{
    /// <summary>
    /// Breathes life into the title screen: depth parallax that follows the mouse,
    /// slowly drifting background silhouettes, floating dust motes, a bobbing title
    /// and a pulsing solar glow. Everything runs on unscaled time so it stays alive
    /// even when the game is paused.
    /// </summary>
    public class MenuAtmosphere : MonoBehaviour
    {
        [System.Serializable]
        public struct ParallaxLayer
        {
            [Tooltip("Element that shifts with the mouse.")]
            public RectTransform target;

            [Tooltip("How far the element travels. Positive values drift against the mouse (background); negative values follow it (foreground).")]
            public float depth;

            [Tooltip("Slow ambient sway amplitude, in canvas units.")]
            public float swayAmount;

            [Tooltip("Slow ambient sway speed.")]
            public float swaySpeed;
        }

        [System.Serializable]
        public struct AmbientMote
        {
            public RectTransform target;
            [Tooltip("Horizontal drift distance, in canvas units.")]
            public float driftAmount;
            [Tooltip("Horizontal drift speed.")]
            public float driftSpeed;
            [Tooltip("Seconds for one full vertical rise before wrapping.")]
            public float risePeriod;
            [Tooltip("Vertical travel distance before wrapping.")]
            public float riseDistance;
            [Tooltip("Phase offset so motes do not move in lockstep.")]
            public float phase;
        }

        [Header("Mouse Parallax")]
        [SerializeField] private List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();
        [Tooltip("Higher = the layers settle onto the mouse position faster.")]
        [SerializeField] private float mouseSmoothing = 4f;
        [Tooltip("Clamp applied to the normalised mouse position before it drives the layers.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float mouseRange = 0.5f;

        [Header("Ambient Drift")]
        [SerializeField] private List<AmbientMote> motes = new List<AmbientMote>();

        [Header("Title")]
        [SerializeField] private RectTransform floatingTitle;
        [SerializeField] private float titleFloatAmplitude = 7f;
        [SerializeField] private float titleFloatSpeed = 0.9f;

        [Header("Solar Glow")]
        [SerializeField] private Image glowImage;
        [SerializeField] private float glowPulseSpeed = 1.1f;
        [Range(0f, 1f)] [SerializeField] private float glowPulseAmount = 0.18f;

        private Vector2 smoothedMouse;
        private Vector2[] layerHome;
        private Vector2[] swayPhase;
        private Vector2[] moteHome;
        private Vector2 titleHome;
        private Color glowBaseColor = Color.white;

        private void Awake()
        {
            layerHome = new Vector2[parallaxLayers.Count];
            swayPhase = new Vector2[parallaxLayers.Count];
            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                RectTransform t = parallaxLayers[i].target;
                if (t == null) continue;
                layerHome[i] = t.anchoredPosition;
                swayPhase[i] = new Vector2(Random.value * 6.28318f, Random.value * 6.28318f);
            }

            moteHome = new Vector2[motes.Count];
            for (int i = 0; i < motes.Count; i++)
            {
                RectTransform t = motes[i].target;
                if (t == null) continue;
                moteHome[i] = t.anchoredPosition;
            }

            if (floatingTitle != null) titleHome = floatingTitle.anchoredPosition;
            if (glowImage != null) glowBaseColor = glowImage.color;
        }

        private void Update()
        {
            float dt = UnityEngine.Time.unscaledDeltaTime;
            float time = UnityEngine.Time.unscaledTime;

            Vector2 targetMouse = ReadNormalisedMouse();
            smoothedMouse = Vector2.Lerp(smoothedMouse, targetMouse, Mathf.Clamp01(mouseSmoothing * dt));

            UpdateParallax(dt, time);
            UpdateMotes(dt, time);
            UpdateTitle(time);
            UpdateGlow(time);
        }

        private Vector2 ReadNormalisedMouse()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

            Vector2 position;
#if ENABLE_INPUT_SYSTEM
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
            position = Input.mousePosition;
#endif

            float nx = ((position.x / Screen.width) - 0.5f) * 2f;
            float ny = ((position.y / Screen.height) - 0.5f) * 2f;
            return new Vector2(Mathf.Clamp(nx, -1f, 1f), Mathf.Clamp(ny, -1f, 1f)) * mouseRange;
        }

        private void UpdateParallax(float dt, float time)
        {
            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                ParallaxLayer layer = parallaxLayers[i];
                RectTransform t = layer.target;
                if (t == null) continue;

                Vector2 offset = -smoothedMouse * layer.depth;

                if (layer.swayAmount > 0f && layer.swaySpeed > 0f)
                {
                    offset += new Vector2(
                        Mathf.Sin((time * layer.swaySpeed) + swayPhase[i].x) * layer.swayAmount,
                        Mathf.Sin((time * layer.swaySpeed * 0.7f) + swayPhase[i].y) * layer.swayAmount * 0.5f);
                }

                t.anchoredPosition = layerHome[i] + offset;
            }
        }

        private void UpdateMotes(float dt, float time)
        {
            for (int i = 0; i < motes.Count; i++)
            {
                AmbientMote mote = motes[i];
                RectTransform t = mote.target;
                if (t == null) continue;

                float drift = Mathf.Sin((time * mote.driftSpeed) + mote.phase) * mote.driftAmount;

                float rise = 0f;
                if (mote.risePeriod > 0.01f)
                {
                    // phase is expressed in radians for the drift sine; convert it to a 0..1 offset here.
                    float progress = Mathf.Repeat((time / mote.risePeriod) + (mote.phase / 6.28318f), 1f);
                    rise = progress * mote.riseDistance;
                }

                t.anchoredPosition = moteHome[i] + new Vector2(drift, rise);
            }
        }

        private void UpdateTitle(float time)
        {
            if (floatingTitle == null) return;
            float bob = Mathf.Sin(time * titleFloatSpeed) * titleFloatAmplitude;
            floatingTitle.anchoredPosition = new Vector2(titleHome.x, titleHome.y + bob);
        }

        private void UpdateGlow(float time)
        {
            if (glowImage == null) return;
            float pulse = 1f + (Mathf.Sin(time * glowPulseSpeed) * glowPulseAmount);
            Color c = glowBaseColor;
            c.a = Mathf.Clamp01(glowBaseColor.a * pulse);
            glowImage.color = c;

            float scale = 1f + (Mathf.Sin(time * glowPulseSpeed * 0.8f) * glowPulseAmount * 0.35f);
            glowImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
