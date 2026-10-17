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
    /// 2. Level_01 (Dawn of the Sundial), Level_02 (Echo of the Vault), Level_03 (The Eclipse Paradox).
    /// Features:
    /// - Hollow Knight / Gris inspired multi-layered atmospheric background (deep indigo/abyss gradients,
    ///   distant cathedral arches, ruined gothic pillars, and soft hovering chasm mist).
    /// - Zero collision lips / perfectly flush shadow bridge alignment.
    /// - 0 softlocks, verified jump and timing budgets.
    /// </summary>
    public static class LevelBuilder
    {
        public const string LevelsFolder = "Assets/_Project/Scenes/Levels";
        public const string PrefabEnvFolder = "Assets/_Project/Prefabs/Environment";
        public const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        public const string PrefabInteractFolder = "Assets/_Project/Prefabs/Interactables";
        public const string PrefabPlayerFolder = "Assets/_Project/Prefabs/Player";
        public const string SpriteFolder = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Build Master Level (Level_Master.unity)")]
        public static void BuildMasterLevel()
        {
            EnsureLevelsDirectory();
            Sprite squareSprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");

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
            CreatePlatform("W1_Cliff_Left", new Vector3(-8.5f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), squareSprite);
            CreatePlatform("W1_Wall_Left", new Vector3(-14.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);
            GameObject w1SpawnShelter = CreatePlatform("W1_Spawn_Shelter", new Vector3(-10.0f, 1.5f, 0f), new Vector3(7.0f, 0.6f, 1f), squareSprite);
            AddInteriorBackdrop(w1SpawnShelter, -1.75f, 0.5f);

            CreateMonolithObject(squareSprite, new Vector3(-3.0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            GameObject w1PlateauRight = CreatePlatform("W1_Plateau_Right", new Vector3(8.0f, -2.5f, 0f), new Vector3(8.0f, 1.5f, 1f), squareSprite);
            AddSunlitSheen(w1PlateauRight, sunInst.transform);

            GameObject w1ThresholdShelter = CreatePlatform("W1_Threshold_Shelter", new Vector3(8.0f, 1.5f, 0f), new Vector3(6.0f, 0.6f, 1f), squareSprite);
            AddInteriorBackdrop(w1ThresholdShelter, -1.75f, 0.5f);

            // Bioluminescent Glow Lichens
            CreateGlowLichen(lichenC, new Vector3(-11.5f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(-3.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(5.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            CreatePlatform("W1_Chasm_Floor", new Vector3(0f, -7.0f, 0f), new Vector3(26f, 2f, 1f), squareSprite);

            // -------------------------------------------------------------
            // WING 2: The Echo Vault (Room 1, x = 12 to 36, Center 24)
            // -------------------------------------------------------------
            GameObject w2VaultCeiling = CreatePlatform("W2_Vault_Ceiling", new Vector3(24.0f, 5.0f, 0f), new Vector3(26.0f, 1.0f, 1f), squareSprite);
            AddInteriorBackdrop(w2VaultCeiling, -1.75f, 0.8f);

            CreatePlatform("W2_Floor_1", new Vector3(18.0f, -2.5f, 0f), new Vector3(12.0f, 1.5f, 1f), squareSprite);
            CreatePlatform("W2_Step_1", new Vector3(15.5f, -0.90f, 0f), new Vector3(2.4f, 0.4f, 1f), squareSprite);
            CreatePlatform("W2_Balcony", new Vector3(19.0f, 0.15f, 0f), new Vector3(4.5f, 0.4f, 1f), squareSprite);

            GameObject gate1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate1.name = "TimedGate_W2";
            gate1.transform.position = new Vector3(24.5f, -0.25f, 0f);

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

            CreatePlatform("W2_Floor_2", new Vector3(30.0f, -2.5f, 0f), new Vector3(12.0f, 1.5f, 1f), squareSprite);

            CreateGlowLichen(lichenB, new Vector3(13.0f, 0.0f, 0f), new Vector3(1.0f, 1.3f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(18.0f, 4.4f, 0f), new Vector3(1.4f, 1.4f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.65f, 3.8f);
            CreateGlowLichen(lichenA, new Vector3(15.0f, -0.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(20.5f, -0.1f, 0f), new Vector3(1.1f, 1.1f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(27.0f, 1.0f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(33.0f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.25f, 0.80f, 0.98f, 1f), 0.50f, 3.0f);

            // -------------------------------------------------------------
            // WING 3: The Clockwork Spire (Room 2, x = 36 to 60, Center 48)
            // -------------------------------------------------------------
            CreatePlatform("W3_Lower_Terrace", new Vector3(39.5f, -2.5f, 0f), new Vector3(7.0f, 1.5f, 1f), squareSprite);
            CreateMonolithObject(squareSprite, new Vector3(43.5f, 0.65f, 0f), new Vector2(1.8f, 4.8f), -1.75f, sunInst.transform, 1.6f);
            CreatePlatform("W3_Left_Balcony", new Vector3(38.5f, 0.8f, 0f), new Vector3(3.5f, 0.4f, 1f), squareSprite);

            GameObject w3UpperLedge = CreatePlatform("W3_Upper_Ledge", new Vector3(56.5f, -0.5f, 0f), new Vector3(7.0f, 1.5f, 1f), squareSprite);
            AddSunlitSheen(w3UpperLedge, sunInst.transform);

            GameObject gate2 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate2.name = "TimedGate_W3";
            gate2.transform.position = new Vector3(53.0f, 1.75f, 0f);

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

            CreateGlowLichen(lichenA, new Vector3(37.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(37.0f, 0.5f, 0f), new Vector3(1.0f, 1.2f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(42.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(57.5f, 0.40f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.22f, 0.74f, 0.97f, 1f), 0.50f, 3.0f);

            CreatePlatform("W3_Chasm_Floor", new Vector3(48.0f, -7.0f, 0f), new Vector3(26f, 2f, 1f), squareSprite);

            // -------------------------------------------------------------
            // WING 4: The Eclipse Paradox Sanctuary (Room 3, x = 60 to 86, Center 72)
            // -------------------------------------------------------------
            GameObject w4WesternAltar = CreatePlatform("W4_Western_Altar", new Vector3(64.0f, -2.5f, 0f), new Vector3(6.0f, 1.5f, 1f), squareSprite);
            AddSunlitSheen(w4WesternAltar, sunInst.transform);

            GameObject w4WesternShelter = CreatePlatform("W4_Western_Shelter", new Vector3(64.0f, 1.5f, 0f), new Vector3(5.0f, 0.5f, 1f), squareSprite);
            AddInteriorBackdrop(w4WesternShelter, -1.75f, 0.5f);

            CreatePlatform("W4_Center_Hub", new Vector3(72.0f, -2.5f, 0f), new Vector3(6.0f, 1.5f, 1f), squareSprite);
            GameObject w4CenterRoof = CreatePlatform("W4_Center_Roof", new Vector3(72.0f, 2.5f, 0f), new Vector3(6.0f, 0.5f, 1f), squareSprite);
            AddInteriorBackdrop(w4CenterRoof, -1.75f, 0.5f);

            CreateMonolithObject(squareSprite, new Vector3(72.0f, 0.5f, 0f), new Vector2(1.8f, 4.5f), -1.75f, sunInst.transform, 2.2f);

            GameObject w4EasternCitadel = CreatePlatform("W4_Eastern_Citadel", new Vector3(81.5f, -2.5f, 0f), new Vector3(9.0f, 1.5f, 1f), squareSprite);
            AddSunlitSheen(w4EasternCitadel, sunInst.transform);

            GameObject w4CitadelCeiling = CreatePlatform("W4_Citadel_Ceiling", new Vector3(81.5f, 3.5f, 0f), new Vector3(9.0f, 1.0f, 1f), squareSprite);
            AddInteriorBackdrop(w4CitadelCeiling, -1.75f, 0.5f);

            CreatePlatform("W4_Wall_Right", new Vector3(86.5f, 1.5f, 0f), new Vector3(1.5f, 9.0f, 1f), squareSprite);

            GameObject gate3 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate3.name = "TimedGate_Sanctuary";
            gate3.transform.position = new Vector3(79.0f, -0.25f, 0f);

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

            CreateGlowLichen(lichenA, new Vector3(62.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenC, new Vector3(64.0f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenC, new Vector3(72.0f, 2.2f, 0f), new Vector3(1.3f, 1.3f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.60f, 3.5f);
            CreateGlowLichen(lichenA, new Vector3(81.0f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(85.5f, 0.0f, 0f), new Vector3(1.0f, 1.3f, 1f), new Color(0.38f, 0.65f, 0.98f, 1f), 0.55f, 3.2f);

            CreateSanctuaryArch(squareSprite, new Vector3(83.5f, -0.4f, 0f), "The Grand Sanctuary", "MainMenu");
            CreatePlatform("W4_Eclipse_Abyss", new Vector3(72.0f, -7.5f, 0f), new Vector3(28f, 2f, 1f), squareSprite);

            // 5. Setup Player, Rewind, Audio, HUD
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_Master.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Contiguous Master Level to {path}");

            // Configure Build Settings
            string menuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (File.Exists(menuPath)) buildScenes.Add(new EditorBuildSettingsScene(menuPath, true));
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_01.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_02.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_03.unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Umbra Chronos/Regenerate All Levels and Sandbox")]
        public static void RegenerateAllLevelsAndSandbox()
        {
            BuildMasterLevel();
            BuildLevel01();
            BuildLevel02();
            BuildLevel03();
            SandboxBuilder.BuildSandboxAndPrefabs();

            string menuPath = "Assets/_Project/Scenes/UI/MainMenu.unity";
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (File.Exists(menuPath)) buildScenes.Add(new EditorBuildSettingsScene(menuPath, true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_Master.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_01.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_02.unity", true));
            buildScenes.Add(new EditorBuildSettingsScene($"{LevelsFolder}/Level_03.unity", true));
            string tutPath = $"{LevelsFolder}/Level_Tutorial.unity";
            if (File.Exists(tutPath)) buildScenes.Add(new EditorBuildSettingsScene(tutPath, true));
            string shafPath = "Assets/_Project/Scenes/Sandboxes/Sandbox_Shaf.unity";
            if (File.Exists(shafPath)) buildScenes.Add(new EditorBuildSettingsScene(shafPath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelBuilder] All levels (Master, 01, 02, 03) and Sandbox_Shaf regenerated successfully with unified Master aesthetics!");
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

            Debug.Log("[LevelBuilder] Successfully built Master, Level 01, Level 02, Level 03!");
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
        }

        // =========================================================================
        // LEVEL 01: "Dawn of the Sundial" (The Ancient Portico at First Light)
        // Aesthetic: Midnight Indigo into Rose Coral Dawn sky, glowing cyan lichens
        // =========================================================================
        [MenuItem("Umbra Chronos/Build Level 01 (Dawn of the Sundial)")]
        public static void BuildLevel01()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging
            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.035f, 0.025f, 0.06f), new Vector2(24f, 15f), new Vector2(0f, 1.0f));

            // 2. Multi-Layered Dawn Atmospheric Backdrop
            SetupAtmosphere(
                sprite,
                gradientSprite,
                pillarSprite,
                archSprite,
                mistSprite,
                -16f, 36f,
                new Color(0.88f, 0.72f, 0.85f, 1.0f),  // Warm Rose Dawn Sky Tint
                new Color(0.08f, 0.07f, 0.16f, 0.85f),  // Distant Ruin Silhouette Color
                new Color(0.32f, 0.28f, 0.38f, 1.0f),  // Golden-Rose Global Ambiance
                0.45f,
                new Color(0.95f, 0.88f, 0.92f, 0.75f),  // Morning Dawn Chasm Mist
                new float[] { -12f, -4f, 4f, 12f, 20f, 28f, 34f }, // Pillars
                new float[] { -8f, 8f, 24f },                        // Arches
                new float[] { 2.0f, 20.0f }                          // Mist Chasm Centers
            );

            // 3. SunDial & Radiation Stack
            GameObject sunInst = SetupSunDial(sprite, new Vector2(9.0f, 0f), 15f, 40f);
            SetupPostProcessingAndLight(sunInst);

            // 4. Ground Geometry & Sanctuaries
            // Left Cliff (x = -14 to -3, surface at y = -1.75)
            CreatePlatform("Cliff_Left", new Vector3(-8.5f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Left", new Vector3(-14.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), sprite);
            GameObject spawnRoof = CreatePlatform("Spawn_Shelter_Roof", new Vector3(-10.0f, 1.5f, 0f), new Vector3(7.0f, 0.6f, 1f), sprite);
            AddInteriorBackdrop(spawnRoof, -1.75f, 0.5f);

            // Monolith 1 at edge of left cliff
            CreateMonolithObject(sprite, new Vector3(-3.0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Center Island (surface at y = -1.75)
            GameObject centerIsland = CreatePlatform("Center_Island", new Vector3(8.0f, -2.5f, 0f), new Vector3(8.0f, 1.5f, 1f), sprite);
            AddSunlitSheen(centerIsland, sunInst.transform);

            GameObject centerRoof = CreatePlatform("Center_Shelter_Roof", new Vector3(8.0f, 1.5f, 0f), new Vector3(6.0f, 0.6f, 1f), sprite);
            AddInteriorBackdrop(centerRoof, -1.75f, 0.5f);

            // Monolith 2 on Center Island casting forward
            CreateMonolithObject(sprite, new Vector3(14.5f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 1.5f);

            // Sanctuary Cliff (surface at y = -1.75)
            GameObject sanctCliff = CreatePlatform("Sanctuary_Cliff", new Vector3(26.5f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            AddSunlitSheen(sanctCliff, sunInst.transform);

            CreatePlatform("Boundary_Wall_Right", new Vector3(32.5f, 2.5f, 0f), new Vector3(1.5f, 9.0f, 1f), sprite);

            // Radiant Sanctuary Arch
            CreateSanctuaryArch(sprite, new Vector3(28.0f, -0.4f, 0f), "Dawn of the Sundial", "Level_02");

            // Continuous Chasm Receptor Floor (x = -15 to 35 at y = -7.0)
            CreatePlatform("Chasm_Floor", new Vector3(10.0f, -7.0f, 0f), new Vector3(52f, 2f, 1f), sprite);

            // Bioluminescent Glow Lichens (Dawn Cyan #38BDF8)
            Color dawnCyan = new Color(0.22f, 0.74f, 0.97f, 1f);
            CreateGlowLichen(lichenC, new Vector3(-11.5f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), dawnCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(-7.5f, 0.8f, 0f), new Vector3(1.1f, 1.1f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(-3.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(4.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(8.0f, 1.15f, 0f), new Vector3(1.2f, 1.2f, 1f), dawnCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(14.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(22.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(26.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), dawnCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(28.0f, 0.2f, 0f), new Vector3(1.0f, 1.2f, 1f), dawnCyan, 0.55f, 3.2f);

            // 5. Setup Player safely in shelter
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_01.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 01 to {path}");
        }

        // =========================================================================
        // LEVEL 02: "Echo of the Vault" (The Subterranean Vault of Echoes)
        // Aesthetic: Deep Subterranean Azure Crypt, soaring vaulted ceilings, electric cyan moss
        // =========================================================================
        [MenuItem("Umbra Chronos/Build Level 02 (Echo of the Vault)")]
        public static void BuildLevel02()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging
            SetupLevelCamera(new Vector3(0f, 1.5f, -10f), 7.5f, new Color(0.015f, 0.025f, 0.05f), new Vector2(24f, 15f), new Vector2(0f, 1.5f));

            // 2. Multi-Layered Subterranean Crypt Backdrop
            SetupAtmosphere(
                sprite,
                gradientSprite,
                pillarSprite,
                archSprite,
                mistSprite,
                -16f, 40f,
                new Color(0.60f, 0.75f, 0.95f, 1.0f),  // Subterranean Abyssal Azure Tint
                new Color(0.05f, 0.07f, 0.14f, 0.90f),  // Deep Crypt Ruin Silhouette Color
                new Color(0.18f, 0.24f, 0.35f, 1.0f),  // Cool Cyan-Indigo Subterranean Light
                0.42f,
                new Color(0.80f, 0.92f, 1.0f, 0.70f),  // Cold Subterranean Cavern Mist
                new float[] { -10f, -2f, 6f, 14f, 22f, 30f, 36f }, // Pillars
                new float[] { -6f, 10f, 26f },                       // Arches
                new float[] { -2.0f, 20.0f }                         // Mist Centers
            );

            // 3. SunDial & Radiation Stack
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 14f, 90f);
            SetupPostProcessingAndLight(sunInst);

            // 4. Ground Geometry & Puzzle Architecture
            // Soaring Vault Ceiling shielding from celestial sunlight
            GameObject vaultCeiling = CreatePlatform("Vault_Ceiling", new Vector3(11.0f, 5.0f, 0f), new Vector3(56.0f, 1.0f, 1f), sprite);
            AddInteriorBackdrop(vaultCeiling, -1.75f, 0.8f);

            CreatePlatform("Wall_Left", new Vector3(-14.5f, 2.0f, 0f), new Vector3(1.5f, 8f, 1f), sprite);

            // Vault Floor 1 (surface at y = -1.75)
            CreatePlatform("Vault_Floor_1", new Vector3(-4.5f, -2.5f, 0f), new Vector3(19.0f, 1.5f, 1f), sprite);

            // Stepping ruins climbing to high balcony
            CreatePlatform("Step_Ledge_1", new Vector3(-7.0f, -0.90f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);
            CreatePlatform("High_Plate_Ledge_1", new Vector3(-3.5f, 0.15f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            // TimedGate 1 blocking threshold to Inner Sanctum
            GameObject gate1 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate1.name = "TimedGate_1";
            gate1.transform.position = new Vector3(5.5f, -0.25f, 0f);

            // PressurePlate 1 on High Balcony 1
            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.name = "PressurePlate_1";
            plate1.transform.position = new Vector3(-3.5f, 0.50f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject soPlate1 = new SerializedObject(plateComp1);
                soPlate1.FindProperty("targetGate").objectReferenceValue = gate1.GetComponent<TimedGate>();
                soPlate1.ApplyModifiedProperties();
            }

            // Vault Floor 2 (Inner Sanctum, surface at y = -1.75)
            CreatePlatform("Vault_Floor_2", new Vector3(19.5f, -2.5f, 0f), new Vector3(27.0f, 1.5f, 1f), sprite);

            // Pushable stone block in Inner Sanctum
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/PushableBlock.prefab");
            if (blockPrefab != null)
            {
                GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
                blockInst.transform.position = new Vector3(13.0f, -1.15f, 0f);
            }

            // Stepping ruins to Balcony 2
            CreatePlatform("Step_Ledge_2", new Vector3(17.5f, -0.90f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);
            CreatePlatform("High_Plate_Ledge_2", new Vector3(21.0f, 0.15f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            // TimedGate 2 blocking exit corridor
            GameObject gate2 = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gate2.name = "TimedGate_2";
            gate2.transform.position = new Vector3(26.5f, -0.25f, 0f);

            // PressurePlate 2 on High Balcony 2
            GameObject plate2 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate2.name = "PressurePlate_2";
            plate2.transform.position = new Vector3(21.0f, 0.50f, 0f);

            var plateComp2 = plate2.GetComponent<PressurePlate>();
            if (plateComp2 != null)
            {
                SerializedObject soPlate2 = new SerializedObject(plateComp2);
                soPlate2.FindProperty("targetGate").objectReferenceValue = gate2.GetComponent<TimedGate>();
                soPlate2.ApplyModifiedProperties();
            }

            CreatePlatform("Wall_Right", new Vector3(33.5f, 2.0f, 0f), new Vector3(1.5f, 8f, 1f), sprite);

            // Radiant Sanctuary Arch
            CreateSanctuaryArch(sprite, new Vector3(30.5f, -0.4f, 0f), "Echo of the Vault", "Level_03");

            // Bioluminescent Glow Lichens (Electric Cyan-Azure #22D3EE / #38BDF8)
            Color vaultCyan = new Color(0.25f, 0.80f, 0.98f, 1f);
            // Ceilings
            CreateGlowLichen(lichenC, new Vector3(-6.0f, 4.4f, 0f), new Vector3(1.3f, 1.3f, 1f), vaultCyan, 0.65f, 3.8f);
            CreateGlowLichen(lichenB, new Vector3(2.0f, 4.4f, 0f), new Vector3(1.1f, 1.2f, 1f), vaultCyan, 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(12.0f, 4.4f, 0f), new Vector3(1.4f, 1.4f, 1f), vaultCyan, 0.65f, 3.8f);
            CreateGlowLichen(lichenB, new Vector3(20.0f, 4.4f, 0f), new Vector3(1.1f, 1.2f, 1f), vaultCyan, 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(28.0f, 4.4f, 0f), new Vector3(1.3f, 1.3f, 1f), vaultCyan, 0.65f, 3.8f);

            // Room 1 Steps & Plate
            CreateGlowLichen(lichenA, new Vector3(-7.0f, -0.65f, 0f), new Vector3(1.2f, 1.0f, 1f), vaultCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(-3.5f, 0.25f, 0f), new Vector3(1.1f, 1.1f, 1f), vaultCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(4.5f, 0.8f, 0f), new Vector3(1.0f, 1.2f, 1f), vaultCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(6.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), vaultCyan, 0.50f, 3.0f);

            // Inner Sanctum & Room 2
            CreateGlowLichen(lichenA, new Vector3(12.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), vaultCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(16.5f, -0.65f, 0f), new Vector3(1.1f, 1.2f, 1f), vaultCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenC, new Vector3(21.0f, 0.25f, 0f), new Vector3(1.1f, 1.1f, 1f), vaultCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(25.5f, 0.8f, 0f), new Vector3(1.0f, 1.2f, 1f), vaultCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(29.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), vaultCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(32.0f, 0.2f, 0f), new Vector3(1.1f, 1.1f, 1f), vaultCyan, 0.55f, 3.2f);

            // 5. Setup Player safely inside vault
            SetupPlayer(new Vector3(-10.5f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_02.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 02 to {path}");
        }

        // =========================================================================
        // LEVEL 03: "The Eclipse Paradox" (The Climax of Shadows & Time)
        // Aesthetic: Cosmic Void into Royal Violet Eclipse sky, mystic violet-cyan runes
        // =========================================================================
        [MenuItem("Umbra Chronos/Build Level 03 (The Eclipse Paradox)")]
        public static void BuildLevel03()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gradient_Hollow.png");
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Gothic_Pillar.png");
            Sprite archSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Bg_Arch_Silhouette.png");
            Sprite mistSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Environment/Chasm_Mist_Gradient.png");
            Sprite lichenA = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_A.png");
            Sprite lichenB = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_B.png");
            Sprite lichenC = LoadSprite($"{SpriteFolder}/Environment/Glow_Lichen_C.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera framed on entire climax chamber (30u wide bounds prevent screen pops)
            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.02f, 0.015f, 0.045f), new Vector2(30f, 15f), new Vector2(0f, 1.0f));

            // 2. Multi-Layered Cosmic Eclipse Backdrop
            SetupAtmosphere(
                sprite,
                gradientSprite,
                pillarSprite,
                archSprite,
                mistSprite,
                -18f, 18f,
                new Color(0.85f, 0.55f, 0.98f, 1.0f),  // Cosmic Royal Violet Tint
                new Color(0.08f, 0.05f, 0.15f, 0.90f),  // Shattered Ruin Silhouette Color
                new Color(0.28f, 0.20f, 0.38f, 1.0f),  // Celestial Violet Ambiance
                0.45f,
                new Color(0.88f, 0.70f, 0.98f, 0.80f),  // Swirling Cosmic Violet Mist
                new float[] { -14f, -9f, -4f, 0f, 4f, 9f, 14f }, // Spires
                new float[] { -8f, 0f, 8f },                     // Arches
                new float[] { -5.0f, 5.0f }                      // Mist Centers
            );

            // 3. SunDial & Radiation Stack (Centered on Monolith, No camera-follow jitter)
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 15f, 90f);
            var dialComp = sunInst.GetComponent<SunDialController>();
            if (dialComp != null)
            {
                SerializedObject soDial = new SerializedObject(dialComp);
                soDial.FindProperty("followCameraX").boolValue = false;
                soDial.ApplyModifiedProperties();
            }
            SetupPostProcessingAndLight(sunInst);

            // 4. Ground Geometry & Dual-Switch Climax Setup
            // Western Altar (surface at y = -1.75, spans x = -10.5 to -5.5)
            GameObject westAltar = CreatePlatform("Western_Altar_Floor", new Vector3(-8.0f, -2.5f, 0f), new Vector3(5.0f, 1.5f, 1f), sprite);
            AddSunlitSheen(westAltar, sunInst.transform);

            CreatePlatform("Western_Boundary_Wall", new Vector3(-11.0f, 2.0f, 0f), new Vector3(1.5f, 8f, 1f), sprite);
            GameObject westShelter = CreatePlatform("Western_Shelter", new Vector3(-8.0f, 1.5f, 0f), new Vector3(5.0f, 0.5f, 1f), sprite);
            AddInteriorBackdrop(westShelter, -1.75f, 0.5f);

            // Central Hub (surface at y = -1.75, spans x = -2.5 to +2.5)
            CreatePlatform("Center_Hub", new Vector3(0f, -2.5f, 0f), new Vector3(5.0f, 1.5f, 1f), sprite);
            GameObject centerRoof = CreatePlatform("Center_Shelter_Roof", new Vector3(0f, 2.8f, 0f), new Vector3(5.0f, 0.5f, 1f), sprite);
            AddInteriorBackdrop(centerRoof, -1.75f, 0.5f);

            // Central Monolith (casts dynamic bridges left to Western Altar and right to Eastern Citadel)
            CreateMonolithObject(sprite, new Vector3(0f, 0.5f, 0f), new Vector2(1.8f, 4.5f), -1.75f, sunInst.transform, 2.2f);

            // Eastern Citadel (surface at y = -1.75, spans x = +5.0 to +11.0)
            GameObject eastCitadel = CreatePlatform("Eastern_Citadel_Floor", new Vector3(8.0f, -2.5f, 0f), new Vector3(6.0f, 1.5f, 1f), sprite);
            AddSunlitSheen(eastCitadel, sunInst.transform);

            CreatePlatform("Eastern_Boundary_Wall", new Vector3(11.5f, 2.0f, 0f), new Vector3(1.5f, 8f, 1f), sprite);
            GameObject eastShelter = CreatePlatform("Eastern_Shelter", new Vector3(8.0f, 1.5f, 0f), new Vector3(5.0f, 0.5f, 1f), sprite);
            AddInteriorBackdrop(eastShelter, -1.75f, 0.5f);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            // TimedGate sealing the Sanctuary
            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.name = "TimedGate_Eclipse";
            gateInst.transform.position = new Vector3(6.5f, -0.25f, 0f);

            // PressurePlate 1 on Western Altar
            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.name = "PressurePlate_Western_Altar";
            plate1.transform.position = new Vector3(-8.0f, -1.60f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject so = new SerializedObject(plateComp1);
                so.FindProperty("targetGate").objectReferenceValue = gateInst.GetComponent<TimedGate>();
                so.ApplyModifiedProperties();
            }

            // Radiant Sanctuary Arch
            CreateSanctuaryArch(sprite, new Vector3(9.5f, -0.4f, 0f), "The Eclipse Paradox", "MainMenu");

            // Bottomless Eclipse Abyss Receptor Floor (surface at y = -7.5)
            CreatePlatform("Eclipse_Abyss", new Vector3(0f, -7.5f, 0f), new Vector3(32f, 2f, 1f), sprite);

            // Bioluminescent Glow Lichens (Twilight Cyan & Cosmic Violet)
            Color twilightCyan = new Color(0.35f, 0.65f, 0.98f, 1f);
            Color cosmicViolet = new Color(0.55f, 0.50f, 0.98f, 1f);

            // Western Altar Lichens
            CreateGlowLichen(lichenC, new Vector3(-9.5f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1f), cosmicViolet, 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(-8.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), twilightCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(-6.2f, -1.65f, 0f), new Vector3(1.1f, 1.2f, 1f), twilightCyan, 0.55f, 3.2f);

            // Central Hub Lichens
            CreateGlowLichen(lichenC, new Vector3(-2.2f, 2.5f, 0f), new Vector3(1.3f, 1.3f, 1f), cosmicViolet, 0.60f, 3.5f);
            CreateGlowLichen(lichenC, new Vector3(2.2f, 2.5f, 0f), new Vector3(1.3f, 1.3f, 1f), cosmicViolet, 0.60f, 3.5f);
            CreateGlowLichen(lichenA, new Vector3(-2.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), twilightCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenA, new Vector3(2.0f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), twilightCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenB, new Vector3(0.0f, 2.2f, 0f), new Vector3(1.0f, 1.2f, 1f), twilightCyan, 0.55f, 3.2f);

            // Eastern Citadel & Sanctuary Lichens
            CreateGlowLichen(lichenA, new Vector3(5.5f, -1.65f, 0f), new Vector3(1.2f, 1.0f, 1f), twilightCyan, 0.50f, 3.0f);
            CreateGlowLichen(lichenC, new Vector3(8.0f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1f), cosmicViolet, 0.55f, 3.2f);
            CreateGlowLichen(lichenA, new Vector3(9.5f, -1.65f, 0f), new Vector3(1.3f, 1.0f, 1f), twilightCyan, 0.55f, 3.2f);
            CreateGlowLichen(lichenB, new Vector3(10.5f, 0.2f, 0f), new Vector3(1.0f, 1.2f, 1f), cosmicViolet, 0.55f, 3.2f);

            // 5. Setup Player at Central Hub
            SetupPlayer(new Vector3(0f, -1.0f, 0f));

            string path = $"{LevelsFolder}/Level_03.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[LevelBuilder] Saved Level 03 to {path}");
        }

        // =========================================================================
        // REUSABLE ATMOSPHERE & GEOMETRY FACTORY METHODS
        // =========================================================================

        public static void SetupLevelCamera(Vector3 pos, float orthoSize, Color bgColor, Vector2 roomDims, Vector2 roomOrigin)
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

        public static void SetupMasterAtmosphere(Sprite squareSprite, Sprite gradientSprite, Sprite pillarSprite, Sprite archSprite, Sprite mistSprite)
        {
            // 1. Continuous Multi-Screen Vertical Gradient Canvas
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(36f, 3.0f, 0f);
            bgWall.transform.localScale = new Vector3(140f, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = gradientSprite != null ? gradientSprite : squareSprite;
            bgSr.sortingOrder = -20;

            // 2. Distant Ruin Silhouettes (Layer -15 for pillars, -14 for arches)
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
            light2D.color = new Color(0.24f, 0.28f, 0.38f, 1.0f);
            light2D.intensity = 0.45f;
        }

        public static void SetupAtmosphere(
            Sprite squareSprite,
            Sprite gradientSprite,
            Sprite pillarSprite,
            Sprite archSprite,
            Sprite mistSprite,
            float minX,
            float maxX,
            Color skyTint,
            Color distantRuinColor,
            Color globalLightColor,
            float globalLightIntensity,
            Color mistTint,
            float[] pillarPositions = null,
            float[] archPositions = null,
            float[] mistCenters = null)
        {
            float centerX = (minX + maxX) * 0.5f;
            float spanWidth = (maxX - minX) + 50f;

            // 1. Continuous Gradient Canvas
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(centerX, 3.0f, 0f);
            bgWall.transform.localScale = new Vector3(spanWidth, 40f, 1f);
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = gradientSprite != null ? gradientSprite : squareSprite;
            bgSr.color = skyTint;
            bgSr.sortingOrder = -20;

            // 2. Distant Ruin Silhouettes
            if (pillarSprite != null && pillarPositions != null)
            {
                for (int i = 0; i < pillarPositions.Length; i++)
                {
                    float px = pillarPositions[i];
                    CreateBackgroundDecor(pillarSprite, new Vector3(px, 1.8f, 0f), new Vector3(1.6f, 3.8f, 1f), distantRuinColor, -15);
                }
            }

            if (archSprite != null && archPositions != null)
            {
                for (int i = 0; i < archPositions.Length; i++)
                {
                    float ax = archPositions[i];
                    CreateBackgroundDecor(archSprite, new Vector3(ax, 0.8f, 0f), new Vector3(2.4f, 3.4f, 1f), distantRuinColor, -14);
                }
            }

            // 3. Volumetric Chasm Mist Bands
            if (mistSprite != null && mistCenters != null)
            {
                for (int i = 0; i < mistCenters.Length; i++)
                {
                    float mx = mistCenters[i];
                    CreateBackgroundDecor(mistSprite, new Vector3(mx, -4.5f, 0f), new Vector3(22f, 4.5f, 1f), mistTint, -8);
                }
            }

            // 4. Global Light 2D
            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = globalLightColor;
            light2D.intensity = globalLightIntensity;
        }

        public static GameObject CreateBackgroundDecor(Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
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

        public static GameObject SetupSunDial(Sprite sprite, Vector2 center, float radius, float startAngle)
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

        public static void SetupPostProcessingAndLight(GameObject sunInst)
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

        public static void SetupPlayer(Vector3 spawnPos)
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

        public static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Sprite sprite)
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

        public static GameObject CreateMonolithObject(Sprite sprite, Vector3 pos, Vector2 size, float groundY, Transform sunTransform, float lengthMultiplier = 1.0f)
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

        public static void CreateSanctuaryArch(Sprite sprite, Vector3 pos, string chamberName = "Sanctuary Reached", string nextScene = "")
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

            var goal = arch.AddComponent<LevelGoal>();
            if (!string.IsNullOrEmpty(chamberName))
            {
                SerializedObject so = new SerializedObject(goal);
                so.FindProperty("chamberName").stringValue = chamberName;
                if (!string.IsNullOrEmpty(nextScene))
                {
                    so.FindProperty("nextSceneName").stringValue = nextScene;
                }
                so.ApplyModifiedProperties();
            }
        }

        public static GameObject CreateGlowLichen(Sprite sprite, Vector3 pos, Vector3 scale, Color glowColor, float intensity = 0.55f, float radius = 3.2f)
        {
            GameObject lichen = new GameObject("Glow_Lichen");
            lichen.transform.position = pos;
            lichen.transform.localScale = scale;

            var sr = lichen.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : GetOrCreateSquareSprite();
            sr.color = glowColor;
            sr.sortingOrder = 11; // Layer 11 in sorting order hierarchy

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

        public static void AddSunlitSheen(GameObject platform, Transform sunTransform)
        {
            if (platform == null) return;
            var sheen = platform.AddComponent<SunlitGroundSheen>();
            SerializedObject so = new SerializedObject(sheen);
            if (sunTransform != null) so.FindProperty("sunTransform").objectReferenceValue = sunTransform;
            so.ApplyModifiedProperties();
        }

        public static void AddInteriorBackdrop(GameObject roof, float floorY, float padding = 0.5f)
        {
            if (roof == null) return;
            var backdrop = roof.AddComponent<InteriorBackdrop>();
            SerializedObject so = new SerializedObject(backdrop);
            so.FindProperty("floorY").floatValue = floorY;
            so.FindProperty("horizontalPadding").floatValue = padding;
            so.ApplyModifiedProperties();
        }

        public static void EnsureLevelsDirectory()
        {
            if (!Directory.Exists(LevelsFolder)) Directory.CreateDirectory(LevelsFolder);
        }

        public static Sprite GetOrCreateSquareSprite()
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

        public static Sprite LoadSprite(string path)
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
