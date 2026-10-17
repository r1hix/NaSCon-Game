using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UmbraChronos.Core;

namespace UmbraChronos.Time
{
    /// <summary>
    /// Global manager coordinating the Tape-Reel Rewind and Umbral Echo mechanics.
    /// Drives rewind state, energy/stamina meter, audio pitch-drop, and post-processing screen effects.
    /// </summary>
    public class RewindManager : MonoBehaviour
    {
        public static RewindManager Instance { get; private set; }

        [Header("Rewind Energy")]
        [SerializeField] private float maxRewindEnergy = 5.0f;
        [SerializeField] private float energyRechargeRate = 1.25f;
        [SerializeField] private float energyDrainRate = 1.0f;

        [Header("Umbral Echo")]
        [SerializeField] private GameObject echoPrefab;
        [SerializeField] private TimeBody playerTimeBody;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource rewindAudioSource;
        [SerializeField] private AudioSource backgroundMusicSource;

        [Header("Visual Effects (Optional)")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private float targetChromaticAberration = 0.85f;
        [SerializeField] private float targetVignette = 0.45f;

        private float currentEnergy;
        private bool isRewindActive;
        private ChromaticAberration chromaticAberration;
        private Vignette vignette;
        private float originalBgmPitch = 1.0f;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxRewindEnergy;
        public float EnergyPercentage => currentEnergy / maxRewindEnergy;
        public bool IsRewinding => isRewindActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentEnergy = maxRewindEnergy;

            // Cache Post-Processing components
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out chromaticAberration);
                postProcessVolume.profile.TryGet(out vignette);
            }

            if (backgroundMusicSource != null)
            {
                originalBgmPitch = backgroundMusicSource.pitch;
            }
        }

        private void Start()
        {
            if (playerTimeBody == null)
            {
                // Auto-locate player TimeBody if not manually assigned
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTimeBody = player.GetComponent<TimeBody>();
                }
            }
        }

        private void Update()
        {
            bool rewindInput = CheckRewindInput();
            bool echoInput = CheckEchoInput();

            if (rewindInput && currentEnergy > 0.1f)
            {
                if (!isRewindActive)
                {
                    StartRewind();
                }

                currentEnergy = Mathf.Max(0f, currentEnergy - energyDrainRate * UnityEngine.Time.deltaTime);

                if (currentEnergy <= 0f)
                {
                    StopRewind();
                }
            }
            else
            {
                if (isRewindActive)
                {
                    StopRewind();
                }

                // Recharge energy when not rewinding
                currentEnergy = Mathf.Min(maxRewindEnergy, currentEnergy + energyRechargeRate * UnityEngine.Time.deltaTime);
            }

            TimeEvents.OnRewindEnergyChanged?.Invoke(currentEnergy, maxRewindEnergy);

            // Umbral Echo Trigger
            if (echoInput)
            {
                TriggerUmbralEcho();
            }

            UpdateEffects();
        }

        private bool CheckRewindInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.isPressed || keyboard.leftShiftKey.isPressed) return true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftTrigger.isPressed || gamepad.buttonWest.isPressed) return true;
            }
            return false;
#else
            return Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private bool CheckEchoInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.fKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame) return true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonNorth.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame) return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.C);
#endif
        }

        private void StartRewind()
        {
            isRewindActive = true;
            TimeEvents.OnRewindStateChanged?.Invoke(true);

            if (rewindAudioSource != null && !rewindAudioSource.isPlaying)
            {
                rewindAudioSource.Play();
            }

            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.pitch = -0.75f; // reverse pitch-drop sensation
            }
        }

        private void StopRewind()
        {
            isRewindActive = false;
            TimeEvents.OnRewindStateChanged?.Invoke(false);

            if (rewindAudioSource != null && rewindAudioSource.isPlaying)
            {
                rewindAudioSource.Stop();
            }

            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.pitch = originalBgmPitch;
            }
        }

        /// <summary>
        /// Spawns an Umbral Echo from the player's recorded trajectory buffer.
        /// </summary>
        public void TriggerUmbralEcho()
        {
            if (playerTimeBody == null || echoPrefab == null) return;

            List<TimeBody.TimeSnapshot> history = playerTimeBody.GetEchoTrajectory();
            if (history == null || history.Count < 5) return;

            // Spawn directly at the starting position of the trajectory
            Vector3 spawnPos = new Vector3(history[0].position.x, history[0].position.y, 0f);
            GameObject echoObj = Instantiate(echoPrefab, spawnPos, Quaternion.Euler(0f, 0f, history[0].rotation));
            var echoComponent = echoObj.GetComponent<UmbralEcho>();
            if (echoComponent != null)
            {
                echoComponent.Initialize(history);
            }
        }

        private void UpdateEffects()
        {
            float targetWeight = isRewindActive ? 1.0f : 0.0f;

            if (postProcessVolume != null)
            {
                postProcessVolume.weight = Mathf.MoveTowards(postProcessVolume.weight, targetWeight, 4f * UnityEngine.Time.deltaTime);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.MoveTowards(
                    chromaticAberration.intensity.value, 
                    isRewindActive ? targetChromaticAberration : 0f, 
                    4f * UnityEngine.Time.deltaTime);
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.MoveTowards(
                    vignette.intensity.value, 
                    isRewindActive ? targetVignette : 0.2f, 
                    2f * UnityEngine.Time.deltaTime);
            }
        }
    }
}
