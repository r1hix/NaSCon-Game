#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UmbraChronos.Player;
using UmbraChronos.Time;
using UmbraChronos.Environment;
using UmbraChronos.Core;
using UmbraChronos.UI;

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Programmatic builder for Level_Tutorial.unity — a gentle, guided introduction
    /// to all core mechanics of Umbra Chronos with in-world animated tutorial prompts.
    ///
    /// Layout: 3 connected rooms (screen-shift) that teach mechanics progressively:
    ///   Room 0 (Tutorial_Move):   Movement (A/D) + Jump (Space) — safe, no hazards.
    ///   Room 1 (Tutorial_SunDial): SunDial rotation (Q/E) + Shadow Bridge walking — first light hazard.
    ///   Room 2 (Tutorial_Echo):    Rewind (R/Shift) + Echo (F/C) + Pressure Plate — full mechanic combo.
    ///
    /// Each room contains floating tutorial prompt cards that explain what to do.
    /// </summary>
    public static class TutorialBuilder
    {
        private const string LevelsFolder = "Assets/_Project/Scenes/Levels";
        private const string PrefabEnvFolder = "Assets/_Project/Prefabs/Environment";
        private const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        private const string PrefabInteractFolder = "Assets/_Project/Prefabs/Interactables";
        private const string PrefabPlayerFolder = "Assets/_Project/Prefabs/Player";
        private const string SpriteFolder = "Assets/_Project/Sprites";

        // Room grid: 24u wide × 15u high, matching camera ortho size 7.5
        private const float RoomWidth = 24f;
        private const float RoomHeight = 15f;
        private static readonly Vector2 RoomOrigin = new Vector2(0f, 1.0f);

        [MenuItem("Umbra Chronos/Build Tutorial Level (Level_Tutorial.unity)")]
        public static void BuildTutorialLevel()
        {
            AssetDatabase.Refresh();
            EnsureDirectory(LevelsFolder);

            Sprite squareSprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ─────────────────────────────────────────────────────────────
            // 1. CAMERA — Screen-shift across 3 tutorial rooms
            // ─────────────────────────────────────────────────────────────
            SetupCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.02f, 0.03f, 0.06f), new Vector2(RoomWidth, RoomHeight), RoomOrigin);

            // ─────────────────────────────────────────────────────────────
            // 2. ATMOSPHERIC BACKDROP — Gentle introductory twilight
            // ─────────────────────────────────────────────────────────────
            SetupAtmosphere(squareSprite, gradientSprite, pillarSprite, archSprite, mistSprite);

            // ─────────────────────────────────────────────────────────────
            // 3. SUNDIAL — Camera-following celestial sun
            // ─────────────────────────────────────────────────────────────
            GameObject sunInst = SetupSunDial(squareSprite, new Vector2(0f, 0f), 15f, 90f);

            // ─────────────────────────────────────────────────────────────
            // 4. POST-PROCESSING & LETHAL LIGHT
            // ─────────────────────────────────────────────────────────────
            SetupPostProcessing(sunInst);

            // Prefab references
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            // ═════════════════════════════════════════════════════════════
            // ROOM 0: MOVEMENT & JUMP TUTORIAL
            // Center: (0, 1), Bounds: X [-12, 12]
            // Fully sheltered — no lethal light. Safe playground.
            // ═════════════════════════════════════════════════════════════

            // Full ceiling over Room 0 — completely safe from sunlight
            GameObject r0Ceiling = CreatePlatform("R0_Ceiling", new Vector3(0f, 6.0f, 0f), new Vector3(26f, 1.5f, 1f), squareSprite);
            AddInteriorBackdrop(r0Ceiling, -2.0f, 0.5f);

            // Ground floor spanning Room 0
            CreatePlatform("R0_Floor", new Vector3(0f, -2.5f, 0f), new Vector3(26f, 1.5f, 1f), squareSprite);

            // Left boundary wall
            CreatePlatform("R0_Wall_Left", new Vector3(-13.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);

            // Small stepping platforms to teach jumping
            CreatePlatform("R0_Step_1", new Vector3(-2.0f, -0.4f, 0f), new Vector3(2.5f, 0.5f, 1f), squareSprite);
            CreatePlatform("R0_Step_2", new Vector3(2.0f, 0.8f, 0f), new Vector3(2.5f, 0.5f, 1f), squareSprite);
            CreatePlatform("R0_Step_3", new Vector3(6.0f, -0.4f, 0f), new Vector3(2.5f, 0.5f, 1f), squareSprite);

            // Small gap in the floor (harmless, just to practice jumping over)
            // Remove a section of the main floor and add a lower chasm with a catch platform
            CreatePlatform("R0_Gap_Floor_Left", new Vector3(-6.0f, -2.5f, 0f), new Vector3(5f, 1.5f, 1f), squareSprite);

            // Glow lichens for ambient mood
            CreateGlowLichen(lichenA, new Vector3(-11.0f, -1.6f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(0f, 5.6f, 0f), new Vector3(1.3f, 1.3f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(8.0f, -1.6f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            // ═════════════════════════════════════════════════════════════
            // ROOM 1: SUNDIAL & SHADOW BRIDGE TUTORIAL
            // Center: (24, 1), Bounds: X [12, 36]
            // Partially open sky — first encounter with lethal sunlight.
            // Player must rotate the sundial to cast a shadow bridge.
            // ═════════════════════════════════════════════════════════════

            // Left cliff (sheltered entry from Room 0)
            CreatePlatform("R1_Cliff_Left", new Vector3(16.0f, -2.5f, 0f), new Vector3(8f, 1.5f, 1f), squareSprite);
            GameObject r1Shelter = CreatePlatform("R1_Shelter_Left", new Vector3(16.0f, 1.5f, 0f), new Vector3(7f, 0.6f, 1f), squareSprite);
            AddInteriorBackdrop(r1Shelter, -1.75f, 0.5f);

            // Monolith casting shadow bridge across chasm
            CreateMonolithObject(squareSprite, new Vector3(20.0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Right plateau (destination)
            GameObject r1PlateauRight = CreatePlatform("R1_Plateau_Right", new Vector3(32.0f, -2.5f, 0f), new Vector3(8f, 1.5f, 1f), squareSprite);
            AddSunlitSheen(r1PlateauRight, sunInst.transform);

            // Right shelter (safe exit zone)
            GameObject r1ShelterRight = CreatePlatform("R1_Shelter_Right", new Vector3(32.0f, 1.5f, 0f), new Vector3(6f, 0.6f, 1f), squareSprite);
            AddInteriorBackdrop(r1ShelterRight, -1.75f, 0.5f);

            // Chasm floor (catch for falling players)
            CreatePlatform("R1_Chasm_Floor", new Vector3(24f, -7.0f, 0f), new Vector3(26f, 2f, 1f), squareSprite);

            // Glow lichens
            CreateGlowLichen(lichenA, new Vector3(13.5f, -1.6f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(20.0f, -1.6f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(33.0f, -1.6f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            // ═════════════════════════════════════════════════════════════
            // ROOM 2: REWIND & ECHO TUTORIAL
            // Center: (48, 1), Bounds: X [36, 60]
            // Sealed vault with ceiling — player must use Rewind + Echo
            // to hold a pressure plate and pass through a timed gate.
            // ═════════════════════════════════════════════════════════════

            // Full vault ceiling
            GameObject r2Ceiling = CreatePlatform("R2_Vault_Ceiling", new Vector3(48f, 5.5f, 0f), new Vector3(26f, 1.0f, 1f), squareSprite);
            AddInteriorBackdrop(r2Ceiling, -1.75f, 0.8f);

            // Vault floor (left half)
            CreatePlatform("R2_Floor_Left", new Vector3(42f, -2.5f, 0f), new Vector3(12f, 1.5f, 1f), squareSprite);

            // Step ledge to reach the plate balcony
            CreatePlatform("R2_Step_1", new Vector3(39.5f, -0.90f, 0f), new Vector3(2.4f, 0.4f, 1f), squareSprite);

            // Plate balcony
            CreatePlatform("R2_Plate_Balcony", new Vector3(43.0f, 0.15f, 0f), new Vector3(4.5f, 0.4f, 1f), squareSprite);

            // Timed Gate blocking passage forward
            GameObject tutGate = null;
            if (gatePrefab != null)
            {
                tutGate = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
                tutGate.name = "TimedGate_Tutorial";
                tutGate.transform.position = new Vector3(48.5f, -0.25f, 0f);
            }

            // Pressure Plate on the balcony
            if (platePrefab != null && tutGate != null)
            {
                GameObject plate = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
                plate.name = "PressurePlate_Tutorial";
                plate.transform.position = new Vector3(43.0f, 0.50f, 0f);

                var plateComp = plate.GetComponent<PressurePlate>();
                if (plateComp != null)
                {
                    SerializedObject so = new SerializedObject(plateComp);
                    so.FindProperty("targetGate").objectReferenceValue = tutGate.GetComponent<TimedGate>();
                    so.ApplyModifiedProperties();
                }
            }

            // Vault floor (right half, behind the gate)
            CreatePlatform("R2_Floor_Right", new Vector3(54f, -2.5f, 0f), new Vector3(12f, 1.5f, 1f), squareSprite);

            // Right boundary wall
            CreatePlatform("R2_Wall_Right", new Vector3(60.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);

            // Sanctuary Arch at the end — victory!
            CreateSanctuaryArch(squareSprite, new Vector3(57.5f, -0.4f, 0f));

            // Glow lichens
            CreateGlowLichen(lichenA, new Vector3(37.0f, -1.6f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(43.0f, 4.8f, 0f), new Vector3(1.4f, 1.4f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.65f, 3.8f);
            CreateGlowLichen(lichenB, new Vector3(55.0f, -1.6f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(48.0f, -1.6f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.50f, 3.0f);

            // ─────────────────────────────────────────────────────────────
            // 5. PLAYER SPAWN (Inside Room 0 shelter at safe position)
            // ─────────────────────────────────────────────────────────────
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            // ─────────────────────────────────────────────────────────────
            // 6. TUTORIAL MANAGER — In-world animated text prompts
            // ─────────────────────────────────────────────────────────────
            SetupTutorialPrompts();

            // ─────────────────────────────────────────────────────────────
            // SAVE
            // ─────────────────────────────────────────────────────────────
            string path = $"{LevelsFolder}/Level_Tutorial.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[TutorialBuilder] Saved Tutorial Level to {path}");

            // Add to Build Settings
            AddToBuildSettings(path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Tutorial Level Built!",
                    $"Successfully generated {path} with:\n" +
                    "- Room 0: Movement & Jump (fully sheltered)\n" +
                    "- Room 1: SunDial & Shadow Bridge (first light hazard)\n" +
                    "- Room 2: Rewind & Echo & Pressure Plate\n" +
                    "- 8 animated in-world tutorial prompts\n\n" +
                    "Ready for playtest!",
                    "Let's Play!");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // TUTORIAL PROMPT SETUP
        // ═══════════════════════════════════════════════════════════════

        private static void SetupTutorialPrompts()
        {
            GameObject tutObj = new GameObject("[TutorialManager]");
            var tutMgr = tutObj.AddComponent<TutorialManager>();

            // ───── ROOM 0: Movement & Jump ─────

            // Prompt 0: Welcome + Move
            tutMgr.RegisterPrompt(
                index: 0,
                header: "WELCOME, WANDERER",
                body: "Light is lethal. Shadows are matter.\nMove through the darkness to survive.",
                keyHint: "[ A ]  Move Left     [ D ]  Move Right",
                worldPos: new Vector3(-8.0f, 1.5f, 0f),
                radius: 5f,
                duration: 8f,
                requiredPrior: -1
            );

            // Prompt 1: Jump
            tutMgr.RegisterPrompt(
                index: 1,
                header: "LEAP OF SHADOW",
                body: "Jump to scale ancient ruins.\nHold briefly for a higher arc.",
                keyHint: "[ SPACE ]  Jump",
                worldPos: new Vector3(-2.0f, 2.2f, 0f),
                radius: 4f,
                duration: 0f,  // dismiss when player leaves
                requiredPrior: 0
            );

            // Prompt 2: Jump across platforms
            tutMgr.RegisterPrompt(
                index: 2,
                header: "TRAVERSE THE RUINS",
                body: "Leap between the stone steps.\nThe next chamber awaits beyond.",
                keyHint: "[ A / D ] + [ SPACE ]",
                worldPos: new Vector3(4.0f, 3.0f, 0f),
                radius: 5f,
                duration: 0f,
                requiredPrior: 1
            );

            // ───── ROOM 1: SunDial & Shadow Bridge ─────

            // Prompt 3: The world rule (entering Room 1)
            tutMgr.RegisterPrompt(
                index: 3,
                header: "CELESTIAL DANGER",
                body: "The open sky burns with lethal radiation.\nStay under cover — direct sunlight is fatal.",
                keyHint: "",
                worldPos: new Vector3(16.0f, 3.0f, 0f),
                radius: 5f,
                duration: 7f,
                requiredPrior: 2
            );

            // Prompt 4: SunDial rotation
            tutMgr.RegisterPrompt(
                index: 4,
                header: "THE SUNDIAL",
                body: "Rotate the celestial sun to shift shadows.\nMonolith shadows become solid bridges.",
                keyHint: "[ Q ]  Rotate Left     [ E ]  Rotate Right",
                worldPos: new Vector3(20.0f, 3.5f, 0f),
                radius: 4f,
                duration: 0f,
                requiredPrior: 3
            );

            // Prompt 5: Walk on shadow (appears after rotating sundial)
            tutMgr.RegisterPrompt(
                index: 5,
                header: "WALK THE SHADOW",
                body: "The monolith's shadow is now a solid bridge.\nCross the lethal chasm on its surface.",
                keyHint: "Rotate sun to cast shadow → walk across",
                worldPos: new Vector3(26.0f, 3.0f, 0f),
                radius: 6f,
                duration: 0f,
                requiredPrior: 4,
                requiresSunDial: true
            );

            // ───── ROOM 2: Rewind & Echo ─────

            // Prompt 6: Rewind introduction
            tutMgr.RegisterPrompt(
                index: 6,
                header: "TAPE-REEL REWIND",
                body: "Hold to rewind time itself.\nYour body retraces its path backwards.",
                keyHint: "[ HOLD R ]  or  [ HOLD SHIFT ]",
                worldPos: new Vector3(40.0f, 3.0f, 0f),
                radius: 5f,
                duration: 0f,
                requiredPrior: 5
            );

            // Prompt 7: Echo + Plate puzzle
            tutMgr.RegisterPrompt(
                index: 7,
                header: "UMBRAL ECHO",
                body: "After rewinding, deploy your shadow clone.\nIt replays your past actions — hold the plate\nwhile you pass through the gate.",
                keyHint: "Stand on plate → [ HOLD R ] to rewind → [ F ] Echo",
                worldPos: new Vector3(43.0f, 3.0f, 0f),
                radius: 4f,
                duration: 0f,
                requiredPrior: 6,
                requiresRewind: true
            );
        }

        // ═══════════════════════════════════════════════════════════════
        // SHARED HELPER METHODS
        // (Mirrors from LevelBuilder to keep TutorialBuilder self-contained
        //  and avoid cross-file dependencies with the other agent's work)
        // ═══════════════════════════════════════════════════════════════

        private static void SetupCamera(Vector3 pos, float orthoSize, Color bgColor, Vector2 roomDims, Vector2 roomOrigin)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = pos;

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.backgroundColor = bgColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();

            var addData = camObj.AddComponent<UniversalAdditionalCameraData>();
            if (addData != null) addData.renderPostProcessing = true;

            var camCtrl = camObj.AddComponent<CameraController>();
            camCtrl.ConfigureRoom(roomDims, roomOrigin, 0.35f);
        }

        private static void SetupAtmosphere(Sprite squareSprite, Sprite gradientSprite, Sprite pillarSprite, Sprite archSprite, Sprite mistSprite)
        {
            // Background canvas spanning all 3 tutorial rooms
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(24f, 3.0f, 0f);
            bgWall.transform.localScale = new Vector3(100f, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = gradientSprite != null ? gradientSprite : squareSprite;
            bgSr.sortingOrder = -20;

            // Distant ruin silhouettes
            Color distantRuinColor = new Color(0.06f, 0.08f, 0.16f, 0.85f);
            if (pillarSprite != null)
            {
                CreateBackgroundDecor(pillarSprite, new Vector3(-6f, 1.5f, 0f), new Vector3(1.5f, 3.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(12f, 1.5f, 0f), new Vector3(1.5f, 3.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(30f, 2.0f, 0f), new Vector3(1.8f, 4.0f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(50f, 2.0f, 0f), new Vector3(2.0f, 4.5f, 1f), distantRuinColor, -15);
            }

            if (archSprite != null)
            {
                CreateBackgroundDecor(archSprite, new Vector3(20f, 0.5f, 0f), new Vector3(2.2f, 3.2f, 1f), distantRuinColor, -14);
                CreateBackgroundDecor(archSprite, new Vector3(44f, 0.5f, 0f), new Vector3(2.5f, 3.5f, 1f), distantRuinColor, -14);
            }

            // Chasm mist
            if (mistSprite != null)
            {
                CreateBackgroundDecor(mistSprite, new Vector3(24f, -4.5f, 0f), new Vector3(25f, 4f, 1f), Color.white, -8);
            }

            // Global ambient light
            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.24f, 0.28f, 0.38f, 1.0f);
            light2D.intensity = 0.45f;
        }

        private static GameObject SetupSunDial(Sprite sprite, Vector2 center, float radius, float startAngle)
        {
            GameObject sunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/SunDial.prefab");
            GameObject sunInst = sunPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(sunPrefab) : new GameObject("SunDial");

            var dial = sunInst.GetComponent<SunDialController>();
            if (dial != null)
            {
                SerializedObject so = new SerializedObject(dial);
                so.FindProperty("orbitCenter").vector2Value = center;
                so.FindProperty("orbitRadius").floatValue = radius;
                so.FindProperty("currentAngle").floatValue = startAngle;
                so.FindProperty("followCameraX").boolValue = true;
                so.ApplyModifiedProperties();
            }

            return sunInst;
        }

        private static void SetupPostProcessing(GameObject sunInst)
        {
            GameObject postPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/PostProcessingVolume.prefab");
            if (postPrefab != null) PrefabUtility.InstantiatePrefab(postPrefab);

            GameObject lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/LethalLightZone.prefab");
            if (lightPrefab != null)
            {
                GameObject lightInst = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab);
                var lightComp = lightInst.GetComponent<LethalLightZone>();
                if (lightComp != null && sunInst != null)
                {
                    SerializedObject so = new SerializedObject(lightComp);
                    so.FindProperty("sunTransform").objectReferenceValue = sunInst.transform;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void SetupPlayer(Vector3 spawnPos)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPlayerFolder}/Player.prefab");
            if (playerPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInst.transform.position = spawnPos;
            }

            GameObject rewindPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/RewindManager.prefab");
            if (rewindPrefab != null) PrefabUtility.InstantiatePrefab(rewindPrefab);

            GameObject audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/AudioManager.prefab");
            if (audioPrefab != null) PrefabUtility.InstantiatePrefab(audioPrefab);

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabCoreFolder}/HUD.prefab");
            if (hudPrefab == null) hudPrefab = HUDBuilder.GenerateHUDPrefab();
            if (hudPrefab != null) PrefabUtility.InstantiatePrefab(hudPrefab);
        }

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Sprite sprite)
        {
            GameObject plat = new GameObject(name);
            plat.transform.position = pos;
            plat.transform.localScale = scale;

            var sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.06f, 0.06f, 0.10f, 1.0f); // Pure obsidian stone
            sr.sortingOrder = 0;

            var col = plat.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            return plat;
        }

        private static GameObject CreateMonolithObject(Sprite sprite, Vector3 pos, Vector2 size, float groundY, Transform sunTransform, float lengthMultiplier = 1.0f)
        {
            GameObject monolith = new GameObject("Monolith");
            monolith.transform.position = pos;
            monolith.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = monolith.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.04f, 0.04f, 0.07f, 1f);
            sr.sortingOrder = 2;

            var box = monolith.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;

            monolith.AddComponent<EdgeCollider2D>();
            var shadowCollider = monolith.AddComponent<DynamicShadowCollider>();
            SerializedObject so = new SerializedObject(shadowCollider);
            so.FindProperty("fallbackGroundY").floatValue = groundY;
            so.FindProperty("shadowLengthMultiplier").floatValue = lengthMultiplier;
            if (sunTransform != null) so.FindProperty("sunTransform").objectReferenceValue = sunTransform;
            so.ApplyModifiedProperties();

            return monolith;
        }

        private static void CreateSanctuaryArch(Sprite sprite, Vector3 pos)
        {
            GameObject arch = new GameObject("Sanctuary_Arch");
            arch.transform.position = pos;
            arch.transform.localScale = new Vector3(2.8f, 3.8f, 1f);

            var sr = arch.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.85f, 0.4f, 0.95f);
            sr.sortingOrder = -5;

            var trigger = arch.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one;
            trigger.isTrigger = true;

            var goal = arch.AddComponent<LevelGoal>();
            SerializedObject so = new SerializedObject(goal);
            so.FindProperty("chamberName").stringValue = "Tutorial Completed";
            so.FindProperty("nextSceneName").stringValue = "Level_01";
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateGlowLichen(Sprite sprite, Vector3 pos, Vector3 scale, Color glowColor, float intensity = 0.55f, float radius = 3.2f)
        {
            GameObject lichen = new GameObject("Glow_Lichen");
            lichen.transform.position = pos;
            lichen.transform.localScale = scale;

            var sr = lichen.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : GetOrCreateSquareSprite();
            sr.color = glowColor;
            sr.sortingOrder = 1;

            GameObject lightChild = new GameObject("Lichen_PointLight");
            lightChild.transform.SetParent(lichen.transform, false);
            lightChild.transform.localPosition = Vector3.zero;

            var pointLight = lightChild.AddComponent<Light2D>();
            pointLight.lightType = Light2D.LightType.Point;
            pointLight.pointLightInnerRadius = 0.25f;
            pointLight.pointLightOuterRadius = radius;
            pointLight.color = glowColor;
            pointLight.intensity = intensity;
            pointLight.falloffIntensity = 0.65f;

            var comp = lichen.AddComponent<GlowLichen>();
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("pointLight").objectReferenceValue = pointLight;
            so.FindProperty("glowColor").colorValue = glowColor;
            so.FindProperty("baseIntensity").floatValue = intensity;
            so.FindProperty("lightRadius").floatValue = radius;
            so.ApplyModifiedProperties();

            return lichen;
        }

        private static GameObject CreateBackgroundDecor(Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
        {
            GameObject decor = new GameObject("Bg_Decor");
            decor.transform.position = pos;
            decor.transform.localScale = scale;
            var sr = decor.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return decor;
        }

        private static void AddSunlitSheen(GameObject platform, Transform sunTransform)
        {
            if (platform == null) return;
            var sheen = platform.AddComponent<SunlitGroundSheen>();
            SerializedObject so = new SerializedObject(sheen);
            if (sunTransform != null) so.FindProperty("sunTransform").objectReferenceValue = sunTransform;
            so.ApplyModifiedProperties();
        }

        private static void AddInteriorBackdrop(GameObject roof, float floorY, float padding = 0.5f)
        {
            if (roof == null) return;
            var backdrop = roof.AddComponent<InteriorBackdrop>();
            SerializedObject so = new SerializedObject(backdrop);
            so.FindProperty("floorY").floatValue = floorY;
            so.FindProperty("horizontalPadding").floatValue = padding;
            so.ApplyModifiedProperties();
        }

        private static void AddToBuildSettings(string tutorialPath)
        {
            var existing = EditorBuildSettings.scenes;
            var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing);

            // Check if already present
            bool found = false;
            for (int i = 0; i < sceneList.Count; i++)
            {
                if (sceneList[i].path == tutorialPath)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                sceneList.Add(new EditorBuildSettingsScene(tutorialPath, true));
                EditorBuildSettings.scenes = sceneList.ToArray();
            }
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static Sprite GetOrCreateSquareSprite()
        {
            string path = $"{SpriteFolder}/Square_32.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (allAssets != null)
            {
                for (int i = 0; i < allAssets.Length; i++)
                {
                    if (allAssets[i] is Sprite spriteObj)
                    {
                        return spriteObj;
                    }
                }
            }
            return null;
        }
    }
}
#endif
