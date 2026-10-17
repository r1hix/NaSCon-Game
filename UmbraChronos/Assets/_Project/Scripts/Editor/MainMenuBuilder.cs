#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UmbraChronos.UI;

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Generates the opening title screen scene (Hollow-Knight-flavoured):
    /// layered silhouette backdrop that parallaxes with the mouse, a breathing
    /// solar glow, drifting dust motes, and animated Play / Settings / Credits / Quit menus.
    ///
    /// Run from the menu bar (Umbra Chronos -> Generate Title Screen Scene) or headlessly:
    ///   Unity.exe -batchmode -quit -projectPath &lt;project&gt;
    ///             -executeMethod UmbraChronos.Editor.MainMenuBuilder.GenerateMainMenuScene
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes/UI";
        private const string ScenePath = "Assets/_Project/Scenes/UI/MainMenu.unity";
        private const string SpriteFolder = "Assets/_Project/Sprites/UI";
        private const string LevelsFolder = "Assets/_Project/Scenes/Levels";

        // ---- Art direction palette (see AGENTS.md) ----
        private static readonly Color DeepVoid = new Color(0.011f, 0.020f, 0.045f, 1f);
        private static readonly Color MidnightIndigo = new Color(0.117f, 0.106f, 0.294f, 1f);
        private static readonly Color SolarCyan = new Color(0.220f, 0.741f, 0.973f, 1f);
        private static readonly Color SolarGold = new Color(1f, 0.820f, 0.400f, 1f);
        private static readonly Color Parchment = new Color(0.961f, 0.945f, 0.902f, 1f);
        private static readonly Color ObsidianPanel = new Color(0.051f, 0.051f, 0.082f, 0.95f);

        [MenuItem("Umbra Chronos/Generate Title Screen Scene")]
        public static void GenerateMainMenuScene()
        {
            AssetDatabase.Refresh();
            EnsureDirectories();
            GenerateMenuSprites();

            // Reuse the HUD's procedural UI art where it already exists.
            Sprite pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Pill_128.png");
            Sprite barSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Bar_White.png");
            Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Danger_Vignette_256.png");
            Sprite sunSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Sun_64.png");
            Sprite knobSprite = sunSprite != null ? sunSprite : barSprite;

            Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Menu_Sky_256.png");
            Sprite spireSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Menu_Spires_512.png");
            Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Menu_Glow_256.png");
            Sprite moteSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Menu_Mote_16.png");
            Sprite caretSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Sprite_Menu_Caret_32.png");

            // Audio assets for title screen ambience and UI button SFX
            AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_ui_hover.wav");
            AudioClip selectClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_ui_select.wav");
            AudioClip backClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/sfx_ui_back.wav");
            AudioClip titleBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/bgm_title_ambient.wav");

            if (hoverClip == null) hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("cca3324f5ed34b5abef01447e8d63eb6"));
            if (selectClip == null) selectClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("f96b6641c9f04dd68e93f5052d5deb3f"));
            if (backClip == null) backClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("6f9ecc7411e34e9c9b9d2eff91f20bdc"));
            if (titleBgm == null) titleBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("c2fb135849c44f3a9c347342de225b44"));

            Debug.Log($"[MainMenuBuilder] hoverClip={(hoverClip != null ? hoverClip.name : "NULL")}, selectClip={(selectClip != null ? selectClip.name : "NULL")}, backClip={(backClip != null ? backClip.name : "NULL")}, titleBgm={(titleBgm != null ? titleBgm.name : "NULL")}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();

            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            AudioSource menuAudio = canvasObj.AddComponent<AudioSource>();
            menuAudio.playOnAwake = true;
            menuAudio.loop = true;
            menuAudio.spatialBlend = 0f;
            menuAudio.clip = titleBgm;
            menuAudio.volume = 0.70f;

            MainMenuController menu = canvasObj.AddComponent<MainMenuController>();
            MenuAtmosphere atmosphere = canvasObj.AddComponent<MenuAtmosphere>();

            // ---------------- Background ----------------
            GameObject background = CreateUIObject("Backdrop", canvasObj.transform);
            Stretch(background.GetComponent<RectTransform>(), 0f, 0f);

            Image sky = CreateImage("Sky_Gradient", background.transform, skySprite, Color.white);
            Stretch(sky.rectTransform, 520f, 420f);

            Image glow = CreateImage("Solar_Glow", background.transform, glowSprite, new Color(1f, 0.82f, 0.45f, 0.30f));
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
            glow.rectTransform.sizeDelta = new Vector2(1500f, 940f);
            glow.rectTransform.anchoredPosition = Vector2.zero;
            glow.raycastTarget = false;

            Image farSpires = CreateSpireLayer("Spires_Far", background.transform, spireSprite, new Color(0.070f, 0.098f, 0.180f, 1f), 3000f, 640f, -10f);
            Image midSpires = CreateSpireLayer("Spires_Mid", background.transform, spireSprite, new Color(0.043f, 0.055f, 0.110f, 1f), 3100f, 540f, -60f);
            Image nearSpires = CreateSpireLayer("Spires_Near", background.transform, spireSprite, new Color(0.020f, 0.028f, 0.060f, 1f), 3200f, 460f, -120f);

            GameObject moteRoot = CreateUIObject("Dust_Motes", background.transform);
            Stretch(moteRoot.GetComponent<RectTransform>(), 0f, 0f);
            List<RectTransform> moteTargets = CreateMotes(moteRoot.transform, moteSprite, 16);

            Image vignette = CreateImage("Vignette", background.transform, vignetteSprite, new Color(0.010f, 0.012f, 0.030f, 0.90f));
            Stretch(vignette.rectTransform, 260f, 200f);
            vignette.raycastTarget = false;

            // ---------------- Content ----------------
            GameObject content = CreateUIObject("Content", canvasObj.transform);
            Stretch(content.GetComponent<RectTransform>(), 0f, 0f);

            // Main panel (title + menu + footer)
            GameObject mainPanel = CreateUIObject("Main_Panel", content.transform);
            Stretch(mainPanel.GetComponent<RectTransform>(), 0f, 0f);
            CanvasGroup mainGroup = mainPanel.AddComponent<CanvasGroup>();

            GameObject titleGroup = CreateUIObject("Title_Group", mainPanel.transform);
            RectTransform titleRect = titleGroup.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.70f);
            titleRect.sizeDelta = new Vector2(1500f, 460f);

            TextMeshProUGUI titleGlow = CreateText(titleGroup.transform, "Title_Glow", "UMBRA CHRONOS", 118, TextAlignmentOptions.Center, new Color(0.196f, 0.663f, 0.878f, 0.22f));
            titleGlow.rectTransform.sizeDelta = new Vector2(1500f, 160f);
            titleGlow.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            titleGlow.fontStyle = FontStyles.Bold;

            TextMeshProUGUI titleText = CreateText(titleGroup.transform, "Title_Text", "UMBRA CHRONOS", 112, TextAlignmentOptions.Center, Parchment);
            titleText.rectTransform.sizeDelta = new Vector2(1500f, 160f);
            titleText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = CreateText(titleGroup.transform, "Subtitle_Text", "ECHOES OF THE SUNDIAL", 34, TextAlignmentOptions.Center, SolarCyan);
            subtitle.rectTransform.sizeDelta = new Vector2(1500f, 50f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -96f);
            subtitle.characterSpacing = 22f;

            TextMeshProUGUI tagline = CreateText(titleGroup.transform, "Tagline_Text", "Light is Lethal.  Shadows are Matter.", 22, TextAlignmentOptions.Center, new Color(0.78f, 0.82f, 0.88f, 0.72f));
            tagline.rectTransform.sizeDelta = new Vector2(1500f, 40f);
            tagline.rectTransform.anchoredPosition = new Vector2(0f, -150f);

            GameObject menuColumn = CreateUIObject("Menu_Column", mainPanel.transform);
            RectTransform menuRect = menuColumn.GetComponent<RectTransform>();
            menuRect.anchorMin = menuRect.anchorMax = new Vector2(0.5f, 0.30f);
            menuRect.sizeDelta = new Vector2(620f, 300f);

            VerticalLayoutGroup layout = menuColumn.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button playButton = CreateMenuButton(menuColumn.transform, "PLAY", caretSprite, hoverClip, selectClip);
            Button levelSelectButton = CreateMenuButton(menuColumn.transform, "SELECT LEVEL [P]", caretSprite, hoverClip, selectClip);
            Button settingsButton = CreateMenuButton(menuColumn.transform, "SETTINGS", caretSprite, hoverClip, selectClip);
            Button creditsButton = CreateMenuButton(menuColumn.transform, "CREDITS", caretSprite, hoverClip, selectClip);
            Button quitButton = CreateMenuButton(menuColumn.transform, "QUIT", caretSprite, hoverClip, selectClip);

            TextMeshProUGUI footer = CreateText(mainPanel.transform, "Footer_Text", "rihix  ·  chendline     —     developed for NaSCon 2026", 22, TextAlignmentOptions.Center, new Color(0.62f, 0.68f, 0.78f, 0.85f));
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0.5f, 0.06f);
            footer.rectTransform.sizeDelta = new Vector2(1400f, 40f);

            TextMeshProUGUI hint = CreateText(mainPanel.transform, "Hint_Text", "Press [P] for Level Select  ·  Unity 6  ·  URP 2D", 16, TextAlignmentOptions.Right, new Color(0.55f, 0.60f, 0.70f, 0.75f));
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.pivot = new Vector2(1f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(-40f, 30f);
            hint.rectTransform.sizeDelta = new Vector2(700f, 30f);

            // Settings panel
            GameObject settingsPanel = CreateModalPanel(content.transform, "Settings_Panel", "SETTINGS", pillSprite, new Vector2(880f, 640f));
            CanvasGroup settingsGroup = settingsPanel.AddComponent<CanvasGroup>();

            TextMeshProUGUI volumeLabel = CreateText(settingsPanel.transform, "Volume_Label", "MASTER VOLUME", 24, TextAlignmentOptions.Left, Parchment);
            volumeLabel.rectTransform.anchorMin = volumeLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            volumeLabel.rectTransform.anchoredPosition = new Vector2(-280f, 70f);
            volumeLabel.rectTransform.sizeDelta = new Vector2(360f, 40f);
            Slider volumeSlider = CreateSlider(settingsPanel.transform, new Vector2(0f, 10f), new Vector2(600f, 36f), barSprite, knobSprite);

            TextMeshProUGUI fullscreenLabel = CreateText(settingsPanel.transform, "Fullscreen_Label", "FULLSCREEN", 24, TextAlignmentOptions.Left, Parchment);
            fullscreenLabel.rectTransform.anchorMin = fullscreenLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            fullscreenLabel.rectTransform.anchoredPosition = new Vector2(-280f, -110f);
            fullscreenLabel.rectTransform.sizeDelta = new Vector2(360f, 40f);
            Toggle fullscreenToggle = CreateToggle(settingsPanel.transform, new Vector2(-40f, -110f), barSprite, knobSprite);

            Button settingsBack = CreateMenuButton(settingsPanel.transform, "BACK", caretSprite, hoverClip, backClip);
            settingsBack.GetComponent<RectTransform>().anchorMin = settingsBack.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            settingsBack.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -230f);
            LayoutElement settingsBackLayout = settingsBack.gameObject.AddComponent<LayoutElement>();
            settingsBackLayout.preferredWidth = 460f;
            settingsBackLayout.preferredHeight = 58f;

            // Credits panel
            GameObject creditsPanel = CreateModalPanel(content.transform, "Credits_Panel", "CREDITS", pillSprite, new Vector2(980f, 760f));
            CanvasGroup creditsGroup = creditsPanel.AddComponent<CanvasGroup>();

            string creditsBody =
                "UMBRA CHRONOS — ECHOES OF THE SUNDIAL\n\n" +
                "<color=#38BDF8>CREATED BY</color>\n" +
                "rihix\nchendline\n\n" +
                "<color=#38BDF8>DEVELOPED FOR</color>\n" +
                "NaSCon 2026\nVibe Coding Game Dev\nFAST-NUCES Islamabad\n\n" +
                "<color=#38BDF8>BUILT WITH</color>\n" +
                "Unity 6  ·  URP 2D Lighting";

            TextMeshProUGUI creditsText = CreateText(creditsPanel.transform, "Credits_Body", creditsBody, 26, TextAlignmentOptions.Center, Parchment);
            creditsText.rectTransform.sizeDelta = new Vector2(880f, 560f);
            creditsText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            creditsText.lineSpacing = 6f;

            Button creditsBack = CreateMenuButton(creditsPanel.transform, "BACK", caretSprite, hoverClip, backClip);
            creditsBack.GetComponent<RectTransform>().anchorMin = creditsBack.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            creditsBack.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -320f);
            LayoutElement creditsBackLayout = creditsBack.gameObject.AddComponent<LayoutElement>();
            creditsBackLayout.preferredWidth = 460f;
            creditsBackLayout.preferredHeight = 58f;

            // Fade overlay (top-most)
            Image fade = CreateImage("Fade_Overlay", content.transform, null, Color.black);
            Stretch(fade.rectTransform, 0f, 0f);
            fade.raycastTarget = false;
            CanvasGroup fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            // ---------------- Wiring ----------------
            WireController(menu, mainGroup, mainPanel.GetComponent<RectTransform>(),
                settingsGroup, settingsPanel.GetComponent<RectTransform>(),
                creditsGroup, creditsPanel.GetComponent<RectTransform>(),
                playButton, levelSelectButton, settingsButton, creditsButton, quitButton,
                volumeSlider, fullscreenToggle, settingsBack, creditsBack, fadeGroup,
                menuAudio, titleBgm, backClip);

            WireAtmosphere(atmosphere, sky.rectTransform, glow.rectTransform, farSpires.rectTransform, midSpires.rectTransform,
                nearSpires.rectTransform, titleRect, menuRect, glow, moteTargets);

            // Hide the modal panels until they are opened.
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MainMenuBuilder] Title screen generated at {ScenePath}");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Title Screen Ready",
                    "Generated Assets/_Project/Scenes/UI/MainMenu.unity\n\n" +
                    "Play / Settings / Credits / Quit, an animated silhouette backdrop that " +
                    "parallaxes with the mouse, and NaSCon 2026 credits for rihix and chendline.",
                    "Let's Go!");
            }
        }

        private static void BuildCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DeepVoid;
            camObj.AddComponent<AudioListener>();

            var additional = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (additional != null) additional.renderPostProcessing = false;
        }

        private static void BuildEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            // The project uses the new Input System only (activeInputHandler: 1).
            // The module assigns the default UI actions on enable.
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Image CreateSpireLayer(string name, Transform parent, Sprite sprite, Color color, float width, float height, float bottomOffset)
        {
            Image image = CreateImage(name, parent, sprite, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, bottomOffset);
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            return image;
        }

        private static List<RectTransform> CreateMotes(Transform parent, Sprite moteSprite, int count)
        {
            var targets = new List<RectTransform>();
            for (int i = 0; i < count; i++)
            {
                Image mote = CreateImage($"Mote_{i:00}", parent, moteSprite, new Color(1f, 0.92f, 0.70f, 0.35f));
                RectTransform rect = mote.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                float size = Random.Range(8f, 26f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(Random.Range(-900f, 900f), Random.Range(-460f, 320f));
                mote.raycastTarget = false;
                mote.color = new Color(1f, Random.Range(0.86f, 0.95f), Random.Range(0.6f, 0.8f), Random.Range(0.18f, 0.42f));
                targets.Add(rect);
            }
            return targets;
        }

        private static Button CreateMenuButton(Transform parent, string labelText, Sprite caretSprite, AudioClip hoverClip = null, AudioClip clickClip = null)
        {
            GameObject obj = CreateUIObject($"Button_{labelText}", parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 60f);

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = 560f;
            layout.preferredHeight = 58f;

            Image hit = obj.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            Button button = obj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;

            GameObject caret = CreateUIObject("Caret", obj.transform);
            RectTransform caretRect = caret.GetComponent<RectTransform>();
            caretRect.anchorMin = caretRect.anchorMax = new Vector2(0f, 0.5f);
            caretRect.pivot = new Vector2(0f, 0.5f);
            caretRect.sizeDelta = new Vector2(20f, 20f);
            caretRect.anchoredPosition = new Vector2(4f, 0f);
            Image caretImage = caret.AddComponent<Image>();
            caretImage.sprite = caretSprite;
            caretImage.color = new Color(SolarCyan.r, SolarCyan.g, SolarCyan.b, 0f);
            caretImage.raycastTarget = false;
            caret.SetActive(false);

            GameObject labelObj = CreateUIObject("Label", obj.transform);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(40f, 0f);
            labelRect.sizeDelta = new Vector2(520f, 56f);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 38f;
            label.alignment = TextAlignmentOptions.Left;
            label.color = new Color(0.60f, 0.66f, 0.76f, 0.85f);
            label.characterSpacing = 6f;
            label.raycastTarget = false;

            MenuButtonEffect effect = obj.AddComponent<MenuButtonEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("labelTransform").objectReferenceValue = labelRect;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("cursor").objectReferenceValue = caretRect;
            so.FindProperty("cursorImage").objectReferenceValue = caretImage;
            if (hoverClip != null) so.FindProperty("hoverSound").objectReferenceValue = hoverClip;
            if (clickClip != null) so.FindProperty("clickSound").objectReferenceValue = clickClip;
            so.ApplyModifiedProperties();

            return button;
        }

        private static GameObject CreateModalPanel(Transform parent, string name, string heading, Sprite pillSprite, Vector2 size)
        {
            GameObject panel = CreateUIObject(name, parent);
            Stretch(panel.GetComponent<RectTransform>(), 0f, 0f);

            Image backdrop = CreateImage("Backdrop", panel.transform, pillSprite, ObsidianPanel);
            RectTransform rect = backdrop.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            backdrop.raycastTarget = true;

            TextMeshProUGUI headingText = CreateText(panel.transform, "Heading", heading, 46, TextAlignmentOptions.Center, SolarCyan);
            headingText.rectTransform.anchorMin = headingText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            headingText.rectTransform.sizeDelta = new Vector2(760f, 70f);
            headingText.rectTransform.anchoredPosition = new Vector2(0f, (size.y * 0.5f) - 90f);
            headingText.characterSpacing = 14f;
            headingText.fontStyle = FontStyles.Bold;

            return panel;
        }

        private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size, Sprite barSprite, Sprite knobSprite)
        {
            GameObject obj = CreateUIObject("Volume_Slider", parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Slider slider = obj.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject bg = CreateUIObject("Background", obj.transform);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.sprite = barSprite;
            bgImage.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

            GameObject fillArea = CreateUIObject("Fill Area", obj.transform);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-24f, 0f);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(24f, 0f);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.sprite = barSprite;
            fillImage.color = SolarCyan;

            GameObject handleArea = CreateUIObject("Handle Slide Area", obj.transform);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.sizeDelta = new Vector2(-24f, 0f);

            GameObject handle = CreateUIObject("Handle", handleArea.transform);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 30f);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = knobSprite;
            handleImage.color = SolarGold;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private static Toggle CreateToggle(Transform parent, Vector2 position, Sprite boxSprite, Sprite checkSprite)
        {
            GameObject obj = CreateUIObject("Fullscreen_Toggle", parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = position;

            Toggle toggle = obj.AddComponent<Toggle>();

            Image box = obj.AddComponent<Image>();
            box.sprite = boxSprite;
            box.color = new Color(0.15f, 0.18f, 0.26f, 0.95f);

            GameObject checkmark = CreateUIObject("Checkmark", obj.transform);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.sizeDelta = Vector2.zero;
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.sprite = checkSprite;
            checkImage.color = SolarCyan;

            toggle.targetGraphic = box;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        private static void WireController(
            MainMenuController menu,
            CanvasGroup mainGroup, RectTransform mainRect,
            CanvasGroup settingsGroup, RectTransform settingsRect,
            CanvasGroup creditsGroup, RectTransform creditsRect,
            Button play, Button levelSelect, Button settings, Button credits, Button quit,
            Slider volume, Toggle fullscreen, Button settingsBack, Button creditsBack,
            CanvasGroup fade,
            AudioSource menuAudio = null, AudioClip titleBgm = null, AudioClip backSound = null)
        {
            string gameplayScene = File.Exists($"{LevelsFolder}/Level_Master.unity") ? "Level_Master" : "Level_01";

            SerializedObject so = new SerializedObject(menu);
            so.FindProperty("gameplaySceneName").stringValue = gameplayScene;
            so.FindProperty("gameplaySceneIndex").intValue = 1;
            so.FindProperty("mainMenuGroup").objectReferenceValue = mainGroup;
            so.FindProperty("settingsGroup").objectReferenceValue = settingsGroup;
            so.FindProperty("creditsGroup").objectReferenceValue = creditsGroup;
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainRect;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsRect;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsRect;
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("levelSelectButton").objectReferenceValue = levelSelect;
            so.FindProperty("settingsButton").objectReferenceValue = settings;
            so.FindProperty("creditsButton").objectReferenceValue = credits;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("masterVolumeSlider").objectReferenceValue = volume;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            so.FindProperty("settingsBackButton").objectReferenceValue = settingsBack;
            so.FindProperty("creditsBackButton").objectReferenceValue = creditsBack;
            so.FindProperty("fadeOverlay").objectReferenceValue = fade;
            if (menuAudio != null) so.FindProperty("menuMusicSource").objectReferenceValue = menuAudio;
            if (titleBgm != null) so.FindProperty("titleAmbience").objectReferenceValue = titleBgm;
            if (backSound != null) so.FindProperty("backSound").objectReferenceValue = backSound;
            so.ApplyModifiedProperties();
        }

        private static void WireAtmosphere(
            MenuAtmosphere atmosphere,
            RectTransform sky, RectTransform glow, RectTransform farSpires, RectTransform midSpires, RectTransform nearSpires,
            RectTransform title, RectTransform menu, Image glowImage, List<RectTransform> motes)
        {
            SerializedObject so = new SerializedObject(atmosphere);

            SerializedProperty layers = so.FindProperty("parallaxLayers");
            layers.arraySize = 7;
            SetLayer(layers, 0, sky, 10f, 6f, 0.07f);
            SetLayer(layers, 1, glow, 6f, 0f, 0f);
            SetLayer(layers, 2, farSpires, 20f, 10f, 0.09f);
            SetLayer(layers, 3, midSpires, 38f, 16f, 0.11f);
            SetLayer(layers, 4, nearSpires, 64f, 22f, 0.13f);
            SetLayer(layers, 5, title, -12f, 0f, 0f);
            SetLayer(layers, 6, menu, -20f, 0f, 0f);

            SerializedProperty moteArray = so.FindProperty("motes");
            moteArray.arraySize = motes.Count;
            for (int i = 0; i < motes.Count; i++)
            {
                SerializedProperty element = moteArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("target").objectReferenceValue = motes[i];
                element.FindPropertyRelative("driftAmount").floatValue = Random.Range(20f, 70f);
                element.FindPropertyRelative("driftSpeed").floatValue = Random.Range(0.15f, 0.5f);
                element.FindPropertyRelative("risePeriod").floatValue = Random.Range(14f, 30f);
                element.FindPropertyRelative("riseDistance").floatValue = Random.Range(220f, 520f);
                element.FindPropertyRelative("phase").floatValue = Random.Range(0f, 6.28318f);
            }

            so.FindProperty("floatingTitle").objectReferenceValue = title;
            so.FindProperty("glowImage").objectReferenceValue = glowImage;
            so.ApplyModifiedProperties();
        }

        private static void SetLayer(SerializedProperty layers, int index, RectTransform target, float depth, float sway, float swaySpeed)
        {
            SerializedProperty element = layers.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("target").objectReferenceValue = target;
            element.FindPropertyRelative("depth").floatValue = depth;
            element.FindPropertyRelative("swayAmount").floatValue = sway;
            element.FindPropertyRelative("swaySpeed").floatValue = swaySpeed;
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };

            string[] gameplayScenes =
            {
                $"{LevelsFolder}/Level_Master.unity",
                $"{LevelsFolder}/Level_01.unity",
                $"{LevelsFolder}/Level_02.unity",
                $"{LevelsFolder}/Level_03.unity",
                $"{LevelsFolder}/Level_Tutorial.unity",
                "Assets/_Project/Scenes/Sandboxes/Sandbox_Shaf.unity"
            };

            foreach (string path in gameplayScenes)
            {
                if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------- Small helpers ----------------

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void Stretch(RectTransform rect, float expandX, float expandY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-expandX * 0.5f, -expandY * 0.5f);
            rect.offsetMax = new Vector2(expandX * 0.5f, expandY * 0.5f);
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);
            if (!Directory.Exists(SpriteFolder)) Directory.CreateDirectory(SpriteFolder);
        }

        // ---------------- Procedural art ----------------

        private static void GenerateMenuSprites()
        {
            CreateSkyGradient($"{SpriteFolder}/Sprite_Menu_Sky_256.png", 256, 256);
            CreateSpireSilhouette($"{SpriteFolder}/Sprite_Menu_Spires_512.png", 512, 256);
            CreateSoftGlow($"{SpriteFolder}/Sprite_Menu_Glow_256.png", 256, 2.2f);
            CreateSoftDot($"{SpriteFolder}/Sprite_Menu_Mote_16.png", 16);
            CreateCaret($"{SpriteFolder}/Sprite_Menu_Caret_32.png", 32);

            AssetDatabase.Refresh();
        }

        private static void CreateSkyGradient(string path, int width, int height)
        {
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1); // 0 = bottom, 1 = top
                // Dusk -> deep void, with a faint solar band near the lower third.
                Color bottom = Color.Lerp(MidnightIndigo, new Color(0.18f, 0.12f, 0.28f, 1f), t);
                Color top = Color.Lerp(DeepVoid, new Color(0.03f, 0.05f, 0.11f, 1f), t);
                Color column = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, t));

                float horizon = Mathf.Exp(-Mathf.Pow((t - 0.22f) / 0.16f, 2f));
                column += new Color(0.35f, 0.22f, 0.10f, 0f) * horizon;

                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, column);
                }
            }
            tex.Apply();
            WriteSprite(path, tex, 100f, FilterMode.Bilinear);
        }

        private static void CreateSpireSilhouette(string path, int width, int height)
        {
            if (File.Exists(path)) return;

            var random = new System.Random(20261018);
            float[] columns = new float[width];
            float baseHeight = 0.30f;

            for (int x = 0; x < width; x++)
            {
                float h = baseHeight;
                h += Mathf.Sin((x * 0.021f) + 1.3f) * 0.05f;
                h += Mathf.Sin((x * 0.087f) + 4.1f) * 0.025f;
                h += (float)(random.NextDouble() - 0.5) * 0.012f;
                columns[x] = h;
            }

            // A handful of tall towers, some with pointed caps.
            int towerCount = 9;
            for (int i = 0; i < towerCount; i++)
            {
                int center = random.Next(20, width - 20);
                int halfWidth = random.Next(4, 11);
                float towerTop = baseHeight + (float)(0.18 + (random.NextDouble() * 0.42));
                bool pointed = random.NextDouble() > 0.45;

                for (int x = center - halfWidth; x <= center + halfWidth; x++)
                {
                    if (x < 0 || x >= width) continue;

                    float local = Mathf.Abs(x - (center + 0.5f)) / halfWidth;
                    float top = pointed ? Mathf.Lerp(towerTop + 0.16f, towerTop, local) : towerTop;
                    if (top > columns[x]) columns[x] = top;
                }
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                int skyline = Mathf.Clamp(Mathf.RoundToInt(columns[x] * height), 1, height);
                for (int y = 0; y < height; y++)
                {
                    bool solid = y < skyline;
                    // Soft 1px top edge so silhouettes do not look jagged when scaled.
                    float alpha = solid ? 1f : 0f;
                    if (solid && y == skyline - 1) alpha = 0.7f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            WriteSprite(path, tex, 100f, FilterMode.Bilinear);
        }

        private static void CreateSoftGlow(string path, int size, float falloff)
        {
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.Pow(alpha, falloff);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            WriteSprite(path, tex, 100f, FilterMode.Bilinear);
        }

        private static void CreateSoftDot(string path, int size)
        {
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    float alpha = Mathf.Clamp01(1f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            tex.Apply();
            WriteSprite(path, tex, 100f, FilterMode.Bilinear);
        }

        private static void CreateCaret(string path, int size)
        {
            if (File.Exists(path)) return;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // A rune-like diamond.
                    float dx = Mathf.Abs(x - half + 0.5f) / half;
                    float dy = Mathf.Abs(y - half + 0.5f) / half;
                    float d = dx + dy;
                    float alpha = Mathf.Clamp01((1f - d) * half);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            WriteSprite(path, tex, 100f, FilterMode.Bilinear);
        }

        private static void WriteSprite(string path, Texture2D texture, float pixelsPerUnit, FilterMode filterMode)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = filterMode;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
