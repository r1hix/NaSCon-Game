using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UmbraChronos.UI
{
    /// <summary>
    /// Drives the opening title screen: Play / Level Select [P] / Settings / Credits / Quit.
    /// Supports both mouse navigation and the [P] hotkey for instant level selection
    /// across Master, numbered chapters, tutorial, and sandboxes.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Starting The Game")]
        [Tooltip("Default scene name loaded by the Play button.")]
        [SerializeField] private string gameplaySceneName = "Level_Master";

        [Tooltip("Build index used if gameplaySceneName is missing from the build settings.")]
        [SerializeField] private int gameplaySceneIndex = 1;

        [Header("Panels")]
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup settingsGroup;
        [SerializeField] private CanvasGroup creditsGroup;
        [SerializeField] private CanvasGroup levelSelectGroup;
        [SerializeField] private RectTransform mainMenuPanel;
        [SerializeField] private RectTransform settingsPanel;
        [SerializeField] private RectTransform creditsPanel;
        [SerializeField] private RectTransform levelSelectPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Panel")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button settingsBackButton;

        [Header("Credits Panel")]
        [SerializeField] private Button creditsBackButton;

        [Header("Level Select Panel")]
        [SerializeField] private Button levelSelectBackButton;
        [SerializeField] private Button btnMaster;
        [SerializeField] private Button btnLevel01;
        [SerializeField] private Button btnLevel02;
        [SerializeField] private Button btnLevel03;
        [SerializeField] private Button btnTutorial;
        [SerializeField] private Button btnSandboxShaf;
        [SerializeField] private Button btnSandboxChendline;

        [Header("Transitions")]
        [Tooltip("Fade/slide duration for a panel reveal, in seconds.")]
        [SerializeField] private float panelFadeDuration = 0.22f;

        [Tooltip("How far a panel slides while revealing, in canvas units.")]
        [SerializeField] private float panelSlideDistance = 48f;

        [Tooltip("Full-screen black image used for the fade-out when Play is pressed.")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float startFadeDuration = 0.7f;

        [Header("Audio")]
        [SerializeField] private AudioSource menuMusicSource;
        [SerializeField] private AudioClip titleAmbience;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip backSound;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.70f;
        [Range(0f, 1f)] [SerializeField] private float backVolume = 0.75f;

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
        private PanelState levelSelectState;
        private bool isStartingGame;

        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;

            EnsureLevelSelectUI();

            mainState = BuildPanelState(mainMenuGroup, mainMenuPanel, -1f, true);
            settingsState = BuildPanelState(settingsGroup, settingsPanel, 1f, false);
            creditsState = BuildPanelState(creditsGroup, creditsPanel, 1f, false);
            levelSelectState = BuildPanelState(levelSelectGroup, levelSelectPanel, 1f, false);

            ApplyPanel(mainState);
            ApplyPanel(settingsState);
            ApplyPanel(creditsState);
            ApplyPanel(levelSelectState);

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }

            WireButtons();
            LoadSettings();

#if UNITY_EDITOR
            if (titleAmbience == null)
                titleAmbience = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/bgm_title_ambient.wav");
            if (selectSound == null)
                selectSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_ui_select.wav");
            if (backSound == null)
                backSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_ui_back.wav");
#endif
            SetupMenuAudio();
        }

        private void SetupMenuAudio()
        {
            if (menuMusicSource == null)
            {
                menuMusicSource = GetComponent<AudioSource>();
                if (menuMusicSource == null)
                {
                    menuMusicSource = gameObject.AddComponent<AudioSource>();
                }
            }

            menuMusicSource.playOnAwake = false;
            menuMusicSource.loop = true;
            menuMusicSource.spatialBlend = 0f;

            if (titleAmbience != null && !menuMusicSource.isPlaying)
            {
                menuMusicSource.clip = titleAmbience;
                float savedVol = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
                menuMusicSource.volume = musicVolume * savedVol;
                menuMusicSource.Play();
            }
        }

        private void PlayBackSound()
        {
            if (backSound != null && menuMusicSource != null)
            {
                menuMusicSource.PlayOneShot(backSound, backVolume);
            }
        }

        private void PlaySelectSound()
        {
            if (selectSound != null && menuMusicSource != null)
            {
                menuMusicSource.PlayOneShot(selectSound, 0.85f);
            }
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
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OpenLevelSelect);
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
            AnimatePanel(levelSelectState);

            // [P] Hotkey to toggle Level Selection anywhere on title screen
            bool pPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) pPressed = true;
#endif
            if (!pPressed && Input.GetKeyDown(KeyCode.P)) pPressed = true;

            if (pPressed)
            {
                if (levelSelectState != null && levelSelectState.visible)
                {
                    ReturnToMainMenu();
                }
                else
                {
                    OpenLevelSelect();
                }
            }

            if (WasCancelPressed())
            {
                if ((settingsState != null && settingsState.visible) ||
                    (creditsState != null && creditsState.visible) ||
                    (levelSelectState != null && levelSelectState.visible))
                {
                    ReturnToMainMenu();
                }
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
            LoadLevelByName(gameplaySceneName);
        }

        public void LoadLevelByName(string sceneName)
        {
            if (isStartingGame) return;
            PlaySelectSound();
            StartCoroutine(StartGameRoutine(sceneName));
        }

        private IEnumerator StartGameRoutine(string targetScene)
        {
            isStartingGame = true;
            float initialMusicVol = menuMusicSource != null ? menuMusicSource.volume : 0f;

            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                float t = 0f;
                float duration = Mathf.Max(0.01f, startFadeDuration);
                while (t < duration)
                {
                    t += UnityEngine.Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(t / duration);
                    fadeOverlay.alpha = progress;
                    if (menuMusicSource != null)
                    {
                        menuMusicSource.volume = Mathf.Lerp(initialMusicVol, 0f, progress);
                    }
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }

            AudioListener.pause = false;
            UnityEngine.Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else if (gameplaySceneIndex >= 0 && gameplaySceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(gameplaySceneIndex);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void OpenSettings()
        {
            SetPanelsVisible(main: false, settings: true, credits: false, levelSelect: false);
        }

        public void OpenCredits()
        {
            SetPanelsVisible(main: false, settings: false, credits: true, levelSelect: false);
        }

        public void OpenLevelSelect()
        {
            PlaySelectSound();
            SetPanelsVisible(main: false, settings: false, credits: false, levelSelect: true);
        }

        public void ReturnToMainMenu()
        {
            PlayBackSound();
            SetPanelsVisible(main: true, settings: false, credits: false, levelSelect: false);
        }

        private void SetPanelsVisible(bool main, bool settings, bool credits, bool levelSelect)
        {
            if (mainState != null) mainState.visible = main;
            if (settingsState != null) settingsState.visible = settings;
            if (creditsState != null) creditsState.visible = credits;
            if (levelSelectState != null) levelSelectState.visible = levelSelect;

            if (settings && settingsState != null && settingsState.group != null)
                settingsState.group.gameObject.SetActive(true);

            if (credits && creditsState != null && creditsState.group != null)
                creditsState.group.gameObject.SetActive(true);

            if (levelSelect && levelSelectState != null && levelSelectState.group != null)
                levelSelectState.group.gameObject.SetActive(true);
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

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(volume));
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

            AudioListener.volume = Mathf.Clamp01(volume);
            Screen.fullScreen = fullscreen;
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            if (menuMusicSource != null)
            {
                menuMusicSource.volume = musicVolume * Mathf.Clamp01(value);
            }
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

        /// <summary>
        /// Builds the Level Select UI modal dynamically if not already serialized in the scene hierarchy.
        /// </summary>
        private void EnsureLevelSelectUI()
        {
            if (levelSelectGroup != null && levelSelectPanel != null) return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform root = parentCanvas != null ? parentCanvas.transform : transform;

            GameObject modal = new GameObject("LevelSelect_Modal");
            modal.transform.SetParent(root, false);

            levelSelectPanel = modal.AddComponent<RectTransform>();
            levelSelectPanel.anchorMin = levelSelectPanel.anchorMax = new Vector2(0.5f, 0.5f);
            levelSelectPanel.sizeDelta = new Vector2(960f, 760f);
            levelSelectPanel.anchoredPosition = Vector2.zero;

            levelSelectGroup = modal.AddComponent<CanvasGroup>();

            // Dark Obsidian Backdrop
            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.96f); // #0D0D15 obsidian
            bg.raycastTarget = true;

            var outline = modal.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.74f, 0.97f, 0.5f); // #38BDF8 cyan
            outline.effectDistance = new Vector2(2f, 2f);

            // Header Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modal.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.92f);
            titleRect.sizeDelta = new Vector2(800f, 50f);

            var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SELECT TIMELINE  [ P ]";
            titleTMP.fontSize = 28f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.98f, 0.95f, 0.88f, 1f);
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(modal.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, 0.85f);
            subRect.sizeDelta = new Vector2(800f, 30f);

            var subTMP = subObj.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Choose an Era, Sanctuary Wing, or Sandbox Testbed";
            subTMP.fontSize = 15f;
            subTMP.color = new Color(0.55f, 0.65f, 0.78f, 0.85f);
            subTMP.alignment = TextAlignmentOptions.Center;

            // Column of buttons
            GameObject col = new GameObject("Button_Column");
            col.transform.SetParent(modal.transform, false);
            var colRect = col.AddComponent<RectTransform>();
            colRect.anchorMin = colRect.anchorMax = new Vector2(0.5f, 0.48f);
            colRect.sizeDelta = new Vector2(760f, 480f);

            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            btnMaster = CreateLevelSelectButton(col.transform, "GRAND SANCTUARY (MASTER LEVEL)", () => LoadLevelByName("Level_Master"), new Color(0.98f, 0.85f, 0.4f, 1f));
            btnLevel01 = CreateLevelSelectButton(col.transform, "ERA 1: DAWN OF THE SUNDIAL", () => LoadLevelByName("Level_01"), Color.white);
            btnLevel02 = CreateLevelSelectButton(col.transform, "ERA 2: ECHO OF THE VAULT", () => LoadLevelByName("Level_02"), Color.white);
            btnLevel03 = CreateLevelSelectButton(col.transform, "ERA 3: THE ECLIPSE PARADOX", () => LoadLevelByName("Level_03"), Color.white);
            btnTutorial = CreateLevelSelectButton(col.transform, "TUTORIAL: THE WAY OF SHADOWS", () => LoadLevelByName("Level_Tutorial"), new Color(0.38f, 0.85f, 0.98f, 1f));
            btnSandboxShaf = CreateLevelSelectButton(col.transform, "DEBUG: SHAF SANDBOX", () => LoadLevelByName("Sandbox_Shaf"), new Color(0.70f, 0.75f, 0.85f, 1f));
            btnSandboxChendline = CreateLevelSelectButton(col.transform, "DEBUG: CHENDLINE SANDBOX", () => LoadLevelByName("Sandbox_Chendline"), new Color(0.70f, 0.75f, 0.85f, 1f));

            // Back Button
            GameObject backObj = new GameObject("Button_Back");
            backObj.transform.SetParent(modal.transform, false);
            var backRect = backObj.AddComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.08f);
            backRect.sizeDelta = new Vector2(300f, 44f);

            Image backImg = backObj.AddComponent<Image>();
            backImg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

            levelSelectBackButton = backObj.AddComponent<Button>();
            levelSelectBackButton.targetGraphic = backImg;
            levelSelectBackButton.onClick.AddListener(ReturnToMainMenu);

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backObj.transform, false);
            var btRect = backTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = btRect.offsetMax = Vector2.zero;

            var btTMP = backTextObj.AddComponent<TextMeshProUGUI>();
            btTMP.text = "[ ESC ]  BACK TO MAIN MENU";
            btTMP.fontSize = 15f;
            btTMP.fontStyle = FontStyles.Bold;
            btTMP.color = new Color(0.75f, 0.80f, 0.90f, 0.9f);
            btTMP.alignment = TextAlignmentOptions.Center;

            modal.SetActive(false);
        }

        private Button CreateLevelSelectButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color textColor)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760f, 46f);

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 46f;
            le.preferredWidth = 760f;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.10f, 0.10f, 0.16f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.10f, 0.10f, 0.16f, 0.85f);
            cb.highlightedColor = new Color(0.18f, 0.22f, 0.35f, 0.95f);
            cb.pressedColor = new Color(0.22f, 0.74f, 0.97f, 0.9f);
            btn.colors = cb;

            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(20f, 0f);
            tRect.offsetMax = new Vector2(-20f, 0f);

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Left;

            return btn;
        }
    }
}
