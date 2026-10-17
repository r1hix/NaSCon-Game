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
    /// Builds and packages the 3 showcase puzzle levels for the 3-minute trailer:
    /// - Level 01: "Dawn of the Sundial" (Multi-screen sun rotation + solid shadow bridge across lethal void)
    /// - Level 02: "Echo of the Vault" (Multi-screen rewind + Umbral Echo holding pressure plate)
    /// - Level 03: "The Eclipse Paradox" (3-screen grand climax: dual shadow bridges + Echo switch holding)
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelsFolder = "Assets/_Project/Scenes/Levels";
        private const string PrefabEnvFolder = "Assets/_Project/Prefabs/Environment";
        private const string PrefabCoreFolder = "Assets/_Project/Prefabs/Core";
        private const string PrefabInteractFolder = "Assets/_Project/Prefabs/Interactables";
        private const string PrefabPlayerFolder = "Assets/_Project/Prefabs/Player";
        private const string SpriteFolder = "Assets/_Project/Sprites";

        [MenuItem("Umbra Chronos/Build All Showcase Levels (01, 02, 03)")]
        public static void BuildAllLevels()
        {
            BuildLevel01();
            BuildLevel02();
            BuildLevel03();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "All 3 Showcase Levels Generated!",
                    "Successfully built:\n- Level_01.unity (Dawn of the Sundial - 2 Screens)\n- Level_02.unity (Echo of the Vault - 2 Screens)\n- Level_03.unity (The Eclipse Paradox - 3 Screens)\n\nAll levels feature smooth screen-shift camera transitions!",
                    "Awesome!");
            }
        }

        [MenuItem("Umbra Chronos/Build Level 01 (Dawn of the Sundial)")]
        public static void BuildLevel01()
        {
            EnsureLevelsDirectory();
            Sprite sprite = GetOrCreateSquareSprite();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging (moves over when player goes off-screen!)
            SetupLevelCamera(new Vector3(0f, 1.0f, -10f), 7.5f, new Color(0.85f, 0.40f, 0.35f), new Vector2(24f, 15f), new Vector2(0f, 1.0f));

            // 2. High Contrast Dawn Sky Atmosphere spanning multiple screens
            SetupAtmosphere(sprite, new Color(0.88f, 0.42f, 0.32f, 1f), new Color(1.0f, 0.85f, 0.70f, 1f));

            // 3. SunDial with camera tracking
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, -1f), 15f, 50f);

            // 4. Lethal Light Zone & Post Processing
            SetupPostProcessingAndLight(sunInst);

            // 5. SCREEN 1: Left Cliff with Spawn Shelter, Step Ledge, Monolith 1, Chasm 1
            CreatePlatform("Cliff_Left", new Vector3(-9.0f, -2.5f, 0f), new Vector3(12f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Left", new Vector3(-15f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Spawn_Shelter_Roof", new Vector3(-9.0f, 1.2f, 0f), new Vector3(6f, 0.5f, 1f), sprite);

            // Monolith 1: standing at edge of left cliff (base at y = -1.75, top at y = 1.85, height = 3.6)
            CreateMonolithObject(sprite, new Vector3(-3.0f, 0.05f, 0f), new Vector2(1.4f, 3.6f), -1.75f, sunInst.transform);

            // Center Island (Spans between Screen 1 and Screen 2: x = 3.5 to 14.5)
            CreatePlatform("Center_Island", new Vector3(9.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Center_Shelter_Roof", new Vector3(9.0f, 1.2f, 0f), new Vector3(5f, 0.5f, 1f), sprite);

            // 6. SCREEN 2: Monolith 2, Chasm 2, and Sanctuary Cliff
            // Monolith 2: at x = 14.5 (base at y = -1.75, top at y = 1.85, height = 3.6)
            CreateMonolithObject(sprite, new Vector3(14.5f, 0.05f, 0f), new Vector2(1.4f, 3.6f), -1.75f, sunInst.transform);

            // Sanctuary Landing Cliff (x = 21.5 to 33.0)
            CreatePlatform("Sanctuary_Cliff", new Vector3(27.0f, -2.5f, 0f), new Vector3(11f, 1.5f, 1f), sprite);
            CreatePlatform("Boundary_Wall_Right", new Vector3(32.5f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // Sanctuary Goal Arch on the right cliff (x = 28.0)
            CreateSanctuaryArch(sprite, new Vector3(28.0f, -0.5f, 0f));

            // Deep lethal chasm floor spanning under both chasms
            CreatePlatform("Chasm_Floor", new Vector3(10.0f, -7.0f, 0f), new Vector3(45f, 2f, 1f), sprite);

            // 7. Player & Rewind Manager (Spawns safely inside Screen 1 shelter)
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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging
            SetupLevelCamera(new Vector3(0f, 1.5f, -10f), 7.5f, new Color(0.12f, 0.15f, 0.25f), new Vector2(24f, 15f), new Vector2(0f, 1.5f));

            // 2. Atmosphere & Indoor Vault Light (Ancient moody teal-azure)
            SetupAtmosphere(sprite, new Color(0.20f, 0.35f, 0.48f, 1f), new Color(0.5f, 0.8f, 0.95f, 1f));

            // 3. Sun & Light
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 14f, 90f);
            SetupPostProcessingAndLight(sunInst);

            // 4. SCREEN 1: Antechamber (x = -14 to 10)
            CreatePlatform("Vault_Floor_1", new Vector3(-2.0f, -2.5f, 0f), new Vector3(24f, 1.5f, 1f), sprite);
            CreatePlatform("Wall_Left", new Vector3(-14f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // Vault Ceilings sealing the chambers from lethal sun
            CreatePlatform("Vault_Ceiling", new Vector3(12.0f, 5.0f, 0f), new Vector3(56f, 1f, 1f), sprite);

            // Step Ledge 1 to reach High Plate 1
            CreatePlatform("Step_Ledge_1", new Vector3(-7.0f, -0.9f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);

            // High Platform with Pressure Plate 1 (surface at y = 0.2, hop 0.9)
            CreatePlatform("High_Plate_Ledge_1", new Vector3(-3.5f, 0.0f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            // TimedGate 1 between Screen 1 and Screen 2 (x = 6.0)
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

            // 5. SCREEN 2: Inner Sanctum (x = 10 to 36)
            CreatePlatform("Vault_Floor_2", new Vector3(24.0f, -2.5f, 0f), new Vector3(28f, 1.5f, 1f), sprite);
            CreatePlatform("Wall_Right", new Vector3(36f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // Pushable block in Screen 2
            GameObject blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabEnvFolder}/PushableBlock.prefab");
            if (blockPrefab != null)
            {
                GameObject blockInst = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab);
                blockInst.transform.position = new Vector3(14.0f, -1.15f, 0f);
            }

            // Step Ledge 2 & High Plate 2 in Screen 2
            CreatePlatform("Step_Ledge_2", new Vector3(17.5f, -0.9f, 0f), new Vector3(2.2f, 0.4f, 1f), sprite);
            CreatePlatform("High_Plate_Ledge_2", new Vector3(21.0f, 0.0f, 0f), new Vector3(4.0f, 0.4f, 1f), sprite);

            // Final Sanctuary Gate (TimedGate 2) at x = 26.5
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

            // Sanctuary Arch inside the inner sanctum (x = 31.0)
            CreateSanctuaryArch(sprite, new Vector3(31.0f, -0.5f, 0f));

            // Player spawn in Screen 1
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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with ScreenShift room-paging across 3 Screens (Western Altar, Central Observatory, Eastern Citadel)
            SetupLevelCamera(new Vector3(0f, 1.5f, -10f), 7.5f, new Color(0.40f, 0.12f, 0.55f), new Vector2(24f, 15f), new Vector2(0f, 1.5f));

            // 2. Climax Atmosphere: Cosmic Eclipse Violet Sky
            SetupAtmosphere(sprite, new Color(0.48f, 0.16f, 0.62f, 1f), new Color(0.85f, 0.70f, 1.0f, 1f));

            // 3. SunDial with camera tracking
            GameObject sunInst = SetupSunDial(sprite, new Vector2(0f, 0f), 15f, 90f);
            SetupPostProcessingAndLight(sunInst);

            // 4. SCREEN 2 (Center Screen, x = 0): Central Observatory & Dual-Facing Monolith
            CreatePlatform("Center_Hub", new Vector3(0f, -2.5f, 0f), new Vector3(9f, 1.5f, 1f), sprite);
            CreatePlatform("Center_Shelter_Roof", new Vector3(0f, 2.8f, 0f), new Vector3(8f, 0.5f, 1f), sprite);

            // Center Monolith: Base at y = -1.75, top at y = 2.25, height = 4.0, lengthMultiplier = 2.2
            CreateMonolithObject(sprite, new Vector3(0f, 0.25f, 0f), new Vector2(1.6f, 4.0f), -1.75f, sunInst.transform, 2.2f);

            // 5. SCREEN 1 (Western Altar, center x = -24):
            CreatePlatform("Western_Altar_Floor", new Vector3(-24.0f, -2.5f, 0f), new Vector3(8f, 1.5f, 1f), sprite);
            CreatePlatform("Western_Boundary_Wall", new Vector3(-28.0f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);
            CreatePlatform("Western_Shelter", new Vector3(-24.0f, 1.5f, 0f), new Vector3(5f, 0.5f, 1f), sprite);

            // 6. SCREEN 3 (Eastern Citadel, center x = 24):
            CreatePlatform("Eastern_Citadel_Floor", new Vector3(24.0f, -2.5f, 0f), new Vector3(10f, 1.5f, 1f), sprite);
            CreatePlatform("Eastern_Boundary_Wall", new Vector3(29.0f, 2f, 0f), new Vector3(1f, 8f, 1f), sprite);

            // TimedGate sealing the Sanctuary on Eastern Citadel
            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/TimedGate.prefab");
            GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabInteractFolder}/PressurePlate.prefab");

            GameObject gateInst = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefab);
            gateInst.transform.position = new Vector3(22.0f, -0.25f, 0f);

            // Pressure Plate 1 is on the Western Altar (x = -24.0)
            GameObject plate1 = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
            plate1.transform.position = new Vector3(-24.0f, -1.65f, 0f);

            var plateComp1 = plate1.GetComponent<PressurePlate>();
            if (plateComp1 != null)
            {
                SerializedObject so = new SerializedObject(plateComp1);
                so.FindProperty("targetGate").objectReferenceValue = gateInst.GetComponent<TimedGate>();
                so.ApplyModifiedProperties();
            }

            // Sanctuary Arch behind the gate on Eastern Citadel (x = 26.5)
            CreateSanctuaryArch(sprite, new Vector3(26.5f, -0.5f, 0f));

            // Deep Lethal Abyss spanning under all 3 screens
            CreatePlatform("Eclipse_Abyss", new Vector3(0f, -7.5f, 0f), new Vector3(70f, 2f, 1f), sprite);

            // Player starts on Central Hub inside the shelter
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

        private static void SetupAtmosphere(Sprite sprite, Color ambientColor, Color lightColor)
        {
            GameObject bgWall = new GameObject("Background_Canvas");
            bgWall.transform.position = new Vector3(0f, 3f, 0f);
            bgWall.transform.localScale = new Vector3(120f, 40f, 1f); // Wide multi-screen coverage!
            var bgSr = bgWall.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = ambientColor;
            bgSr.sortingOrder = -20;

            GameObject lightObj = new GameObject("Global Light 2D");
            var light2D = lightObj.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.color = lightColor;
            light2D.intensity = 0.5f;
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
        }

        private static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Sprite sprite)
        {
            GameObject plat = new GameObject(name);
            plat.transform.position = pos;
            plat.transform.localScale = scale;

            var sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.08f, 0.08f, 0.12f, 1.0f); // #0D0D15 Obsidian stone
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
            sr.color = new Color(0.05f, 0.05f, 0.08f, 1f); // #0D0D15 Dark Matter
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
            arch.transform.localScale = new Vector3(2.5f, 3.5f, 1f);

            var sr = arch.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 0.85f, 0.4f, 0.9f); // Radiant Sanctuary Gold (#FFD166)
            sr.sortingOrder = -5;

            var trigger = arch.AddComponent<BoxCollider2D>();
            trigger.size = Vector2.one;
            trigger.isTrigger = true;
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
    }
}
#endif
