#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UmbraChronos.UI;

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Programmatic generator for the complete Umbra Chronos HUD System.
    /// Creates procedural UI art assets and builds the production HUD prefab:
    /// - Sundial Compass with rotating celestial pointer needle
    /// - Rewind Tape Reel gauge with countdown buffer and pulse VFX
    /// - Interactive runic input prompt pills
    /// - Full-screen solar hazard vignette flare
    /// - Time fracture death rewind prompt
    /// - Level title banner card
    /// </summary>
    public static class HUDBuilder
    {
        private const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        private const string SpriteUIFolder = "Assets/_Project/Sprites/UI";

        [MenuItem("Umbra Chronos/Generate HUD Prefab")]
        public static GameObject GenerateHUDPrefab()
        {
            EnsureDirectories();
            GenerateUISprites();

            Sprite spriteDial = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Dial_128.png");
            Sprite spriteNeedle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Needle_32.png");
            Sprite spritePill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Pill_128.png");
            Sprite spriteBar = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Bar_White.png");
            Sprite spriteSun = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Sun_64.png");
            Sprite spriteTape = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Tape_64.png");
            Sprite spriteVignette = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteUIFolder}/Sprite_Danger_Vignette_256.png");

            // 1. Root Canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            HUDController hud = canvasObj.AddComponent<HUDController>();
            SerializedObject so = new SerializedObject(hud);

            // ==========================================
            // 2. SOLAR DANGER VIGNETTE (Full-Screen)
            // ==========================================
            GameObject dangerObj = new GameObject("Solar_Danger_Vignette");
            dangerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform dangerRect = dangerObj.AddComponent<RectTransform>();
            dangerRect.anchorMin = Vector2.zero;
            dangerRect.anchorMax = Vector2.one;
            dangerRect.offsetMin = Vector2.zero;
            dangerRect.offsetMax = Vector2.zero;
            Image dangerImg = dangerObj.AddComponent<Image>();
            dangerImg.sprite = spriteVignette;
            dangerImg.color = new Color(0.95f, 0.25f, 0.1f, 0f);
            dangerImg.raycastTarget = false;
            so.FindProperty("solarDangerVignette").objectReferenceValue = dangerImg;

            // Solar Alert Banner (Top-Center)
            GameObject alertBanner = new GameObject("Solar_Alert_Banner");
            alertBanner.transform.SetParent(canvasObj.transform, false);
            RectTransform alertRect = alertBanner.AddComponent<RectTransform>();
            alertRect.anchorMin = new Vector2(0.5f, 1f);
            alertRect.anchorMax = new Vector2(0.5f, 1f);
            alertRect.pivot = new Vector2(0.5f, 1f);
            alertRect.anchoredPosition = new Vector2(0f, -80f);
            alertRect.sizeDelta = new Vector2(480f, 44f);
            Image alertBg = alertBanner.AddComponent<Image>();
            alertBg.sprite = spritePill;
            alertBg.color = new Color(0.7f, 0.1f, 0.1f, 0.85f);

            GameObject alertTextObj = CreateText("Alert_Text", alertBanner.transform, "⚠ SEARING LIGHT — SEEK SHADOW!", 20, TextAlignmentOptions.Center, Color.white);
            RectTransform alertTextRect = alertTextObj.GetComponent<RectTransform>();
            alertTextRect.anchorMin = Vector2.zero;
            alertTextRect.anchorMax = Vector2.one;
            alertTextRect.offsetMin = Vector2.zero;
            alertTextRect.offsetMax = Vector2.zero;

            so.FindProperty("solarAlertBanner").objectReferenceValue = alertBanner;
            so.FindProperty("solarAlertText").objectReferenceValue = alertTextObj.GetComponent<TextMeshProUGUI>();
            alertBanner.SetActive(false);

            // ==========================================
            // 3. SUNDIAL COMPASS (Top-Right)
            // ==========================================
            GameObject compassContainer = new GameObject("Sundial_Compass");
            compassContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform compassRect = compassContainer.AddComponent<RectTransform>();
            compassRect.anchorMin = new Vector2(1f, 1f);
            compassRect.anchorMax = new Vector2(1f, 1f);
            compassRect.pivot = new Vector2(1f, 1f);
            compassRect.anchoredPosition = new Vector2(-40f, -40f);
            compassRect.sizeDelta = new Vector2(160f, 160f);

            // Dial Background Ring
            GameObject dialBg = new GameObject("Dial_Ring");
            dialBg.transform.SetParent(compassContainer.transform, false);
            RectTransform dialBgRect = dialBg.AddComponent<RectTransform>();
            dialBgRect.sizeDelta = new Vector2(140f, 140f);
            Image dialBgImg = dialBg.AddComponent<Image>();
            dialBgImg.sprite = spriteDial;
            dialBgImg.color = new Color(0.08f, 0.08f, 0.14f, 0.88f);

            // Rotating Needle
            GameObject needleObj = new GameObject("Needle");
            needleObj.transform.SetParent(dialBg.transform, false);
            RectTransform needleRect = needleObj.AddComponent<RectTransform>();
            needleRect.sizeDelta = new Vector2(20f, 110f);
            Image needleImg = needleObj.AddComponent<Image>();
            needleImg.sprite = spriteNeedle;
            needleImg.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            so.FindProperty("compassNeedle").objectReferenceValue = needleRect;

            // Center Sun Orb
            GameObject centerSun = new GameObject("Sun_Orb");
            centerSun.transform.SetParent(dialBg.transform, false);
            RectTransform sunRect = centerSun.AddComponent<RectTransform>();
            sunRect.sizeDelta = new Vector2(34f, 34f);
            Image sunImg = centerSun.AddComponent<Image>();
            sunImg.sprite = spriteSun;
            sunImg.color = new Color(1f, 0.85f, 0.4f, 1f);
            so.FindProperty("compassSunIcon").objectReferenceValue = sunImg;

            // Phase Text Label
            GameObject phaseObj = CreateText("Phase_Text", compassContainer.transform, "ZENITH (NOON)", 18, TextAlignmentOptions.Center, new Color(0.22f, 0.74f, 0.97f, 1f));
            RectTransform phaseRect = phaseObj.GetComponent<RectTransform>();
            phaseRect.anchoredPosition = new Vector2(0f, -88f);
            phaseRect.sizeDelta = new Vector2(180f, 30f);
            so.FindProperty("phaseText").objectReferenceValue = phaseObj.GetComponent<TextMeshProUGUI>();

            // Angle Text Label
            GameObject angleObj = CreateText("Angle_Text", compassContainer.transform, "90° ARC", 14, TextAlignmentOptions.Center, new Color(0.8f, 0.85f, 0.9f, 0.8f));
            RectTransform angleRect = angleObj.GetComponent<RectTransform>();
            angleRect.anchoredPosition = new Vector2(0f, -112f);
            angleRect.sizeDelta = new Vector2(160f, 24f);
            so.FindProperty("angleText").objectReferenceValue = angleObj.GetComponent<TextMeshProUGUI>();

            // ==========================================
            // 4. TEMPORAL REWIND GAUGE (Top-Left)
            // ==========================================
            GameObject rewindContainer = new GameObject("Rewind_Gauge");
            rewindContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform rewindRect = rewindContainer.AddComponent<RectTransform>();
            rewindRect.anchorMin = new Vector2(0f, 1f);
            rewindRect.anchorMax = new Vector2(0f, 1f);
            rewindRect.pivot = new Vector2(0f, 1f);
            rewindRect.anchoredPosition = new Vector2(40f, -40f);
            rewindRect.sizeDelta = new Vector2(320f, 80f);

            // Container Background Pill
            GameObject rewindBg = new GameObject("Rewind_Bg");
            rewindBg.transform.SetParent(rewindContainer.transform, false);
            RectTransform rwBgRect = rewindBg.AddComponent<RectTransform>();
            rwBgRect.anchorMin = Vector2.zero;
            rwBgRect.anchorMax = Vector2.one;
            rwBgRect.offsetMin = Vector2.zero;
            rwBgRect.offsetMax = Vector2.zero;
            Image rwBgImg = rewindBg.AddComponent<Image>();
            rwBgImg.sprite = spritePill;
            rwBgImg.color = new Color(0.06f, 0.06f, 0.10f, 0.85f);

            // Tape Spool Icon
            GameObject spoolObj = new GameObject("Tape_Spool");
            spoolObj.transform.SetParent(rewindContainer.transform, false);
            RectTransform spoolRect = spoolObj.AddComponent<RectTransform>();
            spoolRect.anchorMin = new Vector2(0f, 0.5f);
            spoolRect.anchorMax = new Vector2(0f, 0.5f);
            spoolRect.pivot = new Vector2(0f, 0.5f);
            spoolRect.anchoredPosition = new Vector2(16f, 0f);
            spoolRect.sizeDelta = new Vector2(48f, 48f);
            Image spoolImg = spoolObj.AddComponent<Image>();
            spoolImg.sprite = spriteTape;
            spoolImg.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            so.FindProperty("rewindTapeSpool").objectReferenceValue = spoolRect;

            // Energy Bar Background Track
            GameObject trackObj = new GameObject("Energy_Track");
            trackObj.transform.SetParent(rewindContainer.transform, false);
            RectTransform trackRect = trackObj.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.anchoredPosition = new Vector2(76f, -10f);
            trackRect.sizeDelta = new Vector2(-96f, 14f);
            Image trackImg = trackObj.AddComponent<Image>();
            trackImg.sprite = spritePill;
            trackImg.color = new Color(0.12f, 0.14f, 0.22f, 0.8f);

            // Filled Energy Bar
            GameObject fillObj = new GameObject("Energy_Fill");
            fillObj.transform.SetParent(trackObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = spritePill;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            so.FindProperty("rewindBarFill").objectReferenceValue = fillImg;

            // Rewind Buffer Text
            GameObject rwTextObj = CreateText("Buffer_Text", rewindContainer.transform, "REWIND: 5.0s / 5.0s", 16, TextAlignmentOptions.Left, Color.white);
            RectTransform rwTextRect = rwTextObj.GetComponent<RectTransform>();
            rwTextRect.anchorMin = new Vector2(0f, 0.5f);
            rwTextRect.anchorMax = new Vector2(1f, 0.5f);
            rwTextRect.anchoredPosition = new Vector2(78f, 15f);
            rwTextRect.sizeDelta = new Vector2(-96f, 24f);
            so.FindProperty("rewindBufferText").objectReferenceValue = rwTextObj.GetComponent<TextMeshProUGUI>();

            // Rewind Status Badge Text
            GameObject statusObj = CreateText("Status_Text", rewindContainer.transform, "BUFFER FULL", 12, TextAlignmentOptions.Right, new Color(0.22f, 0.74f, 0.97f, 0.9f));
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1f, 0.5f);
            statusRect.anchorMax = new Vector2(1f, 0.5f);
            statusRect.pivot = new Vector2(1f, 0.5f);
            statusRect.anchoredPosition = new Vector2(-20f, 15f);
            statusRect.sizeDelta = new Vector2(120f, 20f);
            so.FindProperty("rewindStatusText").objectReferenceValue = statusObj.GetComponent<TextMeshProUGUI>();

            // ==========================================
            // 5. RUNIC INPUT PROMPTS (Bottom-Left)
            // ==========================================
            GameObject promptsContainer = new GameObject("Input_Prompts");
            promptsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform promptsRect = promptsContainer.AddComponent<RectTransform>();
            promptsRect.anchorMin = new Vector2(0f, 0f);
            promptsRect.anchorMax = new Vector2(0f, 0f);
            promptsRect.pivot = new Vector2(0f, 0f);
            promptsRect.anchoredPosition = new Vector2(40f, 35f);
            promptsRect.sizeDelta = new Vector2(720f, 44f);

            HorizontalLayoutGroup hlg = promptsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image pMove = CreatePill(promptsContainer.transform, spritePill, "[A / D]", "MOVE", 115f);
            Image pJump = CreatePill(promptsContainer.transform, spritePill, "[SPACE]", "JUMP", 115f);
            Image pSun = CreatePill(promptsContainer.transform, spritePill, "[Q / E]", "SUNDIAL", 130f);
            Image pRewind = CreatePill(promptsContainer.transform, spritePill, "[HOLD R]", "REWIND", 135f);
            Image pEcho = CreatePill(promptsContainer.transform, spritePill, "[TAP F]", "ECHO", 115f);

            so.FindProperty("pillMoveBg").objectReferenceValue = pMove;
            so.FindProperty("pillJumpBg").objectReferenceValue = pJump;
            so.FindProperty("pillSundialBg").objectReferenceValue = pSun;
            so.FindProperty("pillRewindBg").objectReferenceValue = pRewind;
            so.FindProperty("pillEchoBg").objectReferenceValue = pEcho;

            // ==========================================
            // 6. TIME FRACTURE DEATH OVERLAY (Center)
            // ==========================================
            GameObject deathOverlay = new GameObject("Time_Fracture_Overlay");
            deathOverlay.transform.SetParent(canvasObj.transform, false);
            RectTransform deathRect = deathOverlay.AddComponent<RectTransform>();
            deathRect.anchorMin = Vector2.zero;
            deathRect.anchorMax = Vector2.one;
            deathRect.offsetMin = Vector2.zero;
            deathRect.offsetMax = Vector2.zero;
            CanvasGroup deathCg = deathOverlay.AddComponent<CanvasGroup>();
            deathCg.alpha = 0f;
            so.FindProperty("deathOverlayGroup").objectReferenceValue = deathCg;

            Image deathBg = deathOverlay.AddComponent<Image>();
            deathBg.sprite = spriteBar;
            deathBg.color = new Color(0.04f, 0.04f, 0.06f, 0.75f); // Deep obsidian freeze

            GameObject deathTitle = CreateText("Death_Title", deathOverlay.transform, "TIME FRACTURED", 46, TextAlignmentOptions.Center, new Color(0.96f, 0.25f, 0.37f, 1f));
            RectTransform dtRect = deathTitle.GetComponent<RectTransform>();
            dtRect.anchoredPosition = new Vector2(0f, 40f);
            dtRect.sizeDelta = new Vector2(600f, 60f);

            GameObject deathPrompt = CreateText("Death_Prompt", deathOverlay.transform, "[ HOLD R TO REWIND TIME ]", 26, TextAlignmentOptions.Center, Color.white);
            RectTransform dpRect = deathPrompt.GetComponent<RectTransform>();
            dpRect.anchoredPosition = new Vector2(0f, -30f);
            dpRect.sizeDelta = new Vector2(600f, 40f);
            so.FindProperty("deathPromptText").objectReferenceValue = deathPrompt.GetComponent<TextMeshProUGUI>();

            // ==========================================
            // 7. LEVEL TITLE CARD (Top-Center)
            // ==========================================
            GameObject titleCard = new GameObject("Level_Title_Card");
            titleCard.transform.SetParent(canvasObj.transform, false);
            RectTransform titleCardRect = titleCard.AddComponent<RectTransform>();
            titleCardRect.anchorMin = new Vector2(0.5f, 0.82f);
            titleCardRect.anchorMax = new Vector2(0.5f, 0.82f);
            titleCardRect.pivot = new Vector2(0.5f, 0.5f);
            titleCardRect.sizeDelta = new Vector2(700f, 90f);
            CanvasGroup titleCg = titleCard.AddComponent<CanvasGroup>();
            so.FindProperty("levelTitleGroup").objectReferenceValue = titleCg;

            GameObject titleTextObj = CreateText("Title_Text", titleCard.transform, "UMBRA CHRONOS", 34, TextAlignmentOptions.Center, Color.white);
            RectTransform ttRect = titleTextObj.GetComponent<RectTransform>();
            ttRect.anchoredPosition = new Vector2(0f, 18f);
            ttRect.sizeDelta = new Vector2(700f, 45f);
            so.FindProperty("levelTitleText").objectReferenceValue = titleTextObj.GetComponent<TextMeshProUGUI>();

            GameObject subTextObj = CreateText("Subtitle_Text", titleCard.transform, "Light is Lethal. Shadows are Matter.", 18, TextAlignmentOptions.Center, new Color(0.22f, 0.74f, 0.97f, 0.9f));
            RectTransform stRect = subTextObj.GetComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(0f, -20f);
            stRect.sizeDelta = new Vector2(700f, 30f);
            so.FindProperty("levelSubtitleText").objectReferenceValue = subTextObj.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabPath = $"{PrefabCoreFolder}/HUD.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            Object.DestroyImmediate(canvasObj);
            AssetDatabase.SaveAssets();

            Debug.Log($"[HUDBuilder] Successfully generated production HUD prefab at {prefabPath}");
            return prefab;
        }

        private static Image CreatePill(Transform parent, Sprite sprite, string key, string label, float width)
        {
            GameObject pill = new GameObject($"Pill_{label}");
            pill.transform.SetParent(parent, false);
            RectTransform rect = pill.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 42f);

            Image img = pill.AddComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.75f);

            GameObject textObj = CreateText("Pill_Text", pill.transform, $"<color=#38BDF8>{key}</color> {label}", 14, TextAlignmentOptions.Center, Color.white);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return img;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;

            return obj;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabCoreFolder)) Directory.CreateDirectory(PrefabCoreFolder);
            if (!Directory.Exists(SpriteUIFolder)) Directory.CreateDirectory(SpriteUIFolder);
        }

        private static void GenerateUISprites()
        {
            CreateCircleTexture($"{SpriteUIFolder}/Sprite_Dial_128.png", 128, 6f);
            CreateNeedleTexture($"{SpriteUIFolder}/Sprite_Needle_32.png", 32, 128);
            CreatePillTexture($"{SpriteUIFolder}/Sprite_Pill_128.png", 128, 48, 18);
            CreateSolidTexture($"{SpriteUIFolder}/Sprite_Bar_White.png", 32, 32, Color.white);
            CreateSunTexture($"{SpriteUIFolder}/Sprite_Sun_64.png", 64);
            CreateTapeTexture($"{SpriteUIFolder}/Sprite_Tape_64.png", 64);
            CreateVignetteTexture($"{SpriteUIFolder}/Sprite_Danger_Vignette_256.png", 256);

            AssetDatabase.Refresh();
        }

        private static void CreateCircleTexture(string path, int size, float ringThickness)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = (size * 0.5f) - 4f;
            float inner = radius - ringThickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius && dist >= inner)
                    {
                        float edgeAlpha = Mathf.Clamp01(radius - dist) * Mathf.Clamp01(dist - inner);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(edgeAlpha * 1.5f)));
                    }
                    else if (dist < inner)
                    {
                        tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.12f, 0.65f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreateNeedleTexture(string path, int width, int height)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float halfW = width * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                float allowedW = (1f - t) * (halfW - 2f) + 2f;
                for (int x = 0; x < width; x++)
                {
                    float dist = Mathf.Abs(x - halfW);
                    if (dist <= allowedW)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreatePillTexture(string path, int width, int height, int radius)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int clampX = Mathf.Clamp(x, radius, width - 1 - radius);
                    int clampY = Mathf.Clamp(y, radius, height - 1 - radius);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(clampX, clampY));

                    if (dist <= radius)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreateSolidTexture(string path, int width, int height, Color color)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] cols = new Color[width * height];
            for (int i = 0; i < cols.Length; i++) cols[i] = color;
            tex.SetPixels(cols);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreateSunTexture(string path, int size)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float r = (size * 0.5f) - 6f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= r)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreateTapeTexture(string path, int size)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = (size * 0.5f) - 4f;
            float innerR = outerR * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= outerR && dist >= innerR)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void CreateVignetteTexture(string path, int size)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.SmoothStep(0.2f, 1.0f, dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
    }
}
#endif
