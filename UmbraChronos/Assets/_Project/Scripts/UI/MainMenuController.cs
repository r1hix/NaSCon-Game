using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UmbraChronos.UI
{
    /// <summary>
    /// Drives the opening title screen: Play / Settings / Credits / Quit.
    /// Panels fade and slide in Hollow Knight fashion, and starting the game
    /// runs through a short fade-to-black transition.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Starting The Game")]
        [Tooltip("Scene name loaded by the Play button. Used first if it is in the build settings.")]
        [SerializeField] private string gameplaySceneName = "Level_01";

        [Tooltip("Build index used if gameplaySceneName is missing from the build settings.")]
        [SerializeField] private int gameplaySceneIndex = 1;

        [Header("Panels")]
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup settingsGroup;
        [SerializeField] private CanvasGroup creditsGroup;
        [SerializeField] private RectTransform mainMenuPanel;
        [SerializeField] private RectTransform settingsPanel;
        [SerializeField] private RectTransform creditsPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Panel")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button settingsBackButton;

        [Header("Credits Panel")]
        [SerializeField] private Button creditsBackButton;

        [Header("Transitions")]
        [Tooltip("Fade/slide duration for a panel reveal, in seconds.")]
        [SerializeField] private float panelFadeDuration = 0.22f;

        [Tooltip("How far a panel slides while revealing, in canvas units.")]
        [SerializeField] private float panelSlideDistance = 48f;

        [Tooltip("Full-screen black image used for the fade-out when Play is pressed.")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float startFadeDuration = 0.7f;

        private const string MasterVolumeKey = "umbra_master_volume";
        private const string FullscreenKey = "umbra_fullscreen";

        private sealed class PanelState
        {
            public CanvasGroup group;
            public RectTransform rect;
            public Vector2 home;
            public float direction;
            public bool visible;
            public float t;
        }

        private PanelState mainState;
        private PanelState settingsState;
        private PanelState creditsState;
        private bool isStartingGame;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;

            mainState = BuildPanelState(mainMenuGroup, mainMenuPanel, -1f, true);
            settingsState = BuildPanelState(settingsGroup, settingsPanel, 1f, false);
            creditsState = BuildPanelState(creditsGroup, creditsPanel, 1f, false);

            ApplyPanel(mainState);
            ApplyPanel(settingsState);
            ApplyPanel(creditsState);

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }

            WireButtons();
            LoadSettings();
        }

        private PanelState BuildPanelState(CanvasGroup group, RectTransform rect, float direction, bool visible)
        {
            var state = new PanelState
            {
                group = group,
                rect = rect,
                direction = direction,
                visible = visible,
                t = visible ? 1f : 0f
            };

            if (rect != null) state.home = rect.anchoredPosition;
            return state;
        }

        private void WireButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(StartGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(ReturnToMainMenu);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(ReturnToMainMenu);

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void Update()
        {
            if (isStartingGame) return;

            AnimatePanel(mainState);
            AnimatePanel(settingsState);
            AnimatePanel(creditsState);

            if (WasCancelPressed() && (settingsState.visible || creditsState.visible))
            {
                ReturnToMainMenu();
            }
        }

        private void AnimatePanel(PanelState state)
        {
            if (state == null || state.group == null) return;

            float target = state.visible ? 1f : 0f;
            float step = panelFadeDuration <= 0.001f
                ? 1f
                : UnityEngine.Time.unscaledDeltaTime / panelFadeDuration;

            state.t = Mathf.MoveTowards(state.t, target, step);
            ApplyPanel(state);
        }

        private void ApplyPanel(PanelState state)
        {
            if (state == null || state.group == null) return;

            state.group.alpha = state.t;
            bool active = state.t > 0.001f || state.visible;
            state.group.gameObject.SetActive(active);

            if (state.rect != null)
            {
                float offset = state.direction * (1f - state.t) * panelSlideDistance;
                state.rect.anchoredPosition = new Vector2(state.home.x + offset, state.home.y);
            }

            // Only let a fully open panel take input.
            bool interactive = state.t > 0.85f;
            state.group.interactable = interactive;
            state.group.blocksRaycasts = interactive;
        }

        public void StartGame()
        {
            if (isStartingGame) return;
            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            isStartingGame = true;

            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                float t = 0f;
                float duration = Mathf.Max(0.01f, startFadeDuration);
                while (t < duration)
                {
                    t += UnityEngine.Time.unscaledDeltaTime;
                    fadeOverlay.alpha = Mathf.Clamp01(t / duration);
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }

            LoadGameplayScene();
        }

        private void LoadGameplayScene()
        {
            AudioListener.pause = false;
            UnityEngine.Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            if (gameplaySceneIndex >= 0 && gameplaySceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(gameplaySceneIndex);
                return;
            }

            // Absolute fallback: never leave the player stranded on the title screen.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OpenSettings()
        {
            SetPanelsVisible(main: false, settings: true, credits: false);
        }

        public void OpenCredits()
        {
            SetPanelsVisible(main: false, settings: false, credits: true);
        }

        public void ReturnToMainMenu()
        {
            SetPanelsVisible(main: true, settings: false, credits: false);
        }

        private void SetPanelsVisible(bool main, bool settings, bool credits)
        {
            if (mainState != null) mainState.visible = main;
            if (settingsState != null) settingsState.visible = settings;
            if (creditsState != null) creditsState.visible = credits;

            if (settings && settingsState != null && settingsState.group != null)
            {
                settingsState.group.gameObject.SetActive(true);
            }

            if (credits && creditsState != null && creditsState.group != null)
            {
                creditsState.group.gameObject.SetActive(true);
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadSettings()
        {
            float volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(volume));
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            }

            AudioListener.volume = Mathf.Clamp01(volume);
            Screen.fullScreen = fullscreen;
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            PlayerPrefs.Save();
        }

        private void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
