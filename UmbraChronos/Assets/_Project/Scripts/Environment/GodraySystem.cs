using System.Collections.Generic;
using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Environment
{
    /// <summary>
    /// Projects atmospheric volumetric godrays and floating celestial dust motes
    /// from the celestial sun source. Aligns seamlessly with the sun orbit angle,
    /// dynamically raycasts to terminate on roofs/monoliths, and leaves covered areas
    /// shaded while casting radiant light shafts across open sky/chasms.
    /// </summary>
    public class GodraySystem : MonoBehaviour
    {
        [Header("Sun Source")]
        [Tooltip("Transform of the celestial sun. If null, locates SunDialController automatically.")]
        [SerializeField] private Transform sunTransform;

        [Header("Godray Beams")]
        [Tooltip("Feathered light shaft sprite.")]
        [SerializeField] private Sprite godraySprite;

        [Tooltip("Number of light shafts radiating across the chamber.")]
        [SerializeField] [Range(3, 8)] private int beamCount = 6;

        [Tooltip("Maximum reach of light shafts across open voids.")]
        [SerializeField] private float maxBeamLength = 28f;

        [Tooltip("Base width of each light shaft.")]
        [SerializeField] private float beamWidth = 1.4f;

        [Tooltip("Angular spread across the beam fan.")]
        [SerializeField] private float totalSpreadAngle = 38f;

        [Tooltip("Base color and opacity of the celestial light shafts.")]
        [SerializeField] private Color beamColor = new Color(1f, 0.94f, 0.72f, 0.18f);

        [Header("Occlusion & Shadow Blocking")]
        [Tooltip("Layers that block light beams (roofs, monoliths, ground).")]
        [SerializeField] private LayerMask shadowBlockerMask = ~0;

        [Header("Atmospheric Solar Dust (Particle System)")]
        [Tooltip("Enable floating solar dust motes drifting along sunbeam angles.")]
        [SerializeField] private bool enableParticles = true;

        [Tooltip("Sprite for solar dust particle motes.")]
        [SerializeField] private Sprite moteSprite;

        [SerializeField] private Color particleColor = new Color(1f, 0.94f, 0.65f, 0.65f);

        // Internal beam representation
        private Transform beamPivot;
        private List<SpriteRenderer> beamRenderers = new List<SpriteRenderer>();
        private float[] beamSpeeds;
        private float[] beamPhases;
        private float[] beamWidthOffsets;
        private float[] currentBeamLengths;
        private float currentSunAngle = 50f;
        private float targetRotationZ = 0f;

        // Particle System
        private ParticleSystem solarDustParticles;

        private const float BaseSpriteHeightUnits = 16f; // 1024px / 64 PPU = 16 units

        private void Awake()
        {
            LocateSun();
            LoadSprites();
            CreateBeamHierarchy();
            if (enableParticles)
            {
                CreateSolarDustParticles();
            }
        }

        private void OnEnable()
        {
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
        }

        private void OnDisable()
        {
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
        }

        private void LocateSun()
        {
            if (sunTransform != null) return;

            SunDialController dial = FindAnyObjectByType<SunDialController>();
            if (dial != null)
            {
                sunTransform = dial.transform;
            }
            else
            {
                GameObject sunObj = GameObject.Find("Celestial_Sun") ?? GameObject.Find("Sun");
                if (sunObj != null) sunTransform = sunObj.transform;
            }
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            if (godraySprite == null)
            {
                godraySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Environment/Sprite_Godray_Shaft.png");
            }
            if (moteSprite == null)
            {
                moteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Environment/Sprite_Solar_Mote.png");
            }
#endif
        }

        private void CreateBeamHierarchy()
        {
            GameObject pivotObj = new GameObject("Godray_Pivot");
            pivotObj.transform.SetParent(this.transform);
            beamPivot = pivotObj.transform;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material beamMat = unlitShader != null ? new Material(unlitShader) : null;

            beamRenderers.Clear();
            beamSpeeds = new float[beamCount];
            beamPhases = new float[beamCount];
            beamWidthOffsets = new float[beamCount];
            currentBeamLengths = new float[beamCount];

            float stepAngle = beamCount > 1 ? totalSpreadAngle / (beamCount - 1) : 0f;
            float startAngle = -totalSpreadAngle * 0.5f;

            for (int i = 0; i < beamCount; i++)
            {
                GameObject beamObj = new GameObject($"Godray_Shaft_{i}");
                beamObj.transform.SetParent(beamPivot, false);

                float offsetAngle = startAngle + (i * stepAngle);
                beamObj.transform.localRotation = Quaternion.Euler(0f, 0f, offsetAngle);

                SpriteRenderer sr = beamObj.AddComponent<SpriteRenderer>();
                if (godraySprite != null) sr.sprite = godraySprite;
                if (beamMat != null) sr.material = beamMat;
                sr.color = beamColor;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -5; // Behind platforms (0) and player (10), in front of sky canvas (-20)

                float widthVariance = 0.85f + (Mathf.Sin(i * 1.7f) * 0.25f);
                beamObj.transform.localScale = new Vector3(beamWidth * widthVariance, maxBeamLength / BaseSpriteHeightUnits, 1f);

                beamRenderers.Add(sr);
                beamSpeeds[i] = 0.9f + (i * 0.35f);
                beamPhases[i] = i * 1.25f;
                beamWidthOffsets[i] = widthVariance;
                currentBeamLengths[i] = maxBeamLength;
            }

            UpdateSunAngle(currentSunAngle, true);
        }

        private void CreateSolarDustParticles()
        {
            GameObject psObj = new GameObject("Solar_Dust_Particle_System");
            psObj.transform.SetParent(beamPivot, false);
            psObj.transform.localPosition = new Vector3(0f, -4f, 0f);

            solarDustParticles = psObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = psObj.GetComponent<ParticleSystemRenderer>();

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material psMat = unlitShader != null ? new Material(unlitShader) : null;
            if (psMat != null)
            {
                if (moteSprite != null) psMat.mainTexture = moteSprite.texture;
                psRenderer.material = psMat;
            }
            psRenderer.sortingOrder = -4; // In front of godrays (-5), behind platforms (0)

            // Main Module
            var main = solarDustParticles.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            main.playOnAwake = true;

            // Emission Module
            var emission = solarDustParticles.emission;
            emission.rateOverTime = 8f;

            // Shape Module: Box across upper light zone
            var shape = solarDustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(beamWidth * beamCount * 0.9f, 2f, 1f);

            // Color Over Lifetime
            var col = solarDustParticles.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(particleColor, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(particleColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            // 2D Collision Module (stops motes from penetrating roofs into covered rooms!)
            var collision = solarDustParticles.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision2D;
            collision.lifetimeLoss = 1f; // Dies on contact with roofs/monoliths
            collision.collidesWith = shadowBlockerMask;

            // Noise / Gentle Wobble
            var noise = solarDustParticles.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.4f;

            solarDustParticles.Play();
        }

        private void Update()
        {
            if (sunTransform != null && beamPivot != null)
            {
                beamPivot.position = sunTransform.position;
            }

            // Smooth rotation towards current celestial light vector
            if (beamPivot != null)
            {
                float currentZ = beamPivot.eulerAngles.z;
                float newZ = Mathf.MoveTowardsAngle(currentZ, targetRotationZ, 120f * UnityEngine.Time.deltaTime);
                beamPivot.rotation = Quaternion.Euler(0f, 0f, newZ);
            }

            // Dynamic Occlusion Raycasts + Subtle Shimmering
            Vector2 sunPos = beamPivot != null ? (Vector2)beamPivot.position : Vector2.zero;
            float time = UnityEngine.Time.time;

            for (int i = 0; i < beamRenderers.Count; i++)
            {
                SpriteRenderer sr = beamRenderers[i];
                if (sr == null) continue;

                Transform beamT = sr.transform;
                // Beam points down along -beamT.up
                Vector2 beamDir = -beamT.up;

                // Raycast to check for roofs, monoliths, or platforms (filtering out triggers)
                RaycastHit2D[] hits = Physics2D.RaycastAll(sunPos, beamDir, maxBeamLength, shadowBlockerMask);
                float targetLength = maxBeamLength;

                for (int h = 0; h < hits.Length; h++)
                {
                    var col = hits[h].collider;
                    if (col == null || col.isTrigger) continue;
                    if (col.CompareTag("Player")) continue;
                    if (col.transform.IsChildOf(transform)) continue;

                    targetLength = hits[h].distance;
                    break;
                }

                // Smooth length adjustment to prevent snapping
                currentBeamLengths[i] = Mathf.MoveTowards(currentBeamLengths[i], targetLength, 45f * UnityEngine.Time.deltaTime);

                // Update scale: X = width, Y = length (divided by sprite base height)
                float scaleY = Mathf.Max(0.1f, currentBeamLengths[i] / BaseSpriteHeightUnits);
                beamT.localScale = new Vector3(beamWidth * beamWidthOffsets[i], scaleY, 1f);

                // Subtle luminous shimmer
                float pulse = Mathf.Sin((time * beamSpeeds[i]) + beamPhases[i]);
                float alphaMult = 1f + (pulse * 0.2f);

                Color c = beamColor;
                c.a = Mathf.Clamp01(beamColor.a * alphaMult);
                sr.color = c;
            }
        }

        private void HandleSunAngleChanged(float angle)
        {
            UpdateSunAngle(angle, false);
        }

        private void UpdateSunAngle(float angle, bool snap)
        {
            currentSunAngle = angle;
            // Angle theta: 90 deg = overhead (sun at top, light points straight down, Z = 0)
            // 20 deg = dawn (sun at top-left, light points down-right, Z = +70 deg)
            // 160 deg = dusk (sun at top-right, light points down-left, Z = -70 deg)
            targetRotationZ = 90f - angle;

            if (snap && beamPivot != null)
            {
                beamPivot.rotation = Quaternion.Euler(0f, 0f, targetRotationZ);
            }
        }
    }
}
