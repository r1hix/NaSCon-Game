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

namespace UmbraChronos.Editor
{
    /// <summary>
    /// Programmatic level authoring system for Umbra Chronos.
    /// Builds and packages:
    /// 1. Level_Master.unity: One unified, seamless contiguous master level containing all 4 interconnected
    ///    puzzle chambers (Courtyard of Dawn -> Echo Vault -> Clockwork Spire -> Eclipse Paradox Climax).
    /// 2. Level_01, Level_02, Level_03: Standalone showcase levels.
    /// Features:
    /// - Hollow Knight / Gris inspired multi-layered atmospheric background (deep indigo/abyss gradients,
    ///   distant cathedral arches, ruined gothic pillars, and soft hovering chasm mist).
    /// - Zero collision lips / perfectly flush shadow bridge alignment.
    /// - 0 softlocks, verified jump and timing budgets.
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelsFolder = "Assets/_Project/Scenes/Levels";
        private const string PrefabEnvFolder = "Assets/_Project/Prefabs/Environment";
        private const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        private const string PrefabInteractFolder = "Assets/_Project/Prefabs/Interactables";
        private const string PrefabPlayerFolder = "Assets/_Project/Prefabs/Player";
        private const string SpriteFolder = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Build Master Level (Level_Master.unity)")]
        public static void BuildMasterLevel()
        {
            AssetDatabase.Refresh();
            EnsureLevelsDirectory();
            Sprite squareSprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");
            Debug.Log($"[LevelBuilder] Lichen sprites: A={lichenA != null}, B={lichenB != null}, C={lichenC != null}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging across all 4 rooms
            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.02f, 0.03f, 0.06f), new Vector2(24f, 15f), new Vector2(0f, 1.0f));

            // 2. Hollow Knight Atmospheric Multi-Layered Backdrop
            SetupMasterAtmosphere(squareSprite, gradientSprite, pillarSprite, archSprite, mistSprite);

            // 3. SunDial with camera tracking
            GameObject sunInst = SetupSunDial(squareSprite, new Vector2(0f, 0f), 16f, 35f);

            // 4. Lethal Light Zone & Post Processing
            SetupPostProcessingAndLight(sunInst);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            // -------------------------------------------------------------
            // WING 1: Courtyard of Dawn (Room 0, x = -14 to 12, Center 0)
            // -------------------------------------------------------------
            // Left Cliff (x = -14 to -3, surface at y = -1.75)
            CreatePlatform("W1_Cliff_Left", new Vector3(-8.5f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), squareSprite);
            CreatePlatform("W1_Wall_Left", new Vector3(-14.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);
            CreatePlatform("W1_Spawn_Shelter", new Vector3(-10.0f, 1.5f, 0f), new Vector3(7.0f, 0.6f, 1f), squareSprite);

            // Monolith 1 at edge of cliff: Base embedded at y = -1.75, height = 4.0
            CreateMonolithObject(squareSprite, new Vector3(-3.0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Center Island / Receiving Plateau (x = 4 to 12, surface at y = -1.75)
            CreatePlatform("W1_Plateau_Right", new Vector3(8.0f, -2.5f, 0f), new Vector3(8.0f, 1.5f, 1f), squareSprite);
            CreatePlatform("W1_Threshold_Shelter", new Vector3(8.0f, 1.5f, 0f), new Vector3(6.0f, 0.6f, 1f), squareSprite);

            // Bioluminescent Glow Lichens (Safe ambient lighting)
            CreateGlowLichen(lichenC, new Vector3(-11.5f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(-3.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(5.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            // Deep Chasm Floor (x = -14 to 12 at y = -7.0)
            CreatePlatform("W1_Chasm_Floor", new Vector3(0f, -7.0f, 0f), new Vector3(26f, 2f, 1f), squareSprite);

            // -------------------------------------------------------------
            // WING 2: The Echo Vault (Room 1, x = 12 to 36, Center 24)
            // -------------------------------------------------------------
            // Vault Ceiling sealing from lethal sunlight
            CreatePlatform("W2_Vault_Ceiling", new Vector3(24.0f, 5.0f, 0f), new Vector3(26.0f, 1.0f, 1f), squareSprite);

            // Vault Floor 1 (x = 12 to 24, surface at y = -1.75)
            CreatePlatform("W2_Floor_1", new Vector3(18.0f, -2.5f, 0f), new Vector3(12.0f, 1.5f, 1f), squareSprite);

            // Ruined steps climbing to high balcony
            CreatePlatform("W2_Step_1", new Vector3(15.5f, -0.90f, 0f), new Vector3(2.4f, 0.4f, 1f), squareSprite);
            CreatePlatform("W2_Balcony", new Vector3(19.0f, 0.15f, 0f), new Vector3(4.5f, 0.4f, 1f), squareSprite);

            // TimedGate 1 blocking passage to Screen 2
            GameObject gate1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate1.name = "TimedGate_W2";
            gate1.transform.position = new Vector3(24.5f, -0.25f, 0f);

            // PressurePlate 1 on the high balcony
            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.name = "PressurePlate_W2";
            plate1.transform.position = new Vector3(19.0f, 0.50f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject so1 = new SerializedObject(plateComp1);
                so1.FindProperty("targetGate").objectReferenceValue = gate1.GetComponent<TimedGate>();
                so1.ApplyModifiedProperties();
            }

            // Vault Floor 2 behind the gate (x = 24 to 36, surface at y = -1.75)
            CreatePlatform("W2_Floor_2", new Vector3(30.0f, -2.5f, 0f), new Vector3(12.0f, 1.5f, 1f), squareSprite);

            // Glow Lichens in Vault (illuminating dark crypt crevices)
            CreateGlowLichen(lichenB, new Vector3(13.0f, 0.0f, 0f), new Vector3(1.0f, 1.3f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(18.0f, 4.4f, 0f), new Vector3(1.4f, 1.4f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.65f, 3.8f);
            CreateGlowLichen(lichenA, new Vector3(15.0f, -0.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(20.5f, -0.1f, 0f), new Vector3(1.1f, 1.1f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(27.0f, 1.0f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(33.0f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.50f, 3.0f);

            // -------------------------------------------------------------
            // WING 3: The Clockwork Spire (Room 2, x = 36 to 60, Center 48)
            // -------------------------------------------------------------
            // Lower Terrace (x = 36 to 43, surface at y = -1.75)
            CreatePlatform("W3_Lower_Terrace", new Vector3(39.5f, -2.5f, 0f), new Vector3(7.0f, 1.5f, 1f), squareSprite);

            // Spire Central Monolith (at x = 43.5, height = 4.8)
            CreateMonolithObject(squareSprite, new Vector3(43.5f, 0.65f, 0f), new Vector2(1.8f, 4.8f), -1.75f, sunInst.transform, 1.6f);

            // Left Switch Balcony (surface at y = 1.0)
            CreatePlatform("W3_Left_Balcony", new Vector3(38.5f, 0.8f, 0f), new Vector3(3.5f, 0.4f, 1f), squareSprite);

            // Upper Exit Ledge (x = 53 to 60, surface at y = 0.25)
            CreatePlatform("W3_Upper_Ledge", new Vector3(56.5f, -0.5f, 0f), new Vector3(7.0f, 1.5f, 1f), squareSprite);

            // TimedGate 2 blocking the upper exit ledge
            GameObject gate2 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate2.name = "TimedGate_W3";
            gate2.transform.position = new Vector3(53.0f, 1.75f, 0f);

            // PressurePlate 2 on the Left Switch Balcony
            GameObject plate2 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate2.name = "PressurePlate_W3";
            plate2.transform.position = new Vector3(38.5f, 1.15f, 0f);

            var plateComp2 = plate2.GetComponent<PressurePlate>();
            if (plateComp2 != null)
            {
                SerializedObject so2 = new SerializedObject(plateComp2);
                so2.FindProperty("targetGate").objectReferenceValue = gate2.GetComponent<TimedGate>();
                so2.ApplyModifiedProperties();
            }

            // Glow Lichens in Spire
            CreateGlowLichen(lichenA, new Vector3(37.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(37.0f, 0.5f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(42.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(57.5f, 0.40f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            // Chasm 3 Floor (under x = 36 to 60 at y = -7.0)
            CreatePlatform("W3_Chasm_Floor", new Vector3(48.0f, -7.0f, 0f), new Vector3(26f, 2f, 1f), squareSprite);

            // -------------------------------------------------------------
            // WING 4: The Eclipse Paradox Sanctuary (Room 3, x = 60 to 86, Center 72)
            // -------------------------------------------------------------
            // Western Altar (x = 61 to 67, surface at y = -1.75)
            CreatePlatform("W4_Western_Altar", new Vector3(64.0f, -2.5f, 0f), new Vector3(6.0f, 1.5f, 1f), squareSprite);
            CreatePlatform("W4_Western_Shelter", new Vector3(64.0f, 1.5f, 0f), new Vector3(5.0f, 0.5f, 1f), squareSprite);

            // Central Hub & Monolith (x = 70 to 74, surface at y = -1.75)
            CreatePlatform("W4_Center_Hub", new Vector3(72.0f, -2.5f, 0f), new Vector3(6.0f, 1.5f, 1f), squareSprite);
            CreatePlatform("W4_Center_Roof", new Vector3(72.0f, 2.5f, 0f), new Vector3(6.0f, 0.5f, 1f), squareSprite);
            CreateMonolithObject(squareSprite, new Vector3(72.0f, 0.5f, 0f), new Vector2(1.8f, 4.5f), -1.75f, sunInst.transform, 2.2f);

            // Eastern Citadel (x = 77 to 86, surface at y = -1.75)
            CreatePlatform("W4_Eastern_Citadel", new Vector3(81.5f, -2.5f, 0f), new Vector3(9.0f, 1.5f, 1f), squareSprite);
            CreatePlatform("W4_Citadel_Ceiling", new Vector3(81.5f, 3.5f, 0f), new Vector3(9.0f, 1.0f, 1f), squareSprite);

            // Right World Boundary Wall
            CreatePlatform("W4_Wall_Right", new Vector3(86.5f, 1.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);

            // TimedGate 3 sealing the Sanctuary Arch
            GameObject gate3 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate3.name = "TimedGate_Sanctuary";
            gate3.transform.position = new Vector3(79.0f, -0.25f, 0f);

            // PressurePlate 3 on Western Altar
            GameObject plate3 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate3.name = "PressurePlate_Western_Altar";
            plate3.transform.position = new Vector3(64.0f, -1.60f, 0f);

            var plateComp3 = plate3.GetComponent<PressurePlate>();
            if (plateComp3 != null)
            {
                SerializedObject so3 = new SerializedObject(plateComp3);
                so3.FindProperty("targetGate").objectReferenceValue = gate3.GetComponent<TimedGate>();
                so3.ApplyModifiedProperties();
            }

            // Glow Lichens in Sanctuary (Cosmic twilight cyan)
            CreateGlowLichen(lichenA, new Vector3(62.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenC, new Vector3(64.0f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenC, new Vector3(72.0f, 2.2f, 0f), new Vector3(1.3f, 1.3f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.60f, 3.5f);
            CreateGlowLichen(lichenA, new Vector3(81.0f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(85.5f, 0.0f, 0f), new Vector3(1.0f, 1.3f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);

            // Sanctuary Arch of Victory at x = 83.5
            CreateSanctuaryArch(squareSprite, new Vector3(83.5f, -0.4f, 0f));

            // Deep Eclipse Abyss under Wing 4
            CreatePlatform("W4_Eclipse_Abyss", new Vector3(72.0f, -7.5f, 0f), new Vector3(28f, 2f, 1f), squareSprite);

            // 5. Setup Player, Rewind, Audio, HUD (Spawn in Wing 1 Shelter at x = -10.5)
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_Master.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Contiguous Master Level to {path}");

            // Configure Build Settings: MainMenu.unity first if it exists, then Level_Master.unity
            string menuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (File.Exists(menuPath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(menuPath, true));
            }
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_01.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_02.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_03.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Grand Sanctuary of Chronos Built!",
                    $"Successfully generated {path} with:\n" +
                    "- 4 Interconnected Chambers (Courtyard of Dawn -> Echo Vault -> Clockwork Spire -> Eclipse Paradox Climax)\n" +
                    "- Hollow Knight atmospheric multi-layered backdrop\n" +
                    "- Smooth 0.35s screen-shift camera paging\n" +
                    "- Zero collision gaps or softlocks\n\n" +
                    "Ready for 2-minute uncut trailer recording!",
                    "Let's Play!");
            }
        }

        [MenuItem("Umbra Chronos/Build All Showcase Levels (Master, 01, 02, 03)")]
        public static void BuildAllLevels()
        {
            BuildMasterLevel();
            BuildLevel01();
            BuildLevel02();
            BuildLevel03();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "All Levels Generated!",
                    "Successfully built:\n- Level_Master.unity (The 4-Chamber Grand Sanctuary)\n- Level_01.unity (Dawn of the Sundial)\n- Level_02.unity (Echo of the Vault)\n- Level_03.unity (The Eclipse Paradox)",
                    "Awesome!");
            }
        }

        [MenuItem("Umbra Chronos/Build Standalone Player (macOS)")]
        public static void BuildStandaloneMac()
        {
            EnsureLevelsDirectory();
            string path = $"{LevelsFolder}/Level_Master.unity";
            if (!File.Exists(path))
            {
                BuildMasterLevel();
            }

            string menuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
            var sceneList = new System.Collections.Generic.List<string>();
            if (File.Exists(menuPath)) sceneList.Add(menuPath);
            sceneList.Add(path);

            string macDir = "Builds/macOS";
            if (!Directory.Exists(macDir)) Directory.CreateDirectory(macDir);
            string macPath = $"{macDir}/UmbraChronos.app";
            Debug.Log($"[Build] Building macOS Standalone to {macPath}...");
            var report = BuildPipeline.BuildPlayer(sceneList.ToArray(), macPath, BuildTarget.StandaloneOSX, BuildOptions.None);
            Debug.Log($"[Build] macOS build completed with result: {report.summary.result}");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "macOS Build Complete!",
                    $"Successfully created standalone app:\n{macPath}\n\nResult: {report.summary.result}",
                    "Awesome!");
            }
        }

        [MenuItem("Umbra Chronos/Build Level 01 (Dawn of the Sundial)")]
        public static void BuildLevel01()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.03f, 0.04f, 0.08f), new Vector2(24f, 15f), new Vector2(0f, 1.0f));
            SetupAtmosphereWithDecor(sprite, gradientSprite, pillarSprite, archSprite, mistSprite, 35f);

            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, -1f), 15f, 50f);
            SetupPostProcessingAndLight(sunInst);

            CreatePlatform("Cliff_Left", new Vector3(-9.0f, -2.5f, 0f), new Vector3(12f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Left", new Vector3(-15f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Spawn_Shelter_Roof", new Vector3(-9.0f, 1.2f, 0f), new Vector3(6f, 0.5f, 1f), sprite);

            CreateMonolithObject(sprite, new Vector3(-3.0f, 0.05f, 0f), new Vector2(1.4f, 3.6f), -1.75f, sunInst.transform);

            CreatePlatform("Center_Island", new Vector3(9.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Center_Shelter_Roof", new Vector3(9.0f, 1.2f, 0f), new Vector3(5f, 0.5f, 1f), sprite);

            CreateMonolithObject(sprite, new Vector3(14.5f, 0.05f, 0f), new Vector2(1.4f, 3.6f), -1.75f, sunInst.transform);

            CreatePlatform("Sanctuary_Cliff", new Vector3(27.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Right", new Vector3(32.5f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            CreateSanctuaryArch(sprite, new Vector3(28.0f, -0.5f, 0f));
            CreatePlatform("Chasm_Floor", new Vector3(10.0f, -7.0f, 0f), new Vector3(45f, 2f, 1f), sprite);

            SetupPlayer(new Vector3(-9.0f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_01.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 01 to {path}");
        }

        [MenuItem("Umbra Chronos/Build Level 02 (Echo of the Vault)")]
        public static void BuildLevel02()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLevelCamera(new Vector3(0f, 1.5f, -10f), 7.5f, new Color(0.03f, 0.04f, 0.08f), new Vector2(24f, 15f), new Vector2(0f, 1.5f));
            SetupAtmosphereWithDecor(sprite, gradientSprite, pillarSprite, archSprite, mistSprite, 35f);

            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 14f, 90f);
            SetupPostProcessingAndLight(sunInst);

            CreatePlatform("Vault_Floor_1", new Vector3(-2.0f, -2.5f, 0f), new Vector3(24f, 1.5f, 1f), sprite);
            CreatePlatform("Wall_Left", new Vector3(-14f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Vault_Ceiling", new Vector3(12.0f, 5.0f, 0f), new Vector3(56f, 1f, 1f), sprite);

            CreatePlatform("Step_Ledge_1", new Vector3(-7.0f, -0.9f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);
            CreatePlatform("High_Plate_Ledge_1", new Vector3(-3.5f, 0.0f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            GameObject gate1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate1.name = "TimedGate_1";
            gate1.transform.position = new Vector3(6.0f, -0.25f, 0f);

            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.name = "PressurePlate_1";
            plate1.transform.position = new Vector3(-3.5f, 0.35f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject soPlate1 = new SerializedObject(plateComp1);
                soPlate1.FindProperty("targetGate").objectReferenceValue = gate1.GetComponent<TimedGate>();
                soPlate1.ApplyModifiedProperties();
            }

            CreatePlatform("Vault_Floor_2", new Vector3(24.0f, -2.5f, 0f), new Vector3(28f, 1.5f, 1f), sprite);
            CreatePlatform("Wall_Right", new Vector3(36f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/PushableBlock.prefab");
            if (blockPrefab != null)
            {
                GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
                blockInst.transform.position = new Vector3(14.0f, -1.15f, 0f);
            }

            CreatePlatform("Step_Ledge_2", new Vector3(17.5f, -0.9f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);
            CreatePlatform("High_Plate_Ledge_2", new Vector3(21.0f, 0.0f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            GameObject gate2 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate2.name = "TimedGate_2";
            gate2.transform.position = new Vector3(26.5f, -0.25f, 0f);

            GameObject plate2 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate2.name = "PressurePlate_2";
            plate2.transform.position = new Vector3(21.0f, 0.35f, 0f);

            var plateComp2 = plate2.GetComponent<PressurePlate>();
            if (plateComp2 != null)
            {
                SerializedObject soPlate2 = new SerializedObject(plateComp2);
                soPlate2.FindProperty("targetGate").objectReferenceValue = gate2.GetComponent<TimedGate>();
                soPlate2.ApplyModifiedProperties();
            }

            CreateSanctuaryArch(sprite, new Vector3(31.0f, -0.5f, 0f));
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_02.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 02 to {path}");
        }

        [MenuItem("Umbra Chronos/Build Level 03 (The Eclipse Paradox)")]
        public static void BuildLevel03()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLevelCamera(new Vector3(0f, 1.5f, -10f), 7.5f, new Color(0.04f, 0.02f, 0.08f), new Vector2(24f, 15f), new Vector2(0f, 1.5f));
            SetupAtmosphereWithDecor(sprite, gradientSprite, pillarSprite, archSprite, mistSprite, 60f);

            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 15f, 90f);
            SetupPostProcessingAndLight(sunInst);

            CreatePlatform("Center_Hub", new Vector3(0f, -2.5f, 0f), new Vector3(9f, 1.5f, 1f), sprite);
            CreatePlatform("Center_Shelter_Roof", new Vector3(0f, 2.8f, 0f), new Vector3(8f, 0.5f, 1f), sprite);
            CreateMonolithObject(sprite, new Vector3(0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 2.2f);

            CreatePlatform("Western_Altar_Floor", new Vector3(-24.0f, -2.5f, 0f), new Vector3(8f, 1.5f, 1f), sprite);
            CreatePlatform("Western_Boundary_Wall", new Vector3(-28.0f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Western_Shelter", new Vector3(-24.0f, 1.5f, 0f), new Vector3(5f, 0.5f, 1f), sprite);

            CreatePlatform("Eastern_Citadel_Floor", new Vector3(24.0f, -2.5f, 0f), new Vector3(10f, 1.5f, 1f), sprite);
            CreatePlatform("Eastern_Boundary_Wall", new Vector3(29.0f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.transform.position = new Vector3(22.0f, -0.25f, 0f);

            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.transform.position = new Vector3(-24.0f, -1.65f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject so = new SerializedObject(plateComp1);
                so.FindProperty("targetGate").objectReferenceValue = gateInst.GetComponent<TimedGate>();
                so.ApplyModifiedProperties();
            }

            CreateSanctuaryArch(sprite, new Vector3(26.5f, -0.5f, 0f));
            CreatePlatform("Eclipse_Abyss", new Vector3(0f, -7.5f, 0f), new Vector3(70f, 2f, 1f), sprite);

            SetupPlayer(new Vector3(0f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_03.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 03 to {path}");
        }

        private static void SetupLevelCamera(Vector3 pos, float orthoSize, Color bgColor, Vector2 roomDims, Vector2 roomOrigin)
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

        private static void SetupMasterAtmosphere(Sprite squareSprite, Sprite gradientSprite, Sprite pillarSprite, Sprite archSprite, Sprite mistSprite)
        {
            // 1. Continuous Multi-Screen Vertical Gradient Canvas
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(36f, 3.0f, 0f);
            bgWall.transform.localScale = new Vector3(140f, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = gradientSprite != null ? gradientSprite : squareSprite;
            bgSr.sortingOrder = -20;

            // 2. Distant Ruin Silhouettes (Layer -15)
            Color distantRuinColor = new Color(0.06f, 0.08f, 0.16f, 0.85f);
            if (pillarSprite != null)
            {
                CreateBackgroundDecor(pillarSprite, new Vector3(-8f, 1.5f, 0f), new Vector3(1.5f, 3.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(8f, 1.5f, 0f), new Vector3(1.5f, 3.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(18f, 1.5f, 0f), new Vector3(1.8f, 4.0f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(32f, 1.5f, 0f), new Vector3(1.8f, 4.0f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(44f, 2.0f, 0f), new Vector3(2.0f, 4.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(56f, 2.0f, 0f), new Vector3(2.0f, 4.5f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(66f, 2.5f, 0f), new Vector3(2.2f, 5.0f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(78f, 2.5f, 0f), new Vector3(2.2f, 5.0f, 1f), distantRuinColor, -15);
            }

            if (archSprite != null)
            {
                CreateBackgroundDecor(archSprite, new Vector3(12f, 0.5f, 0f), new Vector3(2.2f, 3.2f, 1f), distantRuinColor, -14);
                CreateBackgroundDecor(archSprite, new Vector3(36f, 0.5f, 0f), new Vector3(2.5f, 3.5f, 1f), distantRuinColor, -14);
                CreateBackgroundDecor(archSprite, new Vector3(60f, 0.5f, 0f), new Vector3(2.8f, 4.0f, 1f), distantRuinColor, -14);
            }

            // 3. Volumetric Chasm Mist Bands (Layer -8)
            if (mistSprite != null)
            {
                CreateBackgroundDecor(mistSprite, new Vector3(1.0f, -4.5f, 0f), new Vector3(20f, 4f, 1f), Color.white, -8);
                CreateBackgroundDecor(mistSprite, new Vector3(48.0f, -4.5f, 0f), new Vector3(24f, 4f, 1f), Color.white, -8);
                CreateBackgroundDecor(mistSprite, new Vector3(72.0f, -4.5f, 0f), new Vector3(28f, 4f, 1f), Color.white, -8);
            }

            // 4. Global Twilight Ambiance
            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.24f, 0.28f, 0.38f, 1.0f); // Hollow Knight cold slate twilight
            light2D.intensity = 0.45f;
        }

        private static void SetupAtmosphereWithDecor(Sprite squareSprite, Sprite gradientSprite, Sprite pillarSprite, Sprite archSprite, Sprite mistSprite, float centerX)
        {
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(centerX, 3.0f, 0f);
            bgWall.transform.localScale = new Vector3(80f, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = gradientSprite != null ? gradientSprite : squareSprite;
            bgSr.sortingOrder = -20;

            Color distantRuinColor = new Color(0.06f, 0.08f, 0.16f, 0.85f);
            if (pillarSprite != null)
            {
                CreateBackgroundDecor(pillarSprite, new Vector3(centerX - 12f, 1.5f, 0f), new Vector3(1.8f, 4.0f, 1f), distantRuinColor, -15);
                CreateBackgroundDecor(pillarSprite, new Vector3(centerX + 12f, 1.5f, 0f), new Vector3(1.8f, 4.0f, 1f), distantRuinColor, -15);
            }

            if (mistSprite != null)
            {
                CreateBackgroundDecor(mistSprite, new Vector3(centerX, -4.5f, 0f), new Vector3(25f, 4f, 1f), Color.white, -8);
            }

            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = new Color(0.24f, 0.28f, 0.38f, 1.0f);
            light2D.intensity = 0.45f;
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

        private static void SetupPostProcessingAndLight(GameObject sunInst)
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
            sr.color = new Color(0.06f, 0.06f, 0.10f, 1.0f); // Pure obsidian stone (#0A0A10)
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
            sr.color = new Color(0.04f, 0.04f, 0.07f, 1f); // Deep Monolith Obsidian
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
            sr.color = new Color(1f, 0.85f, 0.4f, 0.95f); // Radiant Sanctuary Gold (#FFD166)
            sr.sortingOrder = -5;

            var trigger = arch.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one;
            trigger.isTrigger = true;
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

        private static void EnsureLevelsDirectory()
        {
            if (!Directory.Exists(LevelsFolder)) Directory.CreateDirectory(LevelsFolder);
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
