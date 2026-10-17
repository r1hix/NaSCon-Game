using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Core;

namespace UmbraChronos.Time
{
    /// <summary>
    /// Controls dynamic post-processing screen effects for temporal mechanics and celestial danger:
    /// 1. Tape-Reel Rewind: Chromatic aberration, vignette pulsing, and desaturation.
    /// 2. Lethal Sunlight Exposure: Searing solar glare, white heat flaring on screen edges.
    /// 3. Player Death: Solar flash into monochrome with rewind encouragement.
    /// Works automatically with any URP Volume in the scene (or spawns one if missing).
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class RewindScreenFX : MonoBehaviour
    {
        [Header("Rewind Post-Processing")]
        [SerializeField] private float rewindChromaticIntensity = 0.85f;
        [SerializeField] private float rewindVignetteIntensity = 0.45f;
        [SerializeField] private float rewindTransitionSpeed = 8f;

        [Header("Sunlight Heat Flash")]
        [SerializeField] private Color normalVignetteColor = new Color(0.05f, 0.05f, 0.1f, 1f);
        [SerializeField] private Color solarBurnVignetteColor = new Color(1f, 0.85f, 0.5f, 1f);
        [SerializeField] private float maxSolarBloom = 2.2f;

        // Volume Components
        private Volume volume;
        private VolumeProfile profile;
        private ChromaticAberration chromaticAberration;
        private Vignette vignette;
        private Bloom bloom;
        private ColorAdjustments colorAdjustments;

        // Dynamic State
        private bool isRewinding;
        private float currentSunlightExposure;
        private bool isDead;
        private float targetChromatic;
        private float targetVignette;
        private float currentChromatic;
        private float currentVignette;
        private float baseBloomIntensity = 0.7f;

        private void Awake()
        {
            volume = GetComponent<Volume>();
            if (volume == null)
            {
                volume = gameObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 100f;
            }

            // Create an instance profile if not assigned
            if (volume.profile == null)
            {
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }
            profile = volume.profile;

            EnsureVolumeOverrides();
        }

        private void EnsureVolumeOverrides()
        {
            if (!profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = profile.Add<ChromaticAberration>(true);
            }
            chromaticAberration.active = true;
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0f;

            if (!profile.TryGet(out vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.color.overrideState = true;
            vignette.intensity.value = 0.25f;
            vignette.color.value = normalVignetteColor;

            if (!profile.TryGet(out bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.intensity.overrideState = true;
            baseBloomIntensity = bloom.intensity.value > 0 ? bloom.intensity.value : 0.7f;

            if (!profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 0f;
        }

        private void OnEnable()
        {
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnSunlightExposureChanged += HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnSunlightExposureChanged -= HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void Update()
        {
            float dt = UnityEngine.Time.deltaTime;

            // Compute target values based on state
            if (isDead)
            {
                targetChromatic = 0.6f;
                targetVignette = 0.55f;
            }
            else if (isRewinding)
            {
                targetChromatic = rewindChromaticIntensity;
                targetVignette = rewindVignetteIntensity;
            }
            else
            {
                // Normal or Sunlight exposure
                targetChromatic = Mathf.Lerp(0f, 0.45f, currentSunlightExposure);
                targetVignette = Mathf.Lerp(0.25f, 0.5f, currentSunlightExposure);
            }

            // Smooth interpolation
            currentChromatic = Mathf.MoveTowards(currentChromatic, targetChromatic, rewindTransitionSpeed * dt);
            currentVignette = Mathf.MoveTowards(currentVignette, targetVignette, rewindTransitionSpeed * dt);

            // Apply to URP volume
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = currentChromatic;
            }

            if (vignette != null)
            {
                vignette.intensity.value = currentVignette;
                // Tint vignette toward searing amber/solar white when exposed to sunlight
                if (currentSunlightExposure > 0.05f)
                {
                    vignette.color.value = Color.Lerp(normalVignetteColor, solarBurnVignetteColor, currentSunlightExposure);
                }
                else
                {
                    vignette.color.value = normalVignetteColor;
                }
            }

            if (bloom != null)
            {
                float targetBloom = Mathf.Lerp(baseBloomIntensity, maxSolarBloom, currentSunlightExposure);
                bloom.intensity.value = Mathf.MoveTowards(bloom.intensity.value, targetBloom, 5f * dt);
            }

            if (colorAdjustments != null)
            {
                float targetSat = isDead ? -80f : (isRewinding ? -25f : 0f);
                colorAdjustments.saturation.value = Mathf.MoveTowards(colorAdjustments.saturation.value, targetSat, 120f * dt);
            }
        }

        private void HandleRewindStateChanged(bool active)
        {
            isRewinding = active;
            if (active)
            {
                isDead = false;
            }
        }

        private void HandleSunlightExposureChanged(float fraction)
        {
            currentSunlightExposure = fraction;
        }

        private void HandlePlayerDied()
        {
            isDead = true;
        }

        private void HandlePlayerRespawned()
        {
            isDead = false;
            currentSunlightExposure = 0f;
        }
    }
}
