using UnityEngine;
using UnityEngine.SceneManagement;
using UmbraChronos.Core;

namespace UmbraChronos.Core
{
    /// <summary>
    /// Central audio engine for Umbra Chronos: Echoes of the Sundial.
    /// Manages:
    /// - Continuous ambient clockwork drone (BGM) with dynamic Time Rewind pitch shift and tape flutter.
    /// - Mechanical sundial ratcheting audio (tick SFX) throttled to degrees of celestial rotation.
    /// - Looping VHS tape rewind whir with volume crossfades.
    /// - Event-driven SFX (Echo spawn chime, Echo dissolve, solar vaporization, gate slide, plate click, jump, land).
    /// - Decoupled event bus hooked directly to TimeEvents.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource rewindWhirSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music Tracks")]
        [SerializeField] private AudioClip bgmClockworkAbyss;

        [Header("Sound Effects (SFX)")]
        [SerializeField] private AudioClip sfxTick;
        [SerializeField] private AudioClip sfxTapeRewind;
        [SerializeField] private AudioClip sfxEchoSpawn;
        [SerializeField] private AudioClip sfxEchoDissolve;
        [SerializeField] private AudioClip sfxPlateClick;
        [SerializeField] private AudioClip sfxGateOpen;
        [SerializeField] private AudioClip sfxVaporize;
        [SerializeField] private AudioClip sfxJump;
        [SerializeField] private AudioClip sfxLand;
        [SerializeField] private AudioClip sfxPuzzleSolved;

        [Header("Volume Balances")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float rewindWhirVolume = 0.70f;

        [Header("Rewind Pitch Shift Tuning")]
        [SerializeField] private float normalPitch = 1.0f;
        [SerializeField] private float rewindPitch = 0.5f;
        [SerializeField] private float pitchLerpSpeed = 6.0f;
        [SerializeField] private float tapeWobbleAmount = 0.04f;
        [SerializeField] private float tapeWobbleSpeed = 14.0f;

        // Internal states
        private bool isRewinding;
        private float currentPitch = 1.0f;
        private float lastTickAngle;
        private float lastTickTime;
        private float rewindWhirTargetVol = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = Object.FindAnyObjectByType<AudioManager>();
                if (existing == null)
                {
                    var prefab = Resources.Load<GameObject>("AudioManager");
                    if (prefab != null)
                    {
                        var go = Object.Instantiate(prefab);
                        go.name = "AudioManager";
                    }
                    else
                    {
                        var go = new GameObject("AudioManager");
                        go.AddComponent<AudioManager>();
                    }
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // If existing instance has no clips but THIS instance has clips, swap it out cleanly!
                if (Instance.bgmClockworkAbyss == null && this.bgmClockworkAbyss != null)
                {
                    Destroy(Instance.gameObject);
                    Instance = this;
                    DontDestroyOnLoad(gameObject);
                    SetupAudioSources();
                    StartBgm();
                    return;
                }

                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
            LoadAudioClips();
            StartBgm();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TimeEvents.OnSunAngleChanged += HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged += HandleRewindStateChanged;
            TimeEvents.OnEchoCreated += HandleEchoCreated;
            TimeEvents.OnEchoDissolved += HandleEchoDissolved;
            TimeEvents.OnPlayerDied += HandlePlayerDied;
            TimeEvents.OnPuzzleSolved += HandlePuzzleSolved;
            TimeEvents.OnPlayerJumped += HandlePlayerJumped;
            TimeEvents.OnPlayerLanded += HandlePlayerLanded;
            TimeEvents.OnPressurePlateChanged += HandlePressurePlateChanged;
            TimeEvents.OnGateStateChanged += HandleGateStateChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            TimeEvents.OnSunAngleChanged -= HandleSunAngleChanged;
            TimeEvents.OnRewindStateChanged -= HandleRewindStateChanged;
            TimeEvents.OnEchoCreated -= HandleEchoCreated;
            TimeEvents.OnEchoDissolved -= HandleEchoDissolved;
            TimeEvents.OnPlayerDied -= HandlePlayerDied;
            TimeEvents.OnPuzzleSolved -= HandlePuzzleSolved;
            TimeEvents.OnPlayerJumped -= HandlePlayerJumped;
            TimeEvents.OnPlayerLanded -= HandlePlayerLanded;
            TimeEvents.OnPressurePlateChanged -= HandlePressurePlateChanged;
            TimeEvents.OnGateStateChanged -= HandleGateStateChanged;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
            {
                if (bgmSource != null && bgmSource.isPlaying)
                    bgmSource.Stop();
            }
            else
            {
                StartBgm();
            }
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0f; // 2D flat audio
                bgmSource.volume = musicVolume * masterVolume;
            }

            if (rewindWhirSource == null)
            {
                rewindWhirSource = gameObject.AddComponent<AudioSource>();
                rewindWhirSource.playOnAwake = false;
                rewindWhirSource.loop = true;
                rewindWhirSource.spatialBlend = 0f;
                rewindWhirSource.volume = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = sfxVolume * masterVolume;
            }
        }

        private void LoadAudioClips()
        {
#if UNITY_EDITOR
            string sfxPath = "Assets/_Project/Audio/SFX";
            string musicPath = "Assets/_Project/Audio/Music";

            if (bgmClockworkAbyss == null)
                bgmClockworkAbyss = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{musicPath}/bgm_clockwork_abyss.wav");

            if (sfxTick == null)
                sfxTick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_tick.wav");

            if (sfxTapeRewind == null)
                sfxTapeRewind = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_tape_rewind.wav");

            if (sfxEchoSpawn == null)
                sfxEchoSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_echo_spawn.wav");

            if (sfxEchoDissolve == null)
                sfxEchoDissolve = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_echo_dissolve.wav");

            if (sfxPlateClick == null)
                sfxPlateClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_plate_click.wav");

            if (sfxGateOpen == null)
                sfxGateOpen = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_gate_open.wav");

            if (sfxVaporize == null)
                sfxVaporize = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_vaporize.wav");

            if (sfxJump == null)
                sfxJump = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_jump.wav");

            if (sfxLand == null)
                sfxLand = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_land.wav");

            if (sfxPuzzleSolved == null)
                sfxPuzzleSolved = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{sfxPath}/sfx_puzzle_solved.wav");
#endif
        }

        private void StartBgm()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (bgmSource != null && bgmClockworkAbyss != null)
            {
                if (!bgmSource.isPlaying || bgmSource.clip != bgmClockworkAbyss)
                {
                    bgmSource.clip = bgmClockworkAbyss;
                    bgmSource.volume = musicVolume * masterVolume;
                    bgmSource.Play();
                }
            }

            if (rewindWhirSource != null && sfxTapeRewind != null)
            {
                if (!rewindWhirSource.isPlaying)
                {
                    rewindWhirSource.clip = sfxTapeRewind;
                    rewindWhirSource.volume = 0f;
                    rewindWhirSource.Play();
                }
            }
        }

        private void Update()
        {
            float dt = UnityEngine.Time.unscaledDeltaTime;

            // 1. Dynamic Rewind Pitch Shift with authentic tape wobble
            float targetPitch = isRewinding ? rewindPitch : normalPitch;
            currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, pitchLerpSpeed * dt);

            float flutter = isRewinding ? (Mathf.Sin(UnityEngine.Time.unscaledTime * tapeWobbleSpeed) * tapeWobbleAmount) : 0f;
            if (bgmSource != null)
            {
                bgmSource.pitch = Mathf.Clamp(currentPitch + flutter, 0.2f, 1.5f);
                bgmSource.volume = musicVolume * masterVolume;
            }

            // 2. Looping rewind tape whir volume crossfade
            if (rewindWhirSource != null)
            {
                rewindWhirSource.volume = Mathf.MoveTowards(rewindWhirSource.volume, rewindWhirTargetVol, 5f * dt);
                if (isRewinding)
                {
                    // Subtle flutter on the rewind whir motor itself
                    rewindWhirSource.pitch = 1.0f + (Mathf.Sin(UnityEngine.Time.unscaledTime * 18f) * 0.05f);
                }
            }
        }

        #region Event Listeners

        private void HandleSunAngleChanged(float angle)
        {
            // Ratchet tick on every 4 degrees of orbital rotation, throttled to max 14 clicks/second
            float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(lastTickAngle, angle));
            float now = UnityEngine.Time.unscaledTime;
            if (deltaAngle >= 4.0f && (now - lastTickTime) >= 0.07f)
            {
                lastTickAngle = angle;
                lastTickTime = now;
                PlaySFX(sfxTick, 0.45f, Random.Range(0.92f, 1.08f));
            }
        }

        private void HandleRewindStateChanged(bool active)
        {
            isRewinding = active;
            rewindWhirTargetVol = active ? (rewindWhirVolume * masterVolume) : 0f;
        }

        private void HandleEchoCreated()
        {
            PlaySFX(sfxEchoSpawn, 0.85f, 1.0f);
        }

        private void HandleEchoDissolved()
        {
            PlaySFX(sfxEchoDissolve, 0.60f, 1.0f);
        }

        private void HandlePlayerDied()
        {
            PlaySFX(sfxVaporize, 0.95f, Random.Range(0.95f, 1.05f));
        }

        private void HandlePuzzleSolved()
        {
            PlaySFX(sfxPuzzleSolved, 1.0f, 1.0f);
        }

        private void HandlePlayerJumped()
        {
            PlaySFX(sfxJump, 0.50f, Random.Range(0.96f, 1.04f));
        }

        private void HandlePlayerLanded(float fallSpeed)
        {
            float impact = Mathf.Clamp01(fallSpeed / 12f);
            if (impact > 0.15f)
            {
                PlaySFX(sfxLand, impact * 0.65f, Random.Range(0.94f, 1.04f));
            }
        }

        private void HandlePressurePlateChanged(bool pressed)
        {
            if (pressed)
            {
                PlaySFX(sfxPlateClick, 0.70f, Random.Range(0.96f, 1.04f));
            }
        }

        private void HandleGateStateChanged(bool opened)
        {
            PlaySFX(sfxGateOpen, 0.70f, opened ? 1.0f : 0.88f);
        }

        #endregion

        /// <summary>
        /// Plays an audio clip with custom volume multiplier and pitch shift.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sfxSource == null) return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// Static convenience method to play an SFX anywhere in code.
        /// </summary>
        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null)
            {
                Instance.PlaySFX(clip, volume, pitch);
            }
        }
    }
}
