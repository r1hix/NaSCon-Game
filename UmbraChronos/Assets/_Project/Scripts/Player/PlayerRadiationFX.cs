using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Player
{
    /// <summary>
    /// Emits intense solar radiation sizzle sparks and vaporization embers
    /// when the player is caught in lethal celestial sunlight.
    /// Provides punchy, visceral visual feedback for radiation exposure.
    /// </summary>
    public class PlayerRadiationFX : MonoBehaviour
    {
        [Header("Radiation Particle Settings")]
        [Tooltip("Maximum particle emission rate when sunlight exposure is 100%.")]
        [SerializeField] private float maxEmissionRate = 50f;

        [SerializeField] private Color sparkColor = new Color(1f, 0.94f, 0.5f, 0.9f);
        [SerializeField] private Color emberColor = new Color(1f, 0.45f, 0.1f, 0.8f);

        [Tooltip("Sprite for sparks.")]
        [SerializeField] private Sprite sparkSprite;

        private ParticleSystem sizzleParticles;
        private ParticleSystem.EmissionModule emissionModule;
        private float currentExposureFraction;

        private void Awake()
        {
            LoadSprite();
            CreateParticleSystem();
        }

        private void OnEnable()
        {
            TimeEvents.OnSunlightExposureChanged += HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPlayerRespawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunlightExposureChanged -= HandleSunlightExposureChanged;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPlayerRespawned -= HandlePlayerRespawned;
        }

        private void LoadSprite()
        {
#if UNITY_EDITOR
            if (sparkSprite == null)
            {
                sparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Environment/Sprite_Solar_Mote.png");
            }
#endif
        }

        private void CreateParticleSystem()
        {
            GameObject psObj = new GameObject("Player_Radiation_Sizzle_Particles");
            psObj.transform.SetParent(this.transform, false);
            psObj.transform.localPosition = Vector3.zero;

            sizzleParticles = psObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = psObj.GetComponent<ParticleSystemRenderer>();

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material psMat = unlitShader != null ? new Material(unlitShader) : null;
            if (psMat != null)
            {
                if (sparkSprite != null) psMat.mainTexture = sparkSprite.texture;
                psRenderer.material = psMat;
            }
            psRenderer.sortingOrder = 12; // In front of player sprite (order 10)

            // Main Module
            var main = sizzleParticles.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            main.startColor = sparkColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.playOnAwake = true;

            // Emission Module (controlled dynamically)
            emissionModule = sizzleParticles.emission;
            emissionModule.rateOverTime = 0f;

            // Shape Module: capsule around player body
            var shape = sizzleParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.6f, 1.4f, 0.1f);

            // Velocity over lifetime (upward rising heat with gentle drift)
            var vel = sizzleParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Color Over Lifetime: White-hot gold -> Amber -> Fade
            var col = sizzleParticles.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(sparkColor, 0.4f), new GradientColorKey(emberColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            // Size Over Lifetime: Snappy shrink
            var size = sizzleParticles.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(0.7f, 0.6f);
            curve.AddKey(1f, 0f);
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);

            sizzleParticles.Play();
        }

        private void HandleSunlightExposureChanged(float fraction)
        {
            currentExposureFraction = fraction;
            if (sizzleParticles == null) return;

            if (fraction > 0.05f)
            {
                // Quadratic ramp for intense sizzling as danger peaks
                float rate = Mathf.Pow(fraction, 1.5f) * maxEmissionRate;
                emissionModule.rateOverTime = rate;
            }
            else
            {
                emissionModule.rateOverTime = 0f;
            }
        }

        private void HandlePlayerDied()
        {
            if (sizzleParticles == null) return;

            // Big burst on vaporization
            emissionModule.rateOverTime = 0f;
            sizzleParticles.Emit(45);
        }

        private void HandlePlayerRespawned()
        {
            if (sizzleParticles == null) return;
            sizzleParticles.Clear();
            emissionModule.rateOverTime = 0f;
        }
    }
}
