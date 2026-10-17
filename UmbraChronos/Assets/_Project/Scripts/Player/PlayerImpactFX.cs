using UnityEngine;
using UmbraChronos.Core;

namespace UmbraChronos.Player
{
    /// <summary>
    /// Spawns visceral obsidian dust puffs and ground impact particles
    /// when the player jumps, lands, or vaporizes.
    /// Provides punchy, tactile physical weight to the character's movement.
    /// </summary>
    public class PlayerImpactFX : MonoBehaviour
    {
        [Header("Dust Colors")]
        [SerializeField] private Color obsidianDustColor = new Color(0.08f, 0.08f, 0.12f, 0.85f); // #14141F dark obsidian
        [SerializeField] private Color echoBurstColor = new Color(0.22f, 0.74f, 0.97f, 0.85f);    // #38BDF8 radiant cyan

        private ParticleSystem groundDustPS;
        private ParticleSystem.EmitParams emitParams;

        private void Awake()
        {
            CreateParticleSystem();
        }

        private void OnEnable()
        {
            TimeEvents.OnPlayerJumped += HandleJump;
            TimeEvents.OnPlayerLanded += HandleLanded;
            TimeEvents.OnEchoCreated += HandleEchoCreated;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            TimeEvents.OnPlayerJumped -= HandleJump;
            TimeEvents.OnPlayerLanded -= HandleLanded;
            TimeEvents.OnEchoCreated -= HandleEchoCreated;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void CreateParticleSystem()
        {
            GameObject psObj = new GameObject("Player_Impact_Dust_Particles");
            psObj.transform.SetParent(transform, false);
            psObj.transform.localPosition = new Vector3(0f, -0.75f, 0f); // Feet level

            groundDustPS = psObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = psObj.GetComponent<ParticleSystemRenderer>();

            Shader unlitShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            Material psMat = unlitShader != null ? new Material(unlitShader) : null;
            if (psMat != null)
            {
                psRenderer.material = psMat;
            }
            psRenderer.sortingOrder = 9; // Right behind player body (10), in front of platforms (0)

            // Main module
            var main = groundDustPS.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = obsidianDustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            // Emission module (bursts only)
            var emission = groundDustPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            // Shape module
            var shape = groundDustPS.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.5f, 0.1f, 0.1f);

            // Size over lifetime (puffs up slightly then fades out)
            var sizeLife = groundDustPS.sizeOverLifetime;
            sizeLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.2f, 1.2f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime (fades out alpha)
            var colorLife = groundDustPS.colorOverLifetime;
            colorLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colorLife.color = grad;

            emitParams = new ParticleSystem.EmitParams();
        }

        private void HandleJump()
        {
            if (groundDustPS == null) return;

            // Emit a modest puff of dust on jump lift-off
            Vector3 footPos = transform.position + new Vector3(0f, -0.72f, 0f);
            for (int i = 0; i < 5; i++)
            {
                emitParams.position = footPos + new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
                emitParams.velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.2f, 0.9f), 0f);
                emitParams.startColor = obsidianDustColor;
                emitParams.startSize = Random.Range(0.08f, 0.16f);
                emitParams.startLifetime = Random.Range(0.2f, 0.35f);
                groundDustPS.Emit(emitParams, 1);
            }
        }

        private void HandleLanded(float fallSpeed)
        {
            if (groundDustPS == null || fallSpeed < 2.0f) return;

            Vector3 footPos = transform.position + new Vector3(0f, -0.72f, 0f);
            int count = Mathf.Clamp(Mathf.RoundToInt(fallSpeed * 2.2f), 6, 22);
            float spreadVelocity = Mathf.Clamp(fallSpeed * 0.45f, 1.2f, 4.0f);

            // Spray dust left and right horizontally along the ground
            for (int i = 0; i < count; i++)
            {
                float dir = (i % 2 == 0) ? 1f : -1f;
                float speed = Random.Range(spreadVelocity * 0.4f, spreadVelocity);
                emitParams.position = footPos + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0f, 0.05f), 0f);
                emitParams.velocity = new Vector3(dir * speed, Random.Range(0.1f, 0.8f), 0f);
                emitParams.startColor = obsidianDustColor;
                emitParams.startSize = Random.Range(0.10f, 0.24f);
                emitParams.startLifetime = Random.Range(0.25f, 0.45f);
                groundDustPS.Emit(emitParams, 1);
            }
        }

        private void HandleEchoCreated()
        {
            if (groundDustPS == null) return;

            // Cyan ring puff expanding at feet when phantom shadow clone is anchored
            Vector3 footPos = transform.position + new Vector3(0f, -0.6f, 0f);
            for (int i = 0; i < 14; i++)
            {
                float angle = (float)i / 14f * Mathf.PI * 2f;
                Vector3 vel = new Vector3(Mathf.Cos(angle) * 2.5f, Mathf.Sin(angle) * 1.5f, 0f);
                emitParams.position = footPos;
                emitParams.velocity = vel;
                emitParams.startColor = echoBurstColor;
                emitParams.startSize = Random.Range(0.10f, 0.18f);
                emitParams.startLifetime = 0.3f;
                groundDustPS.Emit(emitParams, 1);
            }
        }

        private void HandlePlayerDied()
        {
            if (groundDustPS == null) return;

            // Violent vaporization burst: obsidian ash exploding outward
            Vector3 center = transform.position;
            for (int i = 0; i < 25; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2.0f, 5.5f);
                Vector3 vel = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
                emitParams.position = center + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.4f, 0.4f), 0f);
                emitParams.velocity = vel;
                emitParams.startColor = (i % 3 == 0) ? new Color(1f, 0.6f, 0.2f, 0.9f) : obsidianDustColor;
                emitParams.startSize = Random.Range(0.12f, 0.28f);
                emitParams.startLifetime = Random.Range(0.35f, 0.6f);
                groundDustPS.Emit(emitParams, 1);
            }
        }
    }
}
